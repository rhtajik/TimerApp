namespace TimerApp.Models;

public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public bool IsAdmin { get; set; }
    public bool IsMainAdmin { get; set; } = false; // NY: Super admin flag
    public int RestaurantId { get; set; }
    public Restaurant Restaurant { get; set; } = null!;
    public bool MustChangePassword { get; set; } = true;
    public int? CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // NY: Tidsstempel
    public string? CreatedByIp { get; set; } // NY: IP-adresse

    public ICollection<TimeEntry> TimeEntries { get; set; } = new List<TimeEntry>();
}