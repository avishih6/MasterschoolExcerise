using AdmissionProcessBL.Interfaces;
using AdmissionProcessDAL.Repositories.Interfaces;
using AdmissionProcessModels.DTOs;
using Microsoft.Extensions.Logging;

namespace AdmissionProcessBL;

public class UserLogic : IUserLogic
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<UserLogic> _logger;

    public UserLogic(IUserRepository userRepository, ILogger<UserLogic> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<LogicResult<CreateUserResponse>> CreateUserAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            _logger.LogError("CreateUserAsync failed: email is null or empty");
            return LogicResult<CreateUserResponse>.Failure("Email is required");
        }

        var (user, alreadyExists) = await _userRepository.CreateUserAsync(email).ConfigureAwait(false);

        if (user == null)
        {
            _logger.LogError($"CreateUserAsync failed: unable to create user with email {email}");
            return LogicResult<CreateUserResponse>.Failure("Failed to create user");
        }

        if (alreadyExists)
        {
            _logger.LogInformation($"CreateUserAsync: user with email {email} already exists with ID {user.Id}");
            return LogicResult<CreateUserResponse>.Conflict(
                "User with this email already exists",
                new CreateUserResponse { UserId = user.Id });
        }

        _logger.LogInformation($"CreateUserAsync: successfully created user with ID {user.Id}");
        return LogicResult<CreateUserResponse>.Success(new CreateUserResponse { UserId = user.Id });
    }
}
