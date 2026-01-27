using AdmissionProcessDAL.Models;
using AdmissionProcessDAL.Repositories.Interfaces;
using Microsoft.Extensions.Logging;

namespace AdmissionProcessApi.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<UserService> _logger;

    public UserService(IUserRepository userRepository, ILogger<UserService> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<ServiceResult<string>> CreateUserAsync(string email)
    {
        try
        {
            var user = await _userRepository.CreateUserAsync(email).ConfigureAwait(false);
            _logger.LogInformation("User created with ID: {UserId}", user.Id);
            return ServiceResult<string>.Success(user.Id);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to create user with email: {Email}", email);
            return ServiceResult<string>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error creating user with email: {Email}", email);
            return ServiceResult<string>.Failure("An unexpected error occurred while creating the user");
        }
    }
}
