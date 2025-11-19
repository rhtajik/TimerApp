using System;

namespace TimerApp.Models;

public class AuditLog
{
    public int Id { get; set; }
    public string Action { get; set; } = ""; // "UserCreated", "PasswordChanged"
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public int? PerformedByUserId { get; set; }
    public User? PerformedByUser { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string IpAddress { get; set; } = "";
    public string Details { get; set; } = ""; // JSON med ekstra info
}