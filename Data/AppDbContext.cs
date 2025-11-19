using Microsoft.EntityFrameworkCore;
using TimerApp.Models;


namespace TimerApp.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> opts) : base(opts) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<TimeEntry> TimeEntries => Set<TimeEntry>();
    public DbSet<Restaurant> Restaurants => Set<Restaurant>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>(); // NY

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TimeEntry>().Ignore(t => t.Hours);

        // Sørg for at audit log referencer ikke giver cascade delete
        modelBuilder.Entity<AuditLog>()
            .HasOne(a => a.User)
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<AuditLog>()
            .HasOne(a => a.PerformedByUser)
            .WithMany()
            .HasForeignKey(a => a.PerformedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        base.OnModelCreating(modelBuilder);
    }
}