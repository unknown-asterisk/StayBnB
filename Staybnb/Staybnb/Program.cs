using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Staybnb.Data;
using Staybnb.Models;
using Staybnb.Services;

var b = WebApplication.CreateBuilder(args);
b.Services.AddDbContext<ApplicationDbContext>(o => o.UseSqlServer(b.Configuration.GetConnectionString("DefaultConnection")));
b.Services.AddIdentity<ApplicationUser, IdentityRole>(o => { o.Password.RequiredLength = 6; o.Password.RequireNonAlphanumeric = false; o.Password.RequireUppercase = false; })
    .AddEntityFrameworkStores<ApplicationDbContext>().AddDefaultTokenProviders();
b.Services.ConfigureApplicationCookie(o => { o.LoginPath = "/Account/Login"; o.AccessDeniedPath = "/Account/AccessDenied"; });
// re-validate roles on every request so Guest->Host swaps apply immediately
b.Services.Configure<SecurityStampValidatorOptions>(o => o.ValidationInterval = TimeSpan.Zero);
b.Services.AddScoped<IActivityLogger, ActivityLogger>();
b.Services.AddScoped<INotifier, Notifier>();
b.Services.AddControllersWithViews();

var app = b.Build();
using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.Migrate();
    await RoleSeeder.SeedAsync(scope.ServiceProvider);
}
if (!app.Environment.IsDevelopment()) app.UseExceptionHandler("/Home/Error");
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
app.Run();
