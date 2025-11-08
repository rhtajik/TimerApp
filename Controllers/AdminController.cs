using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using TimerApp.Data;
using TimerApp.Models;
using Microsoft.AspNetCore.Identity; // ? TILFØJ DENNE!

namespace TimerApp.Controllers;

[Authorize]
public class AdminController : Controller
{
    private readonly AppDbContext _db;
    public AdminController(AppDbContext db) => _db = db;

    // Kun brugere fra egen restaurant
    public IActionResult Index()
    {
        if (User.FindFirst("IsAdmin")?.Value != "True") return Forbid();

        var restaurantId = int.Parse(User.FindFirst("RestaurantId")!.Value);
        var users = _db.Users
                       .Include(u => u.TimeEntries)
                       .Where(u => u.RestaurantId == restaurantId)
                       .ToList();
        return View(users);
    }

    public IActionResult CreateUser() => View();

    [HttpPost]
    public async Task<IActionResult> CreateUser(User model)
    {
        if (User.FindFirst("IsAdmin")?.Value != "True") return Forbid();

        // ? KORREKT: Hash password før det gemmes
        model.RestaurantId = int.Parse(User.FindFirst("RestaurantId")!.Value);

        var passwordHasher = new PasswordHasher<User>();
        model.PasswordHash = passwordHasher.HashPassword(model, model.PasswordHash); // Forventer password i PasswordHash feltet

        _db.Users.Add(model);
        await _db.SaveChangesAsync();
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