using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TimerApp.Data;
using TimerApp.Models;
using TimerApp.Services;
using TimerApp.ViewModels;

namespace TimerApp.Controllers;

public class AccountController : Controller
{
    private readonly AppDbContext _db;
    private readonly AuditService _auditService;

    public AccountController(AppDbContext db, AuditService auditService)
    {
        _db = db;
        _auditService = auditService;
    }

    [HttpGet]
    public async Task<IActionResult> Login()
    {
        if (User.Identity?.IsAuthenticated == true && User.FindFirst("IsMainAdmin")?.Value == "True")
            return RedirectToAction("Index", "Restaurants");

        var vm = new LoginVM
        {
            RestaurantList = await GetRestaurantList()
        };
        return View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginVM vm)
    {
        if (!await ValidateLoginModel(vm))
        {
            vm.RestaurantList = await GetRestaurantList();
            return View(vm);
        }

        // **SUPER ADMIN LOGIN - uden restaurant valg**
        if (vm.Email.ToLower() == "superadmin@rh.dk")
        {
            var superAdmin = await _db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == "superadmin@rh.dk" && u.IsMainAdmin);
            if (superAdmin != null && VerifyPassword(superAdmin, vm.Password))
            {
                await SignInSuperAdmin(superAdmin);
                return RedirectToAction("Index", "Restaurants");
            }

            ModelState.AddModelError("", "Ugyldigt login.");
            vm.RestaurantList = await GetRestaurantList();
            return View(vm);
        }

        // **NORMAL BRUGER LOGIN - med restaurant valg**
        var user = await _db.Users
            .Include(u => u.Restaurant)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == vm.Email.ToLower() &&
                                     u.RestaurantId == vm.RestaurantId &&
                                     !u.IsMainAdmin);

        if (user == null || !VerifyPassword(user, vm.Password))
        {
            ModelState.AddModelError("", "Ugyldigt login.");
            vm.RestaurantList = await GetRestaurantList();
            return View(vm);
        }

        await SignInUser(user, vm.RestaurantId.ToString());

        if (user.MustChangePassword)
            return RedirectToAction("ChangePassword", new { firstLogin = true });

        return RedirectToAction("Index", "Home");
    }

    private async Task<bool> ValidateLoginModel(LoginVM vm)
    {
        if (string.IsNullOrWhiteSpace(vm.Email) || string.IsNullOrWhiteSpace(vm.Password))
        {
            ModelState.AddModelError("", "Email og password er påkrævet.");
            return false;
        }

        // Super admin behøver ikke restaurant
        if (vm.Email.ToLower() == "superadmin@rh.dk")
            return true;

        // Normale brugere SKAL vælge restaurant
        if (vm.RestaurantId == 0)
        {
            ModelState.AddModelError(nameof(vm.RestaurantId), "Vælg en restaurant.");
            return false;
        }

        return true;
    }

    private bool VerifyPassword(User? user, string password)
    {
        if (user == null || string.IsNullOrWhiteSpace(user.PasswordHash))
            return false;

        var passwordHasher = new PasswordHasher<User>();
        try
        {
            return passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password)
                   != PasswordVerificationResult.Failed;
        }
        catch
        {
            return false;
        }
    }

    private async Task SignInSuperAdmin(User user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Name),
            new("IsAdmin", "True"),
            new("IsMainAdmin", "True"),
            new("MustChangePassword", "False")
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(new ClaimsPrincipal(identity), new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
        });
    }

    private async Task SignInUser(User user, string restaurantId)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Name),
            new("IsAdmin", user.IsAdmin.ToString()),
            new("RestaurantId", restaurantId),
            new("MustChangePassword", user.MustChangePassword.ToString())
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(new ClaimsPrincipal(identity), new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
        });
    }

    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync();
        return RedirectToAction("Login");
    }

    [Authorize]
    [HttpGet]
    public IActionResult ChangePassword(bool firstLogin = false)
    {
        ViewBag.FirstLogin = firstLogin;
        return View();
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> ChangePassword(ChangePasswordVM vm, bool firstLogin = false)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.FirstLogin = firstLogin;
            return View(vm);
        }

        var uid = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var user = await _db.Users.FindAsync(uid);
        if (user == null) return NotFound();

        var passwordHasher = new PasswordHasher<User>();

        if (!firstLogin && user.PasswordHash != null)
        {
            var verifyResult = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, vm.OldPassword);
            if (verifyResult == PasswordVerificationResult.Failed)
            {
                ModelState.AddModelError("", "Nuværende kode er forkert.");
                ViewBag.FirstLogin = firstLogin;
                return View(vm);
            }
        }

        user.PasswordHash = passwordHasher.HashPassword(user, vm.NewPassword);
        user.MustChangePassword = false;
        await _db.SaveChangesAsync();

        // Log password change
        await _auditService.LogPasswordChangedAsync(user.Id, uid,
            HttpContext.Connection.RemoteIpAddress?.ToString());

        return RedirectToAction("Index", "Home");
    }

    private async Task<List<SelectListItem>> GetRestaurantList()
    {
        return await _db.Restaurants
            .OrderBy(r => r.Name)
            .Select(r => new SelectListItem { Value = r.Id.ToString(), Text = r.Name })
            .ToListAsync();
    }
}