using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Staybnb.Data;
using Staybnb.Models;
using Staybnb.Services;
namespace Staybnb.Controllers;
[Authorize(Roles = "Guest")]
public class GuestController(ApplicationDbContext db, IWebHostEnvironment env, IActivityLogger log, INotifier notifier) : Controller
{
    string Uid => User.FindFirstValue(ClaimTypes.NameIdentifier);

    public async Task<IActionResult> Bookings() =>
        View(await db.Bookings.Include(b => b.Property).ThenInclude(p => p.Images).Include(b => b.GuestCheckIn)
            .Where(b => b.GuestId == Uid).OrderByDescending(b => b.Id).ToListAsync());

    [HttpPost]
    public async Task<IActionResult> Book(int propertyId, DateTime checkIn, DateTime checkOut, int guests)
    {
        var p = await db.Properties.FirstOrDefaultAsync(x => x.Id == propertyId && x.IsActive);
        if (p == null) return NotFound();
        var nights = (checkOut.Date - checkIn.Date).Days;
        string err = null;
        if (checkIn.Date < DateTime.Today) err = "Check-in cannot be in the past.";
        else if (nights < 1) err = "Check-out must be after check-in.";
        else if (guests < 1 || guests > p.MaxGuests) err = $"Guests must be between 1 and {p.MaxGuests}.";
        else if (await db.Bookings.AnyAsync(b => b.PropertyId == p.Id && (b.Status == BookingStatus.Approved || b.Status == BookingStatus.CheckedIn)
                 && checkIn.Date < b.CheckOutDate && checkOut.Date > b.CheckInDate)) err = "Those dates are not available.";
        if (err != null) { TempData["Error"] = err; return RedirectToAction("Details", "Properties", new { id = propertyId }); }

        var (service, total) = Pricing.Calculate(p.PricePerNight, nights, p.CleaningFee);
        var booking = new Booking { PropertyId = p.Id, GuestId = Uid, CheckInDate = checkIn.Date, CheckOutDate = checkOut.Date, Guests = guests,
            CleaningFee = p.CleaningFee, ServiceFee = service, TotalPrice = total, Status = BookingStatus.Pending,
            Payment = new Payment { Amount = total, Status = PaymentStatus.Pending } };
        db.Bookings.Add(booking);
        await db.SaveChangesAsync();
        await log.LogAsync(Uid, $"Booking #{booking.Id} requested (Pending)", ActivityType.BookingStatus);
        await notifier.NotifyAsync(p.HostId, "New Booking Request", $"New request for '{p.Title}' ({nights} nights).", NotificationType.Booking, "/Host/Bookings");
        TempData["Success"] = "Booking requested. Waiting for host approval.";
        return RedirectToAction(nameof(Bookings));
    }

    [HttpPost]
    public async Task<IActionResult> Cancel(int id)
    {
        var b = await db.Bookings.Include(x => x.Payment).FirstOrDefaultAsync(x => x.Id == id && x.GuestId == Uid);
        if (b == null || b.Status is BookingStatus.CheckedIn or BookingStatus.Cancelled) return RedirectToAction(nameof(Bookings));
        b.Status = BookingStatus.Cancelled;
        await db.SaveChangesAsync();
        await log.LogAsync(Uid, $"Booking #{b.Id} cancelled", ActivityType.BookingStatus);
        return RedirectToAction(nameof(Bookings));
    }

    [HttpPost]
public async Task<IActionResult> Review(int bookingId, int rating, string comment)
{
    var b = await db.Bookings.Include(x => x.Property).FirstOrDefaultAsync(x => x.Id == bookingId && x.GuestId == Uid);
    if (b == null || b.Status != BookingStatus.CheckedIn || rating < 1 || rating > 5)
    { TempData["Error"] = "You can only review a stay you have checked in to."; return RedirectToAction(nameof(Bookings)); }
    if (await db.Reviews.AnyAsync(r => r.PropertyId == b.PropertyId && r.ReviewerId == Uid))
    { TempData["Error"] = "You have already reviewed this property."; return RedirectToAction(nameof(Bookings)); }
    db.Reviews.Add(new Review { PropertyId = b.PropertyId, ReviewerId = Uid, Rating = rating, Comment = comment });
    await db.SaveChangesAsync();
    await log.LogAsync(Uid, $"Review left for '{b.Property.Title}'", ActivityType.Property);
    await notifier.NotifyAsync(b.Property.HostId, "New Review", $"A guest left a {rating}-star review on '{b.Property.Title}'.", NotificationType.Info);
    TempData["Success"] = "Thanks for your review!";
    return RedirectToAction(nameof(Bookings));
}

    // Multi-step check-in: 1) read host rules/steps  2) upload ID/Passport  3) wait for host verification
    public async Task<IActionResult> CheckIn(int bookingId)
    {
        var b = await db.Bookings.Include(x => x.Property).ThenInclude(p => p.CheckInProcess).Include(x => x.GuestCheckIn).ThenInclude(c => c.Documents)
            .FirstOrDefaultAsync(x => x.Id == bookingId && x.GuestId == Uid);
        if (b == null) return NotFound();
        if (b.Status != BookingStatus.Approved && b.Status != BookingStatus.CheckedIn) { TempData["Error"] = "Check-in opens once the host approves your booking."; return RedirectToAction(nameof(Bookings)); }
        if (b.GuestCheckIn == null)
        {
            var proc = b.Property.CheckInProcess ?? new CheckInProcess { PropertyId = b.PropertyId, Title = "Standard check-in", StepsJson = "[\"Bring a valid ID or passport\",\"Follow the host's arrival instructions\"]" };
            b.GuestCheckIn = new GuestCheckIn { CheckInProcess = proc };
            await db.SaveChangesAsync();
            await log.LogAsync(Uid, $"Check-in started for booking #{b.Id}", ActivityType.CheckIn);
        }
        return View(b);
    }

    [HttpPost]
    public async Task<IActionResult> UploadDocument(int bookingId, string documentType, IFormFile file)
    {
        var b = await db.Bookings.Include(x => x.GuestCheckIn).Include(x => x.Property).FirstOrDefaultAsync(x => x.Id == bookingId && x.GuestId == Uid);
        if (b?.GuestCheckIn == null) return NotFound();
        if (!FileHelper.IsValid(file) || string.IsNullOrWhiteSpace(documentType)) { TempData["Error"] = "Choose a document type and a valid file (jpg, png, pdf < 8MB)."; return RedirectToAction(nameof(CheckIn), new { bookingId }); }
        db.GuestDocuments.Add(new GuestDocument { GuestCheckInId = b.GuestCheckIn.Id, DocumentType = documentType, FileName = await FileHelper.SaveAsync(file, env, "docs") });
        b.GuestCheckIn.Status = CheckInStatus.DocumentsSubmitted;
        await db.SaveChangesAsync();
        await log.LogAsync(Uid, $"Document uploaded for booking #{b.Id}", ActivityType.CheckIn);
        await notifier.NotifyAsync(b.Property.HostId, "Guest Document Uploaded", $"A guest uploaded {documentType} for verification.", NotificationType.Info, "/Host/CheckIns");
        return RedirectToAction(nameof(CheckIn), new { bookingId });
    }
}
