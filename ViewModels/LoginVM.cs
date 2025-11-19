using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace TimerApp.ViewModels;

public class LoginVM
{
    [Required(ErrorMessage = "Email er påkrævet")]
    [EmailAddress(ErrorMessage = "Ugyldig email-format")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Password er påkrævet")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = "";

    // **FJERNET [Required] - super admin behøver ikke vælge restaurant**
    public int RestaurantId { get; set; }

    [ValidateNever]
    public List<SelectListItem> RestaurantList { get; set; } = new();
}