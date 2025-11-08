using System.Collections.Generic;

namespace TimerApp.Models;

public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public bool IsAdmin { get; set; }
    public int RestaurantId { get; set; }
    public Restaurant Restaurant { get; set; } = null!;

    // ? NYT: Skal brugeren skifte password ved første login?
    public bool MustChangePassword { get; set; } = true;

    // ? NYT: Hvem oprettede brugeren (til sporbarhed)
    public int? CreatedByUserId { get; set; }

    public ICollection<TimeEntry> TimeEntries { get; set; } = new List<TimeEntry>();
}