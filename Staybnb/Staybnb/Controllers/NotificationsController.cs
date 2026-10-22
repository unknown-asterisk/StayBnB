using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Staybnb.Data;
namespace Staybnb.Controllers;
[Authorize]
public class NotificationsController(ApplicationDbContext db) : Controller
{
    string Uid => User.FindFirstValue(ClaimTypes.NameIdentifier);
    public async Task<IActionResult> Index() => View(await db.Notifications.Where(n => n.UserId == Uid).OrderByDescending(n => n.CreatedAt).Take(100).ToListAsync());
    public async Task<IActionResult> Open(int id)
    {
        var n = await db.Notifications.FirstOrDefaultAsync(x => x.Id == id && x.UserId == Uid);
        if (n == null) return NotFound();
        n.IsRead = true; await db.SaveChangesAsync();
        return Url.IsLocalUrl(n.Link) ? Redirect(n.Link) : RedirectToAction(nameof(Index));
    }
    [HttpPost] public async Task<IActionResult> MarkAllRead()
    {
        await db.Notifications.Where(n => n.UserId == Uid && !n.IsRead).ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));
        return RedirectToAction(nameof(Index));
    }
}
