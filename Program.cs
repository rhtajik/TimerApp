using Microsoft.EntityFrameworkCore;
using TimerApp.Data;
using TimerApp.Models;
using Microsoft.AspNetCore.Identity;
using TimerApp.Services;
using TimerApp.Filters;

var builder = WebApplication.CreateBuilder(args);

// Services
builder.Services.AddControllersWithViews();

// Database - PostgreSQL (tilpasset Render.com)
string connectionString;

// PÅ RENDER: Brug DATABASE_URL (prioritet #1)
if (!string.IsNullOrEmpty(builder.Configuration["DATABASE_URL"]))
{
    var databaseUrl = builder.Configuration["DATABASE_URL"];
    var uri = new Uri(databaseUrl);
    var userInfo = uri.UserInfo.Split(':');
    connectionString = $"Host={uri.Host};Database={uri.AbsolutePath.Substring(1)};Username={userInfo[0]};Password={userInfo[1]};SSL Mode=Require;Trust Server Certificate=true";
    Console.WriteLine($"DEBUG: Using Render DATABASE_URL - Host={uri.Host}");
}
// LOKALT: Brug appsettings.json (prioritet #2)
else
{
    connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrWhiteSpace(connectionString))
        throw new InvalidOperationException("? Connection string mangler! Set DATABASE_URL på Render eller DefaultConnection i appsettings.json");

    Console.WriteLine($"DEBUG: Using local appsettings.json");
}

builder.Services.AddDbContext<AppDbContext>(o => o.UseNpgsql(connectionString));
builder.Services.AddScoped<PasswordHasher<User>>();

// Services
builder.Services.AddSingleton<EmailService>();
builder.Services.AddScoped<AuditService>();
builder.Services.AddSingleton<PasswordGenerationService>();

// Authentication
builder.Services.AddAuthentication("Cookies")
    .AddCookie("Cookies", o =>
    {
        o.LoginPath = "/Account/Login";
        o.AccessDeniedPath = "/Account/AccessDenied";
        o.Cookie.HttpOnly = true;
        o.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        o.Cookie.SameSite = SameSiteMode.Strict;
        o.ExpireTimeSpan = TimeSpan.FromHours(8);
    });

builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// Database seeding
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var hasher = scope.ServiceProvider.GetRequiredService<PasswordHasher<User>>();

    Console.WriteLine("=== Kører migrationer ===");
    db.Database.Migrate();
    Console.WriteLine("=== Migrationer færdige ===");

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

    // Seed Super Admin
    if (!db.Users.Any(u => u.Email == "superadmin@rh.dk"))
    {
        Console.WriteLine("=== Seeder Super Admin ===");
        var superAdmin = new User
        {
            Name = "Super Administrator",
            Email = "superadmin@rh.dk",
            IsAdmin = true,
            IsMainAdmin = true,
            RestaurantId = db.Restaurants.First().Id,
            MustChangePassword = false,
            CreatedAt = DateTime.UtcNow,
            CreatedByIp = "System"
        };

        superAdmin.PasswordHash = hasher.HashPassword(superAdmin, "SuperAdmin2025!");
        db.Users.Add(superAdmin);
        db.SaveChanges();
        Console.WriteLine("? Super Admin oprettet");
    }

    // Debug output
    Console.WriteLine("\n=== Aktive brugere ===");
    foreach (var u in db.Users.Include(u => u.Restaurant))
    {
        Console.WriteLine($"{u.Email} | Admin: {u.IsAdmin} | Main: {u.IsMainAdmin} | Restaurant: {u.Restaurant?.Name}");
    }
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

app.UseDefaultFiles();
app.UseStaticFiles();
app.Run();