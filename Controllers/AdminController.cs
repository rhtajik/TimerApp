using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TimerApp.Data;
using TimerApp.Filters;
using TimerApp.Models;
using TimerApp.Services;
using TimerApp.ViewModels;

namespace TimerApp.Controllers;

[RequireAdmin]
public class AdminController : Controller
{
    private readonly AppDbContext _db;
    private readonly PasswordGenerationService _passwordService;
    private readonly AuditService _auditService;

    public AdminController(AppDbContext db, PasswordGenerationService passwordService, AuditService auditService)
    {
        _db = db;
        _passwordService = passwordService;
        _auditService = auditService;
    }

    public IActionResult Index()
    {
        if (User.FindFirst("IsMainAdmin")?.Value == "True")
            return Forbid();

        var restaurantId = int.Parse(User.FindFirst("RestaurantId")!.Value);
        var users = _db.Users
                       .Include(u => u.TimeEntries)
                       .Where(u => u.RestaurantId == restaurantId && !u.IsMainAdmin)
                       .OrderBy(u => u.Name)
                       .ToList();
        return View(users);
    }

    public IActionResult CreateUser()
    {
        if (User.FindFirst("IsMainAdmin")?.Value == "True")
            return Forbid();

        return View(new CreateUserVM());
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser(CreateUserVM vm)
    {
        if (User.FindFirst("IsMainAdmin")?.Value == "True")
            return Forbid();

        if (!ModelState.IsValid)
            return View(vm);

        var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var restaurantId = int.Parse(User.FindFirst("RestaurantId")!.Value);

        if (await _db.Users.AnyAsync(u => u.Email.ToLower() == vm.Email.ToLower() && u.RestaurantId == restaurantId))
        {
            ModelState.AddModelError(nameof(vm.Email), "Email er allerede registreret i denne restaurant.");
            return View(vm);
        }

        var tempPassword = _passwordService.Generate();
        var passwordHasher = new PasswordHasher<User>();

        var newUser = new User
        {
            Name = vm.Name,
            Email = vm.Email,
            IsAdmin = vm.IsAdmin,
            RestaurantId = restaurantId,
            CreatedByUserId = currentUserId,
            CreatedAt = DateTime.UtcNow,
            CreatedByIp = HttpContext.Connection.RemoteIpAddress?.ToString(),
            MustChangePassword = true
        };

        newUser.PasswordHash = passwordHasher.HashPassword(newUser, tempPassword);
        _db.Users.Add(newUser);
        await _db.SaveChangesAsync();

        await _auditService.LogUserCreatedAsync(newUser.Id, currentUserId,
            HttpContext.Connection.RemoteIpAddress?.ToString(), tempPassword);
        await _auditService.SendTempPasswordEmailAsync(newUser, tempPassword);

        return Json(new { success = true, tempPassword, email = newUser.Email });
    }

    public async Task<IActionResult> ExportCsv(int year, int month)
    {
        if (User.FindFirst("IsMainAdmin")?.Value == "True")
            return Forbid();

        var restaurantId = int.Parse(User.FindFirst("RestaurantId")!.Value);
        var data = await _db.TimeEntries
                            .Include(t => t.User)
                            .Where(t => t.Date.Year == year && t.Date.Month == month && t.User.RestaurantId == restaurantId)
                            .OrderBy(t => t.User.Name).ThenBy(t => t.Date)
                            .ToListAsync();

        var csv = "Navn;Dato;Timer;Note\n" +
                  string.Join("\n", data.Select(t =>
                      $"{t.User.Name};{t.Date:yyyy-MM-dd};{t.Hours:F2};{t.Note ?? ""}"));

        return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv",
                    $"timer_{year}_{month}.csv");
    }

    // ? OPDATERET DELETE MED AUDIT LOGS
    [HttpPost]
    public async Task<IActionResult> DeleteUser(int id)
    {
        if (User.FindFirst("IsMainAdmin")?.Value == "True")
            return Forbid();

        var restaurantId = int.Parse(User.FindFirst("RestaurantId")!.Value);

        // **?? FØRST: FIND brugeren med alle relaterede data**
        var user = await _db.Users
            .Include(u => u.TimeEntries)
            .FirstOrDefaultAsync(u => u.Id == id && u.RestaurantId == restaurantId);

        if (user == null) return NotFound();

        // **?? ANDEN: SLET ALLE AUDIT LOGS (KRITISK!)**
        var userAuditLogs = _db.AuditLogs.Where(a => a.UserId == id);
        _db.AuditLogs.RemoveRange(userAuditLogs);

        // **?? TREDJE: SLET alle time entries**
        _db.TimeEntries.RemoveRange(user.TimeEntries);

        // **?? FJERDE: SLET brugeren**
        _db.Users.Remove(user);

        // **?? GEM ALT I EEN TRANSACTION**
        await _db.SaveChangesAsync();

        TempData["Success"] = "Medarbejder og alle relationer slettet.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> AuditLog()
    {
        if (User.FindFirst("IsMainAdmin")?.Value == "True")
            return Forbid();

        var restaurantId = int.Parse(User.FindFirst("RestaurantId")!.Value);
        var logs = await _db.AuditLogs
            .Include(a => a.User)
            .Include(a => a.PerformedByUser)
            .Where(a => a.User.RestaurantId == restaurantId)
            .OrderByDescending(a => a.Timestamp)
            .Take(100)
            .ToListAsync();

        return View(logs);
    }
}