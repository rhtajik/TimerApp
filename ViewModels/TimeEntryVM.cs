using System;
using System.ComponentModel.DataAnnotations;

namespace TimerApp.ViewModels;

public class TimeEntryVM
{
    [Required]
    public DateTime Date { get; set; }

    [Required]
    public DateTime StartTime { get; set; } // ?? Ændret fra TimeSpan

    [Required]
    public DateTime EndTime { get; set; } // ?? Ændret fra TimeSpan

    public string? Note { get; set; }
}