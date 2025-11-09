using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace TimerApp.ViewModels;

public class LoginVM
{
    [Required(ErrorMessage = "Email er påkrævet")]
    [EmailAddress(ErrorMessage = "Ugyldig email-format")]
    [Display(Name = "Email")]
    public string Email { get; set; } = null!;

    [Required(ErrorMessage = "Password er påkrævet")]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = null!;

    [Required(ErrorMessage = "Vælg en restaurant")]
    [Display(Name = "Restaurant")]
    public int RestaurantId { get; set; }

    [ValidateNever] // ?? Dette er løsningen!
    public List<SelectListItem> RestaurantList { get; set; } = new();
}