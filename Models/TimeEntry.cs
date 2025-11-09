using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace TimerApp.Models;

public class TimeEntry
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public DateTime Date { get; set; }

    // ?? SKAL være DateTime (ikke TimeSpan)
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; } // Nullable fordi den kan være tom

    public string? Note { get; set; }

    [ForeignKey("UserId")]
    public User User { get; set; } = null!;

    [NotMapped]
    public double Hours => EndTime.HasValue ? (EndTime.Value - StartTime).TotalHours : 0;
}