using Microsoft.Extensions.Logging;

namespace AdmissionProcessDAL.Services;

public class MockEmailService : IEmailService
{
    private readonly ILogger<MockEmailService> _logger;

    public MockEmailService(ILogger<MockEmailService> logger)
    {
        _logger = logger;
    }

    public async Task SendVerificationEmailAsync(string email, string userId, string verificationToken)
    {
        // Mock implementation - in production, this would send a real email
        _logger.LogInformation(
            "Sending verification email to {Email} for user {UserId}. Token: {Token}",
            email, userId, verificationToken);
        
        // Simulate email sending
        await Task.Delay(100);
        
        _logger.LogInformation("Verification email sent successfully to {Email}", email);
    }
}
