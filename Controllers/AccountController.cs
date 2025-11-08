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
using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace TimerApp.Controllers;

public class AccountController : Controller
{
    private readonly AppDbContext _db;
    public AccountController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Login()
    {
        var vm = new LoginVM
        {
            RestaurantList = await GetRestaurantList()
        };
        return View(vm);
    }


    [HttpPost]
    public async Task<IActionResult> Login(LoginVM vm)
    {
        Console.WriteLine($"=== LOGIN FORSØG ===");
        Console.WriteLine($"Email input: '{vm.Email}'");
        Console.WriteLine($"RestaurantId input: {vm.RestaurantId}");
        Console.WriteLine($"Password input: '{vm.Password}' (længde: {vm.Password?.Length ?? 0})");

        if (!ModelState.IsValid)
        {
            Console.WriteLine("ModelState er ugyldig!");
            vm.RestaurantList = await GetRestaurantList();
            return View(vm);
        }

        Console.WriteLine("ModelState er gyldig, søger bruger...");

        var user = await _db.Users
            .Include(u => u.Restaurant)
            .SingleOrDefaultAsync(u => EF.Functions.ILike(u.Email, vm.Email) && u.RestaurantId == vm.RestaurantId);

        if (user == null)
        {
            Console.WriteLine($"? FEJL: Bruger IKKE fundet! Email='{vm.Email}', RestaurantId={vm.RestaurantId}");
            ModelState.AddModelError("", "Ugyldigt login.");
            vm.RestaurantList = await GetRestaurantList();
            return View(vm);
        }

        Console.WriteLine($"? Bruger FUNDET: ID={user.Id}, Email='{user.Email}', Name='{user.Name}', IsAdmin={user.IsAdmin}");
        Console.WriteLine($"   Restaurant: {user.Restaurant?.Name}");
        Console.WriteLine($"   PasswordHash eksisterer: {!string.IsNullOrEmpty(user.PasswordHash)}");

        if (string.IsNullOrWhiteSpace(user.PasswordHash))
        {
            Console.WriteLine($"? FEJL: PasswordHash er tom eller null!");
            ModelState.AddModelError("", "Ugyldigt login.");
            vm.RestaurantList = await GetRestaurantList();
            return View(vm);
        }

        Console.WriteLine($"   PasswordHash længde: {user.PasswordHash.Length}");
        Console.WriteLine($"   PasswordHash første 50 chars: {user.PasswordHash.Substring(0, Math.Min(50, user.PasswordHash.Length))}");

        var passwordHasher = new PasswordHasher<User>();
        PasswordVerificationResult result;

        try
        {
            Console.WriteLine("   Prøver at verificere password...");
            result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, vm.Password);
            Console.WriteLine($"   Resultat: {result}");
        }
        catch (FormatException ex)
        {
            Console.WriteLine($"? FEJL: FormatException ved VerifyHashedPassword: {ex.Message}");
            ModelState.AddModelError("", "Ugyldigt login.");
            vm.RestaurantList = await GetRestaurantList();
            return View(vm);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"? FEJL: Uventet exception: {ex.GetType().Name} - {ex.Message}");
            ModelState.AddModelError("", "Ugyldigt login.");
            vm.RestaurantList = await GetRestaurantList();
            return View(vm);
        }

        if (result == PasswordVerificationResult.Failed)
        {
            Console.WriteLine("? FEJL: Password mismatch!");
            ModelState.AddModelError("", "Ugyldigt login.");
            vm.RestaurantList = await GetRestaurantList();
            return View(vm);
        }

        Console.WriteLine("? Password er korrekt, opretter claims og logger ind...");

        var claims = new List<Claim>
    {
        new(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new(ClaimTypes.Name, user.Name),
        new("IsAdmin", user.IsAdmin.ToString()),
        new("RestaurantId", user.RestaurantId.ToString()),
        new("MustChangePassword", user.MustChangePassword.ToString())
    };

        var id = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

        await HttpContext.SignInAsync(new ClaimsPrincipal(id), new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
        });

        Console.WriteLine($"=== LOGIN SUCCES ===");

        if (user.MustChangePassword)
        {
            Console.WriteLine("Redirecter til ChangePassword (firstLogin=true)");
            return RedirectToAction("ChangePassword", new { firstLogin = true });
        }

        Console.WriteLine("Redirecter til Home/Index");
        return RedirectToAction("Index", "Home");
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

        if (user == null)
        {
            ModelState.AddModelError("", "Bruger ikke fundet.");
            ViewBag.FirstLogin = firstLogin;
            return View(vm);
        }

        var passwordHasher = new PasswordHasher<User>();

        // Ved første login: Tjek ikke gammelt password
        if (!firstLogin)
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

        // Opdater claim efter skift
        var claims = User.Claims.ToList();
        var mustChangeClaim = claims.FirstOrDefault(c => c.Type == "MustChangePassword");
        if (mustChangeClaim != null)
        {
            claims.Remove(mustChangeClaim);
            claims.Add(new Claim("MustChangePassword", "False"));
        }

        await HttpContext.SignInAsync(new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)));

        return RedirectToAction("Index", "Home");
    }

    // Helper metode
    private async Task<List<SelectListItem>> GetRestaurantList()
    {
        return await _db.Restaurants
            .OrderBy(r => r.Name)
            .Select(r => new SelectListItem { Value = r.Id.ToString(), Text = r.Name })
            .ToListAsync();
    }
}