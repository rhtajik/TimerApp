using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace TimerApp.ViewModels;

public class LoginVM
{
    [Required] public string Email { get; set; } = "";
    [Required] public string Password { get; set; } = "";

    public int RestaurantId { get; set; }
    public List<SelectListItem>? RestaurantList { get; set; }
}