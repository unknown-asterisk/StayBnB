using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Staybnb.Models;
using Staybnb.Services;
namespace Staybnb.Controllers;
public class AccountController(UserManager<ApplicationUser> um, SignInManager<ApplicationUser> sm, IActivityLogger log) : Controller
{
    [HttpGet] public IActionResult Register() => View(new RegisterVM());
    [HttpPost]
    public async Task<IActionResult> Register(RegisterVM vm)
    {
        if (!ModelState.IsValid) return View(vm);
        var u = new ApplicationUser { UserName = vm.Email, Email = vm.Email, EmailConfirmed = true, FirstName = vm.FirstName, LastName = vm.LastName };
        var r = await um.CreateAsync(u, vm.Password);
        if (!r.Succeeded) { foreach (var e in r.Errors) ModelState.AddModelError("", e.Description); return View(vm); }
        await um.AddToRoleAsync(u, "Guest"); // initial state: Guest
        await log.LogAsync(u.Id, "User registered as Guest", ActivityType.Registration);
        await sm.SignInAsync(u, false);
        return RedirectToAction("Index", "Home");
    }
    [HttpGet] public IActionResult Login() => View(new LoginVM());
    [HttpPost]
    public async Task<IActionResult> Login(LoginVM vm)
    {
        if (!ModelState.IsValid) return View(vm);
        var u = await um.FindByEmailAsync(vm.Email);
        if (u != null && (await sm.PasswordSignInAsync(u, vm.Password, false, false)).Succeeded)
        {
            await log.LogAsync(u.Id, "User logged in", ActivityType.Login);
            return RedirectToAction("Index", "Home");
        }
        ModelState.AddModelError("", "Invalid email or password.");
        return View(vm);
    }
    [HttpPost] public async Task<IActionResult> Logout() { await sm.SignOutAsync(); return RedirectToAction("Index", "Home"); }
    public IActionResult AccessDenied() => View();
}
