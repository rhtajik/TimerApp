using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace TimerApp.ViewModels;

public class CreateUserVM
{
    [Required(ErrorMessage = "Navn er påkrævet")]
    public string Name { get; set; } = "";

    [Required(ErrorMessage = "Email er påkrævet")]
    [EmailAddress(ErrorMessage = "Ugyldig email-format")]
    public string Email { get; set; } = "";

    public bool IsAdmin { get; set; }

    [ValidateNever]
    public string TempPassword { get; set; } = "";
}