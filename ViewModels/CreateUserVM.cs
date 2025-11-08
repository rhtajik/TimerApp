using System.ComponentModel.DataAnnotations;

namespace TimerApp.ViewModels
{
    public class CreateUserVM
    {
        [Required(ErrorMessage = "Navn er påkrævet")]
        public string Name { get; set; } = "";

        [Required(ErrorMessage = "Email er påkrævet")]
        [EmailAddress(ErrorMessage = "Ugyldig email-format")]
        public string Email { get; set; } = "";

        [Display(Name = "Admin-bruger")]
        public bool IsAdmin { get; set; }
    }
}