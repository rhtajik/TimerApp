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
    db.Database.Migrate();  // ? Kør migrations
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

    // Seed users - KUN hvis tabellen er tom
    if (!db.Users.Any())
    {
        // I Program.cs - Området hvor du "sikrer kendte passwords"
        var passwordHasher = new PasswordHasher<User>();

        // Definer kendte brugere (tilpas med dine rigtige passwords)
        var knownUsers = new[]
        {
    (Email: "admin.bh@rh.dk", Password: "AdminBH123"),
    (Email: "user.bh@rh.dk", Password: "UserBH123"),
    (Email: "admin.p@rh.dk", Password: "AdminP123"),
    (Email: "user.p@rh.dk", Password: "UserP123"),
    (Email: "admin.t@rh.dk", Password: "AdminT123"),
    (Email: "user.t@rh.dk", Password: "UserT123")
};

        foreach (var (Email, Password) in knownUsers)
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u.Email == Email);
            if (user == null) continue; // Hop over hvis bruger ikke findes

            try
            {
                // ? Tjek om hash er tom
                if (string.IsNullOrWhiteSpace(user.PasswordHash))
                {
                    throw new FormatException("Tom hash");
                }

                // ? Prøv at verificere
                var result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, Password);

                if (result == PasswordVerificationResult.Failed)
                {
                    Console.WriteLine($"?? Opdaterer password for {Email}");
                    user.PasswordHash = passwordHasher.HashPassword(user, Password);
                    db.Users.Update(user);
                }
                else
                {
                    Console.WriteLine($"? Password OK for {Email}");
                }
            }
            catch (FormatException)
            {
                // ?? FANGER DIN FEJL OG FIKSER DEN!
                Console.WriteLine($"? UGYLDIG HASH for {Email} - genopretter!");
                user.PasswordHash = passwordHasher.HashPassword(user, Password);
                db.Users.Update(user);
            }
        }

        await db.SaveChangesAsync();
    }



    // ?? ONE-TIME FIX: Sørg for at alle passwords er korrekte
    Console.WriteLine("=== SIKRER KENDTE PASSWORDS ===");
    var fixHasher = new PasswordHasher<User>();
    var allUsers = db.Users.ToList();

    foreach (var u in allUsers)
    {
        var testPassword = u.IsAdmin ? "admin123" : "user123";

        // ? DEFENSIVT TJEK: Spring over hvis hash er tom eller ugyldig
        if (string.IsNullOrWhiteSpace(u.PasswordHash))
        {
            Console.WriteLine($"   ?? Manglende hash for {u.Email} - opretter ny!");
            u.PasswordHash = fixHasher.HashPassword(u, testPassword);
            continue;
        }

        PasswordVerificationResult verifyResult;
        try
        {
            verifyResult = fixHasher.VerifyHashedPassword(u, u.PasswordHash, testPassword);
        }
        catch (FormatException)
        {
            Console.WriteLine($"   ?? Ugyldig hash format for {u.Email} - reparerer!");
            u.PasswordHash = fixHasher.HashPassword(u, testPassword);
            continue;
        }

        if (verifyResult == PasswordVerificationResult.Failed)
        {
            u.PasswordHash = fixHasher.HashPassword(u, testPassword);
            Console.WriteLine($"   ?? Fixet password for {u.Email}");
        }
        else
        {
            Console.WriteLine($"   ? Password allerede korrekt for {u.Email}");
        }
    }
    db.SaveChanges();
    Console.WriteLine("=================================\n");




    // Debug output
    Console.WriteLine("\n=== ONLINE DEBUG: Aktive brugere ===");
    var debugUsers = db.Users.Include(u => u.Restaurant).ToList();
    foreach (var u in debugUsers)
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