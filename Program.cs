using Microsoft.EntityFrameworkCore;
using TimerApp.Data;
using TimerApp.Models;
using Microsoft.AspNetCore.Identity;
using System;

var builder = WebApplication.CreateBuilder(args);

// Services
builder.Services.AddControllersWithViews();

// FIX: Konverter PostgreSQL URL til Npgsql format
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (connectionString != null && connectionString.StartsWith("postgres://"))
{
    var uri = new Uri(connectionString);
    var userInfo = uri.UserInfo.Split(':');
    connectionString = $"Host={uri.Host};Port={uri.Port};Database={uri.AbsolutePath.Substring(1)};Username={userInfo[0]};Password={userInfo[1]}";
}

builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseNpgsql(connectionString));

builder.Services.AddAuthentication("Cookies")
    .AddCookie("Cookies", o =>
    {
        o.LoginPath = "/Account/Login";
        o.AccessDeniedPath = "/Account/AccessDenied";
    });
builder.Services.AddAuthorization();

var app = builder.Build();

// Resten af din kode (behold din seeding)
Directory.CreateDirectory("App_Data");
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();

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
            var admin = new User
            {
                Name = $"Admin {r.Name}",
                Email = $"admin@{r.Name.Replace(" ", "").ToLower()}.dk",
                IsAdmin = true,
                RestaurantId = r.Id
            };
            admin.PasswordHash = passwordHasher.HashPassword(admin, "admin123");

            var user = new User
            {
                Name = $"Medarbejder {r.Name}",
                Email = $"user@{r.Name.Replace(" ", "").ToLower()}.dk",
                IsAdmin = false,
                RestaurantId = r.Id
            };
            user.PasswordHash = passwordHasher.HashPassword(user, "user123");

            db.Users.AddRange(admin, user);
        }
        db.SaveChanges();
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
app.Run();