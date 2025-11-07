using System;
using System.ComponentModel.DataAnnotations;

namespace TimerApp.ViewModels;

public class TimeEntryVM
{
    [Required] public DateTime Date { get; set; }

    [Required] public TimeSpan StartTime { get; set; }
    [Required] public TimeSpan EndTime { get; set; }

    public string? Note { get; set; }
}