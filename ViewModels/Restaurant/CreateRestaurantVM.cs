using System.ComponentModel.DataAnnotations;

namespace TimerApp.ViewModels.Restaurant;

public class CreateRestaurantVM
{
    [Required(ErrorMessage = "Restaurant navn er påkrævet")]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = "";
}