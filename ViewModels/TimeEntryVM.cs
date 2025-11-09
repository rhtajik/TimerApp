using System;
using System.ComponentModel.DataAnnotations;

namespace TimerApp.ViewModels;

public class TimeEntryVM
{
    [Required]
    public DateTime Date { get; set; }

    [Required]
    public DateTime StartTime { get; set; }

    [Required]
    public DateTime EndTime { get; set; }

    public string? Note { get; set; }
}