using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Staybnb.Models;
namespace Staybnb.Data;
public static class RoleSeeder
{
    public static readonly string[] Roles = { "SuperAdmin", "Admin", "Host", "Guest" };
    public static async Task SeedAsync(IServiceProvider sp)
    {
        var rm = sp.GetRequiredService<RoleManager<IdentityRole>>();
        var um = sp.GetRequiredService<UserManager<ApplicationUser>>();
        var db = sp.GetRequiredService<ApplicationDbContext>();
        foreach (var r in Roles) if (!await rm.RoleExistsAsync(r)) await rm.CreateAsync(new IdentityRole(r));

        const string email = "superadmin@staybnb.com";
        if (await um.FindByEmailAsync(email) == null)
        {
            var u = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true, FirstName = "Super", LastName = "Admin" };
            if ((await um.CreateAsync(u, "Admin@123")).Succeeded)
            {
                await um.AddToRoleAsync(u, "SuperAdmin");
                db.ActivityLogs.Add(new ActivityLog { UserId = u.Id, Action = "SuperAdmin seeded", ActivityType = ActivityType.System });
                await db.SaveChangesAsync();
            }
        }
        if (!await db.Amenities.AnyAsync())
        {
            db.Amenities.AddRange(new Amenity { Name = "Wifi", IconClass = "bi-wifi" }, new Amenity { Name = "Kitchen", IconClass = "bi-cup-hot" },
                new Amenity { Name = "Pool", IconClass = "bi-water" }, new Amenity { Name = "Air Conditioning", IconClass = "bi-snow" },
                new Amenity { Name = "Parking", IconClass = "bi-car-front" }, new Amenity { Name = "Washer", IconClass = "bi-tsunami" });
            await db.SaveChangesAsync();
        }
    }
}
