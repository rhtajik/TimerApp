using TimerApp.Models;

namespace TimerApp.Services;

public class EmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendTempPasswordAsync(User user, string tempPassword)
    {
        // I produktion: Implementer rigtig SMTP her
        // For demo: Log til konsollen
        _logger.LogInformation("=== EMAIL SENDT TIL {Email} ===", user.Email);
        _logger.LogInformation("Restaurant: {Restaurant}", user.Restaurant?.Name ?? "N/A");
        _logger.LogInformation("Midlertidig adgangskode: {Password}", tempPassword);
        _logger.LogInformation("Udløber: {Expiry}", DateTime.UtcNow.AddHours(24));
        _logger.LogInformation("===================================");

        await Task.CompletedTask; // Simuler async operation
    }
}