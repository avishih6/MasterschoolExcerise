using AdmissionProcessDAL.Models;
using AdmissionProcessDAL.Services;
using AdmissionProcessApi.Configuration;
using AdmissionProcessApi.Models.DTOs;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace AdmissionProcessApi.Services;

public class UserService : IUserService
{
    private readonly IUserDataService _userDataService;
    private readonly IEmailService _emailService;
    private readonly IFeatureFlagService _featureFlagService;
    private readonly IServiceBusService _serviceBusService;
    private readonly ILogger<UserService> _logger;

    public UserService(
        IUserDataService userDataService,
        IEmailService emailService,
        IFeatureFlagService featureFlagService,
        IServiceBusService serviceBusService,
        ILogger<UserService> logger)
    {
        _userDataService = userDataService;
        _emailService = emailService;
        _featureFlagService = featureFlagService;
        _serviceBusService = serviceBusService;
        _logger = logger;
    }

    public async Task<CreateUserResponse> CreateUserAsync(CreateUserRequest request)
    {
        // Always use DAL service for user creation (single source of truth)
        var user = await _userDataService.CreateUserAsync(request.Email);
        _logger.LogInformation("User created via DAL: UserId={UserId}", user.Id);

        // If feature flag is enabled, send to Service Bus
        if (_featureFlagService.IsEnabled("ServiceBusUserRegistration"))
        {
            try
            {
                await _serviceBusService.SendUserRegistrationMessageAsync(user);
                _logger.LogInformation("User {UserId} registration sent to Service Bus", user.Id);
            }
            catch (Exception ex)
            {
                // Log error but don't fail the user creation
                _logger.LogError(ex, "Failed to send user registration to Service Bus for user {UserId}", user.Id);
            }
        }
        else
        {
            // Feature flag is off - handle registration directly (same logic as Service Bus consumer)
            _logger.LogDebug("Service Bus feature flag disabled - processing registration directly");
            
            try
            {
                // Generate verification token
                var verificationToken = GenerateVerificationToken(user.Id, user.Email);
                
                // Send verification email
                await _emailService.SendVerificationEmailAsync(user.Email, user.Id, verificationToken);
                _logger.LogInformation("Verification email sent to {Email}", user.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send verification email for user {UserId}", user.Id);
                // Don't fail user creation if email fails
            }
        }

        return new CreateUserResponse { UserId = user.Id };
    }

    private string GenerateVerificationToken(string userId, string email)
    {
        var tokenData = $"{userId}:{email}:{DateTime.UtcNow:O}";
        var bytes = Encoding.UTF8.GetBytes(tokenData);
        var hash = SHA256.HashData(bytes);
        return Convert.ToBase64String(hash).Replace("+", "-").Replace("/", "_").TrimEnd('=');
    }
}
