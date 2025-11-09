using Microsoft.AspNetCore.Mvc.Rendering;

namespace TimerApp.ViewModels;

public class CreateUserVM
{
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public bool IsAdmin { get; set; }
    public int RestaurantId { get; set; }
    public string TempPassword { get; set; } = ""; // 🎯 TILFØJET
    public List<SelectListItem> RestaurantList { get; set; } = new();
}