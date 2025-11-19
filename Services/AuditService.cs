using TimerApp.Data;
using TimerApp.Models;
using System.Text.Json; 

namespace TimerApp.Services;

public class AuditService
{
    private readonly AppDbContext _db;
    private readonly IServiceProvider _serviceProvider;

    public AuditService(AppDbContext db, IServiceProvider serviceProvider)
    {
        _db = db;
        _serviceProvider = serviceProvider;
    }

    public async Task LogUserCreatedAsync(int userId, int? createdByUserId, string? ipAddress, string tempPassword)
    {
        var log = new AuditLog
        {
            Action = "UserCreated",
            UserId = userId,
            PerformedByUserId = createdByUserId,
            IpAddress = ipAddress ?? "Unknown",
            Details = JsonSerializer.Serialize(new { tempPassword, note = "Bruger oprettet" })
        };

        _db.AuditLogs.Add(log);
        await _db.SaveChangesAsync();
    }

    public async Task LogPasswordChangedAsync(int userId, int? performedByUserId, string? ipAddress)
    {
        var log = new AuditLog
        {
            Action = "PasswordChanged",
            UserId = userId,
            PerformedByUserId = performedByUserId,
            IpAddress = ipAddress ?? "Unknown",
            Details = JsonSerializer.Serialize(new { note = "Password ændret" })
        };

        _db.AuditLogs.Add(log);
        await _db.SaveChangesAsync();
    }

    public async Task SendTempPasswordEmailAsync(User user, string tempPassword)
    {
        var emailService = _serviceProvider.GetRequiredService<EmailService>();
        await emailService.SendTempPasswordAsync(user, tempPassword);
    }
}