namespace TimerApp.Models;

public class TimeEntry
{
    public int Id { get; set; }
    public int UserId { get; set; }

    public DateTime Date { get; set; }          // dato
    public TimeSpan StartTime { get; set; }     // 16:30
    public TimeSpan EndTime { get; set; }       // 20:30

    public string? Note { get; set; }

    public User User { get; set; } = null!;

    // Hjælpe-property: beregnede timer
  

  
    public decimal Hours => (decimal)(EndTime - StartTime).TotalHours;
}