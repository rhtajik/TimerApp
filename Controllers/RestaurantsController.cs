using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TimerApp.Data;
using TimerApp.Models;
using TimerApp.Filters;
using TimerApp.Services;
using TimerApp.ViewModels.Restaurant;

namespace TimerApp.Controllers;

[RequireMainAdmin]
public class RestaurantsController : Controller
{
    private readonly AppDbContext _db;
    private readonly AuditService _auditService;
    private readonly PasswordHasher<User> _passwordHasher;

    public RestaurantsController(AppDbContext db, AuditService auditService, PasswordHasher<User> passwordHasher)
    {
        _db = db;
        _auditService = auditService;
        _passwordHasher = passwordHasher;
    }

    public async Task<IActionResult> Index()
    {
        var restaurants = await _db.Restaurants
            .Include(r => r.Users)
            .Select(r => new RestaurantVM
            {
                Id = r.Id,
                Name = r.Name,
                UserCount = r.Users.Count(u => !u.IsMainAdmin)
            })
            .ToListAsync();

        return View(restaurants);
    }

    public IActionResult Create() => View();

    [HttpPost]
    public async Task<IActionResult> Create(CreateRestaurantVM vm)
    {
        if (!ModelState.IsValid)
            return View(vm);

        // Rens restaurant navn til URL-venlig version
        var cleanName = new string(vm.Name
            .ToLower()
            .Replace(" ", "")
            .Where(c => char.IsLetterOrDigit(c))
            .ToArray());

        // Tjek om restaurant findes (både originalt navn og renset navn)
        if (await _db.Restaurants.AnyAsync(r =>
            r.Name.ToLower() == vm.Name.ToLower() ||
            r.Name.ToLower().Replace(" ", "") == cleanName))
        {
            ModelState.AddModelError(nameof(vm.Name), "Restaurant findes allerede.");
            return View(vm);
        }

        var restaurant = new Restaurant { Name = vm.Name };
        _db.Restaurants.Add(restaurant);
        await _db.SaveChangesAsync();

        TempData["Success"] = $"Restaurant '{restaurant.Name}' oprettet.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var restaurant = await _db.Restaurants.FindAsync(id);
        if (restaurant == null) return NotFound();

        return View(new CreateRestaurantVM { Name = restaurant.Name });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, CreateRestaurantVM vm)
    {
        if (!ModelState.IsValid)
            return View(vm);

        var restaurant = await _db.Restaurants.FindAsync(id);
        if (restaurant == null) return NotFound();

        if (await _db.Restaurants.AnyAsync(r => r.Name.ToLower() == vm.Name.ToLower() && r.Id != id))
        {
            ModelState.AddModelError(nameof(vm.Name), "Restaurant navn findes allerede.");
            return View(vm);
        }

        restaurant.Name = vm.Name;
        await _db.SaveChangesAsync();

        TempData["Success"] = "Restaurant opdateret.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var restaurant = await _db.Restaurants
            .Include(r => r.Users)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (restaurant == null) return NotFound();

        if (restaurant.Users.Any(u => !u.IsMainAdmin))
        {
            TempData["Error"] = "Kan ikke slette restaurant med tilknyttede brugere.";
            return RedirectToAction(nameof(Index));
        }

        _db.Restaurants.Remove(restaurant);
        await _db.SaveChangesAsync();

        TempData["Success"] = "Restaurant slettet.";
        return RedirectToAction(nameof(Index));
    }

    // ✅ OPDATERET METODE: Bedre email format
    public async Task<IActionResult> CreateAdmin(int restaurantId)
    {
        var restaurant = await _db.Restaurants.FindAsync(restaurantId);
        if (restaurant == null) return NotFound();

        // **Lav email baseret på restaurant navn (rens for specialtegn)**
        var cleanName = new string(restaurant.Name
            .ToLower()
            .Replace(" ", "")
            .Where(c => char.IsLetterOrDigit(c))
            .ToArray());

        var adminEmail = $"admin.{cleanName}@rh.dk"; // ✅ Eksempel: admin.burgerpalace@rh.dk

        // Tjek om admin allerede findes
        if (await _db.Users.AnyAsync(u => u.Email == adminEmail))
        {
            TempData["Error"] = $"Admin {adminEmail} findes allerede.";
            return RedirectToAction(nameof(Index));
        }

        var admin = new User
        {
            Name = $"{restaurant.Name} Administrator",
            Email = adminEmail,
            IsAdmin = true,
            IsMainAdmin = false,
            RestaurantId = restaurantId,
            MustChangePassword = false,
            CreatedAt = DateTime.UtcNow,
            CreatedByIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "System"
        };

        admin.PasswordHash = _passwordHasher.HashPassword(admin, "Admin123!");

        _db.Users.Add(admin);
        await _db.SaveChangesAsync();

        TempData["Success"] = $"Admin {admin.Email} oprettet med password: Admin123!";
        return RedirectToAction(nameof(Index));
    }
}