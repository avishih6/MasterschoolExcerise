using AdmissionProcessDAL.Models;
using AdmissionProcessDAL.Services;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace UserRegistrationService.DI.Services;

public class UserRegistrationProcessor : IUserRegistrationProcessor
{
    private readonly IUserDataService _userDataService;
    private readonly IEmailService _emailService;
    private readonly ILogger<UserRegistrationProcessor> _logger;

    public UserRegistrationProcessor(
        IUserDataService userDataService,
        IEmailService emailService,
        ILogger<UserRegistrationProcessor> logger)
    {
        _userDataService = userDataService;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task ProcessUserRegistrationAsync(UserRegistrationEvent registrationEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Processing user registration: UserId={UserId}, Email={Email}, CreatedAt={CreatedAt}",
            registrationEvent.UserId, registrationEvent.Email, registrationEvent.CreatedAt);

        try
        {
            // 1. Create user in database (via DAL - single source of truth)
            var user = await _userDataService.CreateUserAsync(registrationEvent.Email);
            _logger.LogInformation("User created in database via DAL: UserId={UserId}", user.Id);

            // 2. Generate verification token
            var verificationToken = GenerateVerificationToken(user.Id, user.Email);
            
            // 3. Send verification email
            await _emailService.SendVerificationEmailAsync(user.Email, user.Id, verificationToken);
            _logger.LogInformation("Verification email sent to {Email}", user.Email);

            // 4. Here you could also:
            // - Initialize user progress
            // - Trigger other workflows
            // - Send welcome notifications
        
            _logger.LogInformation(
                "User registration processed successfully: UserId={UserId}",
                user.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing user registration: UserId={UserId}", registrationEvent.UserId);
            throw;
        }
    }

    private string GenerateVerificationToken(string userId, string email)
    {
        // Generate a secure token
        var tokenData = $"{userId}:{email}:{DateTime.UtcNow:O}";
        var bytes = Encoding.UTF8.GetBytes(tokenData);
        var hash = SHA256.HashData(bytes);
        return Convert.ToBase64String(hash).Replace("+", "-").Replace("/", "_").TrimEnd('=');
    }
}
