using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Staybnb.Data;
using Staybnb.Models;
using Staybnb.Services;
namespace Staybnb.Controllers;
[Authorize(Roles = "Guest,Host")]
public class ApplyController(ApplicationDbContext db, IWebHostEnvironment env, IActivityLogger log, INotifier notifier, UserManager<ApplicationUser> um) : Controller
{
    string Uid => User.FindFirstValue(ClaimTypes.NameIdentifier);
    [HttpGet]
public async Task<IActionResult> Create()
{
    ViewBag.Amenities = await db.Amenities.ToListAsync();
    return View(new ApplyVM());
}
    [HttpPost]
    public async Task<IActionResult> Create(ApplyVM vm, List<IFormFile> images)
    {
        var valid = images?.Where(FileHelper.IsValid).Where(i => !i.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)).ToList() ?? new();
        if (valid.Count == 0) ModelState.AddModelError("images", "At least one property image is required. The application cannot be submitted without one.");
        if (!ModelState.IsValid) return View(vm);

        await using var tx = await db.Database.BeginTransactionAsync(); // property + images + application together
        var prop = new HostProperty { Title = vm.Title, Description = vm.Description, PricePerNight = vm.PricePerNight, CleaningFee = vm.CleaningFee,
            Address = vm.Address, City = vm.City, PropertyType = vm.PropertyType, MaxGuests = vm.MaxGuests, HostId = Uid, IsActive = false };
        foreach (var f in valid) prop.Images.Add(new PropertyImage { ImageUrl = "/uploads/properties/" + await FileHelper.SaveAsync(f, env, "properties") });
        db.Properties.Add(prop);
        await db.SaveChangesAsync();
        db.HostApplications.Add(new HostApplication { ApplicationUserId = Uid, PropertyId = prop.Id });
        await db.SaveChangesAsync();
        await tx.CommitAsync();

        await log.LogAsync(Uid, $"Host application submitted: {prop.Title}", ActivityType.Application);
        await notifier.NotifyAsync(Uid, "Application Submitted", $"Your application for '{prop.Title}' is under review.", NotificationType.Application);
        foreach (var a in await um.GetUsersInRoleAsync("Admin")) await notifier.NotifyAsync(a.Id, "New Host Application", $"'{prop.Title}' needs review.", NotificationType.Application, "/Admin/Applications");
        foreach (var a in await um.GetUsersInRoleAsync("SuperAdmin")) await notifier.NotifyAsync(a.Id, "New Host Application", $"'{prop.Title}' needs review.", NotificationType.Application, "/Admin/Applications");
        return RedirectToAction(nameof(Status));
    }
    public async Task<IActionResult> Status() =>
        View(await db.HostApplications.Include(a => a.Property).ThenInclude(p => p.Images).Where(a => a.ApplicationUserId == Uid).OrderByDescending(a => a.AppliedAt).ToListAsync());
}
