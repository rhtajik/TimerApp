using Microsoft.EntityFrameworkCore;
using TimerApp.Models;

namespace TimerApp.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> opts) : base(opts) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<TimeEntry> TimeEntries => Set<TimeEntry>();

    public DbSet<Restaurant> Restaurants => Set<Restaurant>();


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TimeEntry>().Ignore(t => t.Hours);
    }

}


