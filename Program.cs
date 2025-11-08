using Microsoft.EntityFrameworkCore;
using TimerApp.Data;
using TimerApp.Models;
using Microsoft.AspNetCore.Identity;
using System;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// Services
builder.Services.AddControllersWithViews();

// FIX: Tjek både postgres:// og postgresql://
var rawConnString = builder.Configuration.GetConnectionString("DefaultConnection");
var connectionString = rawConnString;

if (!string.IsNullOrWhiteSpace(rawConnString))
{
    Console.WriteLine($"=== DEBUG: Raw connection string ===");
    Console.WriteLine($"Value: {rawConnString}");

    // Konverter URL til Npgsql format (både postgres:// og postgresql://)
    if (rawConnString.StartsWith("postgres://") || rawConnString.StartsWith("postgresql://"))
    {
        try
        {
            var uri = new Uri(rawConnString);
            var userInfo = uri.UserInfo.Split(':', 2);

            // Nogle URL'er har ikke port - brug default 5432
            var port = uri.Port > 0 ? uri.Port : 5432;

            // Fjern leading slash fra databasenavn
            var database = uri.AbsolutePath.Substring(1);

            connectionString = $"Host={uri.Host};Port={port};Database={database};Username={userInfo[0]};Password={userInfo[1]}";
            Console.WriteLine($"=== Konverteret til: {connectionString}");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"? URL parse fejl: {ex.Message}", ex);
        }
    }
    else
    {
        Console.WriteLine($"=== Bruger direkte: {rawConnString}");
    }
}
else
    throw new InvalidOperationException("? Connection string mangler!");

builder.Services.AddDbContext<AppDbContext>(o => o.UseNpgsql(connectionString));

// AUTHENTICATION med sikre cookies (online-ready)
builder.Services.AddAuthentication("Cookies")
    .AddCookie("Cookies", o =>
    {
        o.LoginPath = "/Account/Login";
        o.AccessDeniedPath = "/Account/AccessDenied";
        o.Cookie.HttpOnly = true;              // Sikkerhed: Kun HTTP, ikke JavaScript
        o.Cookie.SecurePolicy = CookieSecurePolicy.Always; // Kræv HTTPS
        o.Cookie.SameSite = SameSiteMode.Strict; // Beskyt mod CSRF
        o.ExpireTimeSpan = TimeSpan.FromHours(8);
    });

builder.Services.AddAuthorization();

var app = builder.Build();

Directory.CreateDirectory("App_Data");
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    Console.WriteLine("=== Running Migrations ===");
    db.Database.Migrate();  // ? KORREKT: Kør migrations
    Console.WriteLine("=== Migrations completed ===");

    // Seed restauranter
    if (!db.Restaurants.Any())
    {
        db.Restaurants.AddRange(
            new Restaurant { Name = "BurgerHytten" },
            new Restaurant { Name = "Pullokiska" },
            new Restaurant { Name = "Tervakoski" }
        );
        db.SaveChanges();
    }

    // Seed users
    if (!db.Users.Any())
    {
        var passwordHasher = new PasswordHasher<User>();
        var restaurants = db.Restaurants.ToList();

        foreach (var r in restaurants)
        {
            string abbreviation = r.Name.ToLower() switch
            {
                "burgerhytten" => "bh",
                "pullokiska" => "p",
                "tervakoski" => "t",
                _ => r.Name.Substring(0, 1).ToLower()
            };

            var admin = new User
            {
                Name = $"Admin {r.Name}",
                Email = $"admin.{abbreviation}@rh.dk",
                IsAdmin = true,
                RestaurantId = r.Id,
                MustChangePassword = false // Eksisterende admins skal ikke skifte
            };
            admin.PasswordHash = passwordHasher.HashPassword(admin, "admin123");

            var user = new User
            {
                Name = $"Medarbejder {r.Name}",
                Email = $"user.{abbreviation}@rh.dk",
                IsAdmin = false,
                RestaurantId = r.Id,
                MustChangePassword = false // Eksisterende brugere skal ikke skifte
            };
            user.PasswordHash = passwordHasher.HashPassword(user, "user123");

            db.Users.AddRange(admin, user);
        }
        db.SaveChanges();
    }

    // ?? ONLINE DEBUG: Vis alle brugere ved app start (SEK KUN ÉN GANG!)
    Console.WriteLine("\n=== ONLINE DEBUG: Aktive brugere ===");
    var allUsers = db.Users.Include(u => u.Restaurant).ToList();
    foreach (var u in allUsers)
    {
        Console.WriteLine($"ID:{u.Id} | Email: {u.Email} | Admin: {u.IsAdmin} | Restaurant: {u.Restaurant?.Name}");
    }
    Console.WriteLine("====================================\n");
}

// Pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
app.Run();