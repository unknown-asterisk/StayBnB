using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Staybnb.Data;
namespace Staybnb.Controllers;
public class PropertiesController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Details(int id)
    {
        var p = await db.Properties.Include(x => x.Images).Include(x => x.Host).Include(x => x.Amenities).Include(x => x.Reviews).ThenInclude(r => r.Reviewer)
            .FirstOrDefaultAsync(x => x.Id == id && x.IsActive);
        return p == null ? NotFound() : View(p);
    }
}
