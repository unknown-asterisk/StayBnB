using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Staybnb.Data;
namespace Staybnb.Controllers;
public class HomeController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index(string city, string type)
    {
        // role-aware landing: Hosts/Admins go to their dashboards, Guests/anonymous browse
        if (User.IsInRole("Host")) return RedirectToAction("Dashboard", "Host");
        if (User.IsInRole("Admin") || User.IsInRole("SuperAdmin")) return RedirectToAction("Dashboard", "Admin");
        var q = db.Properties.Include(p => p.Images).Include(p => p.Reviews).Where(p => p.IsActive);
        if (!string.IsNullOrWhiteSpace(city)) q = q.Where(p => p.City.Contains(city));
        if (!string.IsNullOrWhiteSpace(type)) q = q.Where(p => p.PropertyType == type);
        ViewBag.City = city; ViewBag.Type = type;
        ViewBag.Types = await db.Properties.Select(p => p.PropertyType).Distinct().ToListAsync();
        return View(await q.OrderByDescending(p => p.Id).ToListAsync());
    }
    public IActionResult Error() => View();
}
