using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using TimerApp.Data;
using TimerApp.Models;
using TimerApp.ViewModels;

namespace TimerApp.Controllers;

[Authorize]
public class TimeEntriesController : Controller
{
    private readonly AppDbContext _db;
    public TimeEntriesController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var uid = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        var list = await _db.TimeEntries
                            .Where(t => t.UserId == uid)
                            .OrderByDescending(t => t.Date)
                            .ToListAsync();
        return View(list);
    }

    public IActionResult Create() => View(new TimeEntryVM { Date = DateTime.Today });

    [HttpPost]
    public async Task<IActionResult> Create(TimeEntryVM vm)
    {
        if (!ModelState.IsValid)
        {
            // Log fejl for debugging
            Console.WriteLine("! ModelState invalid: " + string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
            return View(vm);
        }

        var uid = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);

        // KOMBINER dato og tid korrekt
        var startDateTime = vm.Date.Date.Add(vm.StartTime.TimeOfDay);
        var endDateTime = vm.Date.Date.Add(vm.EndTime.TimeOfDay);

        _db.TimeEntries.Add(new TimeEntry
        {
            UserId = uid,
            Date = vm.Date.Date,
            StartTime = DateTime.SpecifyKind(startDateTime, DateTimeKind.Utc),
            EndTime = DateTime.SpecifyKind(endDateTime, DateTimeKind.Utc),
            Note = vm.Note
        });

        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> MonthlySum()
    {
        var uid = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        var year = DateTime.Now.Year;
        var month = DateTime.Now.Month;

        var entries = await _db.TimeEntries
                               .Where(t => t.UserId == uid && t.Date.Year == year && t.Date.Month == month)
                               .ToListAsync();

        decimal sum = entries.Sum(t => (decimal)(t.EndTime.HasValue ? (t.EndTime.Value - t.StartTime).TotalHours : 0));
        ViewBag.Sum = sum;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var uid = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        var entry = await _db.TimeEntries.FirstOrDefaultAsync(t => t.Id == id && t.UserId == uid);
        if (entry != null)
        {
            _db.TimeEntries.Remove(entry);
            await _db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }
}