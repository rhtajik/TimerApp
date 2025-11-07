using System;
using System.ComponentModel.DataAnnotations;

namespace TimerApp.ViewModels
{
    public class ChangePasswordVM
    {
        [Required]
        [DataType(DataType.Password)]
        public string OldPassword { get; set; } = "";

        [Required]
        [StringLength(100, ErrorMessage = "Minimum 6 tegn.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; } = "";

        [Required]
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "De nye koder matcher ikke.")]
        public string ConfirmPassword { get; set; } = "";
    }
}