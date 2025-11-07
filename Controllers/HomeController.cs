using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TimerApp.Models;

namespace TimerApp.Controllers;

public class HomeController : Controller
{
    [Authorize]
    public IActionResult Index() => View();
}