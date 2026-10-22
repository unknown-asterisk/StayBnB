using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Staybnb.Data;
using Staybnb.Models;
using Staybnb.Services;
namespace Staybnb.Controllers;
[Authorize(Roles = "Admin,SuperAdmin")]
public class AdminController(ApplicationDbContext db, UserManager<ApplicationUser> um, IActivityLogger log, INotifier notifier) : Controller
{
    string Uid => User.FindFirstValue(ClaimTypes.NameIdentifier);

    public async Task<IActionResult> Dashboard()
    {
        ViewBag.Users = await db.Users.CountAsync();
        ViewBag.PendingApps = await db.HostApplications.CountAsync(a => a.Status == ApplicationStatus.Pending);
        ViewBag.ActiveProps = await db.Properties.CountAsync(p => p.IsActive);
        ViewBag.Revenue = await db.Payments.Where(p => p.Status == PaymentStatus.Paid).SumAsync(p => (decimal?)p.Amount) ?? 0;
        ViewBag.Bookings = await db.Bookings.CountAsync();
        ViewBag.Hosts = (await um.GetUsersInRoleAsync("Host")).Count;
        ViewBag.Recent = await db.ActivityLogs.Include(l => l.User).OrderByDescending(l => l.CreatedAt).Take(8).ToListAsync();
        return View();
    }

    public async Task<IActionResult> Applications() =>
        View(await db.HostApplications.Include(a => a.ApplicationUser).Include(a => a.Property).ThenInclude(p => p.Images).OrderBy(a => a.Status).ThenByDescending(a => a.AppliedAt).ToListAsync());

    [HttpPost]
    public async Task<IActionResult> Decide(int id, bool approve)
    {
        var a = await db.HostApplications.Include(x => x.Property).FirstOrDefaultAsync(x => x.Id == id);
        if (a == null) return NotFound();
        if (a.Status != ApplicationStatus.Pending) return RedirectToAction(nameof(Applications));
        var user = await um.FindByIdAsync(a.ApplicationUserId);
        if (approve)
        {
            // The Transition: add Host, remove Guest (a user is a Guest OR a Host)
            if (!await um.IsInRoleAsync(user, "Host")) await um.AddToRoleAsync(user, "Host");
            if (await um.IsInRoleAsync(user, "Guest")) await um.RemoveFromRoleAsync(user, "Guest");
            await um.UpdateSecurityStampAsync(user); // forces cookie refresh with new roles
            a.Property.IsActive = true;
            a.Status = ApplicationStatus.Approved;
            await log.LogAsync(user.Id, "Role change: Guest -> Host", ActivityType.RoleChange);
        }
        else a.Status = ApplicationStatus.Rejected;
        await db.SaveChangesAsync();
        await log.LogAsync(Uid, $"Application #{a.Id} {a.Status}", ActivityType.Application);
        await notifier.NotifyAsync(user.Id, approve ? "Host Application Approved" : "Host Application Rejected",
            approve ? $"Congratulations! '{a.Property.Title}' is approved and live." : $"Your application for '{a.Property.Title}' was rejected.",
            approve ? NotificationType.Success : NotificationType.Warning, approve ? "/Host/Dashboard" : "/Apply/Status");
        return RedirectToAction(nameof(Applications));
    }

    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> Users(string search)
    {
        var q = db.Users.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search)) q = q.Where(u => u.Email.Contains(search) || u.FirstName.Contains(search) || u.LastName.Contains(search));
        var users = await q.OrderBy(u => u.Email).Take(100).ToListAsync();
        var roles = new Dictionary<string, IList<string>>();
        foreach (var u in users) roles[u.Id] = await um.GetRolesAsync(u);
        ViewBag.Roles = roles; ViewBag.Search = search;
        return View(users);
    }

    [HttpPost, Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> Promote(string userId)
    {
        var user = await um.FindByIdAsync(userId);
        if (user == null) return NotFound();
        if (!await um.IsInRoleAsync(user, "Guest")) { TempData["Error"] = "Only Guests can be promoted to Admin."; return RedirectToAction(nameof(Users)); }
        await um.RemoveFromRoleAsync(user, "Guest");
        await um.AddToRoleAsync(user, "Admin");
        await um.UpdateSecurityStampAsync(user);
        await log.LogAsync(Uid, $"Promoted {user.Email} to Admin", ActivityType.RoleChange);
        await notifier.NotifyAsync(user.Id, "You are now an Admin", "A SuperAdmin promoted your account.", NotificationType.Success, "/Admin/Dashboard");
        return RedirectToAction(nameof(Users));
    }

    public async Task<IActionResult> Logs(ActivityType? type)
    {
        var q = db.ActivityLogs.Include(l => l.User).AsQueryable();
        if (type != null) q = q.Where(l => l.ActivityType == type);
        ViewBag.Type = type;
        return View(await q.OrderByDescending(l => l.CreatedAt).Take(300).ToListAsync());
    }
}
