using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace TimerApp.ViewModels;

public class LoginVM
{
    [Required]
    [EmailAddress] // Validerer at det er en gyldig email
    public string Email { get; set; }

    [Required]
    [DataType(DataType.Password)] // Fortæller browseren det er et password-felt
    public string Password { get; set; }

    [Required]
    public int RestaurantId { get; set; }

    public List<SelectListItem> RestaurantList { get; set; }
}