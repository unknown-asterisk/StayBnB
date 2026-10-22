using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Staybnb.Data;
using Staybnb.Models;
using Staybnb.Services;
namespace Staybnb.Controllers;
[Authorize(Roles = "Host")]
public class HostController(ApplicationDbContext db, IActivityLogger log, INotifier notifier) : Controller
{
    string Uid => User.FindFirstValue(ClaimTypes.NameIdentifier);

    public async Task<IActionResult> Dashboard()
    {
        var props = db.Properties.Where(p => p.HostId == Uid);
        var bookings = db.Bookings.Where(b => b.Property.HostId == Uid);
        ViewBag.Properties = await props.CountAsync();
        ViewBag.Active = await props.CountAsync(p => p.IsActive);
        ViewBag.Bookings = await bookings.CountAsync();
        ViewBag.Pending = await bookings.CountAsync(b => b.Status == BookingStatus.Pending);
        ViewBag.Earnings = await bookings.Where(b => b.Payment.Status == PaymentStatus.Paid).SumAsync(b => (decimal?)b.TotalPrice) ?? 0;
        var ratings = await db.Reviews.Where(r => r.Property.HostId == Uid).Select(r => r.Rating).ToListAsync();
        ViewBag.Rating = ratings.Count == 0 ? 0 : ratings.Average();
        return View();
    }

    public async Task<IActionResult> Properties() =>
        View(await db.Properties.Include(p => p.Images).Include(p => p.Bookings).Where(p => p.HostId == Uid).OrderByDescending(p => p.Id).ToListAsync());

    [HttpPost]
    public async Task<IActionResult> Toggle(int id)
    {
        var p = await db.Properties.FirstOrDefaultAsync(x => x.Id == id && x.HostId == Uid);
        if (p == null) return NotFound();
        var approved = await db.HostApplications.AnyAsync(a => a.PropertyId == id && a.Status == ApplicationStatus.Approved);
        if (!approved) { TempData["Error"] = "This property has not been approved by an admin yet."; return RedirectToAction(nameof(Properties)); }
        p.IsActive = !p.IsActive;
        await db.SaveChangesAsync();
        await log.LogAsync(Uid, $"Property '{p.Title}' set {(p.IsActive ? "Active" : "Inactive")}", ActivityType.Property);
        return RedirectToAction(nameof(Properties));
    }

    public async Task<IActionResult> Bookings() =>
        View(await db.Bookings.Include(b => b.Property).Include(b => b.Guest).Where(b => b.Property.HostId == Uid).OrderByDescending(b => b.Id).ToListAsync());

    [HttpPost]
    public async Task<IActionResult> Decide(int id, bool approve)
    {
        var b = await db.Bookings.Include(x => x.Property).Include(x => x.Payment).FirstOrDefaultAsync(x => x.Id == id && x.Property.HostId == Uid);
        if (b == null) return NotFound();
        if (b.Status != BookingStatus.Pending) return RedirectToAction(nameof(Bookings));
        b.Status = approve ? BookingStatus.Approved : BookingStatus.Rejected;
        if (b.Payment != null) b.Payment.Status = approve ? PaymentStatus.Paid : PaymentStatus.Refunded; // simulated payment
        await db.SaveChangesAsync();
        await log.LogAsync(Uid, $"Booking #{b.Id} {b.Status}", ActivityType.BookingStatus);
        await notifier.NotifyAsync(b.GuestId, approve ? "Booking Approved" : "Booking Rejected", $"Your booking for '{b.Property.Title}' was {b.Status}.", approve ? NotificationType.Success : NotificationType.Warning, "/Guest/Bookings");
        return RedirectToAction(nameof(Bookings));
    }

