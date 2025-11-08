using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using TimerApp.Data;
using TimerApp.Models;
using TimerApp.ViewModels;
using Microsoft.AspNetCore.Identity;

namespace TimerApp.Controllers;

[Authorize]
public class AdminController : Controller
{
    private readonly AppDbContext _db;
    public AdminController(AppDbContext db) => _db = db;

    // ? Helper metode til at generere sikker midlertidig adgangskode
    private string GenerateTempPassword()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, 10)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }

    public IActionResult Index()
    {
        if (User.FindFirst("IsAdmin")?.Value != "True") return Forbid();

        var restaurantId = int.Parse(User.FindFirst("RestaurantId")!.Value);
        var users = _db.Users
                       .Include(u => u.TimeEntries)
                       .Where(u => u.RestaurantId == restaurantId)
                       .OrderBy(u => u.Name)
                       .ToList();
        return View(users);
    }

    public IActionResult CreateUser() => View();

    [HttpPost]
    public async Task<IActionResult> CreateUser(CreateUserVM vm)
    {
        if (User.FindFirst("IsAdmin")?.Value != "True") return Forbid();

        if (!ModelState.IsValid) return View(vm);

        // Tjek om email allerede eksisterer i samme restaurant
        var restaurantId = int.Parse(User.FindFirst("RestaurantId")!.Value);
        if (await _db.Users.AnyAsync(u => u.Email.ToLower() == vm.Email.ToLower() && u.RestaurantId == restaurantId))
        {
            ModelState.AddModelError(nameof(vm.Email), "Denne email er allerede registreret.");
            return View(vm);
        }

        var tempPassword = GenerateTempPassword();
        var passwordHasher = new PasswordHasher<User>();
        var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var newUser = new User
        {
            Name = vm.Name,
            Email = vm.Email,
            IsAdmin = vm.IsAdmin,
            RestaurantId = restaurantId,
            CreatedByUserId = currentUserId,
            MustChangePassword = true // ? Vigtigt: Tving password-skift
        };

        newUser.PasswordHash = passwordHasher.HashPassword(newUser, tempPassword);

        _db.Users.Add(newUser);
        await _db.SaveChangesAsync();

        // Vis den midlertidige adgangskode til admin én gang
        TempData["TempPassword"] = tempPassword;
        TempData["NewUserEmail"] = vm.Email;

        return RedirectToAction(nameof(Index));
    }

    // CSV kun for egen restaurant
    public async Task<IActionResult> ExportCsv(int year, int month)
    {
        if (User.FindFirst("IsAdmin")?.Value != "True") return Forbid();

        var restaurantId = int.Parse(User.FindFirst("RestaurantId")!.Value);
        var data = await _db.TimeEntries
                            .Include(t => t.User)
                            .Where(t => t.Date.Year == year && t.Date.Month == month && t.User.RestaurantId == restaurantId)
                            .OrderBy(t => t.User.Name).ThenBy(t => t.Date)
                            .ToListAsync();

        var csv = "Navn;Dato;Timer;Note\n" +
                  string.Join("\n", data.Select(t =>
                      $"{t.User.Name};{t.Date:yyyy-MM-dd};{t.Hours};{t.Note ?? ""}"));

        return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv",
                    $"timer_{year}_{month}.csv");
    }

    // Slet kun brugere fra egen restaurant
    [HttpPost]
    public async Task<IActionResult> DeleteUser(int id)
    {
        if (User.FindFirst("IsAdmin")?.Value != "True") return Forbid();

        var restaurantId = int.Parse(User.FindFirst("RestaurantId")!.Value);
        var user = await _db.Users
                            .Include(u => u.TimeEntries)
                            .FirstOrDefaultAsync(u => u.Id == id && u.RestaurantId == restaurantId);

        if (user == null) return NotFound();

        _db.TimeEntries.RemoveRange(user.TimeEntries);
        _db.Users.Remove(user);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}