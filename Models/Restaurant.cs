using TimerApp.Models;

public class Restaurant
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public ICollection<User> Users { get; set; } = new List<User>(); // TILFØJ DENNE
}