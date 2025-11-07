using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;
using TimerApp.Data;
using TimerApp.Models;
using TimerApp.ViewModels;

namespace TimerApp.Controllers;

public class AccountController : Controller
{
    private readonly AppDbContext _db;
    public AccountController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Login()
    {
        var vm = new LoginVM();
        vm.RestaurantList = await _db.Restaurants
            .OrderBy(r => r.Name)
            .Select(r => new SelectListItem { Value = r.Id.ToString(), Text = r.Name })
            .ToListAsync();
        return View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginVM vm)
    {
        if (!ModelState.IsValid)
        {
            vm.RestaurantList = await _db.Restaurants
                .OrderBy(r => r.Name)
                .Select(r => new SelectListItem { Value = r.Id.ToString(), Text = r.Name })
                .ToListAsync();
            return View(vm);
        }

        var user = await _db.Users
            .Include(u => u.Restaurant)
            .SingleOrDefaultAsync(u => u.Email == vm.Email && u.RestaurantId == vm.RestaurantId);

        if (user == null || user.PasswordHash != vm.Password)
        {
            ModelState.AddModelError("", "Ugyldigt login.");
            vm.RestaurantList = await _db.Restaurants
                .OrderBy(r => r.Name)
                .Select(r => new SelectListItem { Value = r.Id.ToString(), Text = r.Name })
                .ToListAsync();
            return View(vm);
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Name),
            new("IsAdmin", user.IsAdmin.ToString()),
            new("RestaurantId", user.RestaurantId.ToString())
        };
        var id = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(new ClaimsPrincipal(id));
        return RedirectToAction("Index", "Home");
    }

    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync();
        return RedirectToAction("Login");
    }

    [Authorize]
    [HttpGet]
    public IActionResult ChangePassword() => View();

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> ChangePassword(ChangePasswordVM vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var uid = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var user = await _db.Users.FindAsync(uid);

        if (user == null || user.PasswordHash != vm.OldPassword)
        {
            ModelState.AddModelError("", "Nuværende kode er forkert.");
            return View(vm);
        }

        user.PasswordHash = vm.NewPassword;
        await _db.SaveChangesAsync();

        return RedirectToAction("Index", "Home");
    }
}