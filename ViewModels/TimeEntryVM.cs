using System;
using System.ComponentModel.DataAnnotations;

namespace TimerApp.ViewModels;

public class TimeEntryVM
{
    [Required]
    public DateTime Date { get; set; }

    [Required]
    public DateTime StartTime { get; set; } // ?? DATETIME IKKE TIMESPAN

    [Required]
    public DateTime EndTime { get; set; } // ?? DATETIME IKKE TIMESPAN

    public string? Note { get; set; }
}