    [HttpGet]
    public async Task<IActionResult> CheckInProcess(int propertyId)
    {
        var p = await db.Properties.Include(x => x.CheckInProcess).FirstOrDefaultAsync(x => x.Id == propertyId && x.HostId == Uid);
        if (p == null) return NotFound();
        ViewBag.Property = p;
        var steps = p.CheckInProcess == null ? new List<string>() : JsonSerializer.Deserialize<List<string>>(p.CheckInProcess.StepsJson);
        ViewBag.Steps = string.Join("\n", steps);
        return View(p.CheckInProcess ?? new CheckInProcess { PropertyId = propertyId });
    }
    [HttpPost]
    public async Task<IActionResult> CheckInProcess(int propertyId, string title, string steps)
    {
        var p = await db.Properties.Include(x => x.CheckInProcess).FirstOrDefaultAsync(x => x.Id == propertyId && x.HostId == Uid);
        if (p == null) return NotFound();
        var list = (steps ?? "").Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        var json = JsonSerializer.Serialize(list);
        if (p.CheckInProcess == null) p.CheckInProcess = new CheckInProcess { Title = title, StepsJson = json };
        else { p.CheckInProcess.Title = title; p.CheckInProcess.StepsJson = json; }
        await db.SaveChangesAsync();
        await log.LogAsync(Uid, $"Check-in process configured for '{p.Title}'", ActivityType.Property);
        TempData["Success"] = "Check-in process saved.";
        return RedirectToAction(nameof(Properties));
    }

    public async Task<IActionResult> CheckIns() =>
        View(await db.GuestCheckIns.Include(c => c.Documents).Include(c => c.Booking).ThenInclude(b => b.Guest).Include(c => c.Booking).ThenInclude(b => b.Property)
            .Where(c => c.Booking.Property.HostId == Uid).OrderByDescending(c => c.Id).ToListAsync());

    [HttpPost]
    public async Task<IActionResult> VerifyDocument(int id, bool approve)
    {
        var d = await db.GuestDocuments.Include(x => x.GuestCheckIn).ThenInclude(c => c.Booking).ThenInclude(b => b.Property).FirstOrDefaultAsync(x => x.Id == id && x.GuestCheckIn.Booking.Property.HostId == Uid);
        if (d == null) return NotFound();
        d.Status = approve ? DocumentStatus.Approved : DocumentStatus.Rejected;
        if (approve) d.GuestCheckIn.Status = CheckInStatus.Verified;
        await db.SaveChangesAsync();
        await log.LogAsync(Uid, $"Document #{d.Id} {d.Status}", ActivityType.CheckIn);
        await notifier.NotifyAsync(d.GuestCheckIn.Booking.GuestId, $"Document {d.Status}", $"Your {d.DocumentType} was {d.Status}.", approve ? NotificationType.Success : NotificationType.Warning, $"/Guest/CheckIn?bookingId={d.GuestCheckIn.BookingId}");
        return RedirectToAction(nameof(CheckIns));
    }

    [HttpPost] // Pending -> Approved -> CheckedIn
    public async Task<IActionResult> ConfirmCheckIn(int checkInId)
    {
        var c = await db.GuestCheckIns.Include(x => x.Documents).Include(x => x.Booking).ThenInclude(b => b.Property).FirstOrDefaultAsync(x => x.Id == checkInId && x.Booking.Property.HostId == Uid);
        if (c == null) return NotFound();
        if (!c.Documents.Any(d => d.Status == DocumentStatus.Approved)) { TempData["Error"] = "Verify at least one document first."; return RedirectToAction(nameof(CheckIns)); }
        c.Status = CheckInStatus.Completed;
        c.Booking.Status = BookingStatus.CheckedIn;
        await db.SaveChangesAsync();
        await log.LogAsync(Uid, $"Booking #{c.BookingId} CheckedIn", ActivityType.BookingStatus);
        await notifier.NotifyAsync(c.Booking.GuestId, "Checked In", $"You're checked in at '{c.Booking.Property.Title}'. Enjoy your stay!", NotificationType.Success, "/Guest/Bookings");
        return RedirectToAction(nameof(CheckIns));
    }
}
