using AdmissionProcessDAL.Models;

namespace AdmissionProcessDAL.Services;

/// <summary>
/// Centralized service for all user data operations.
/// All data writing should go through this service.
/// </summary>
public interface IUserDataService
{
    Task<User> CreateUserAsync(string email);
    Task<User?> GetUserByIdAsync(string userId);
    Task<bool> UserExistsAsync(string userId);
    Task MarkEmailAsVerifiedAsync(string userId);
    Task<bool> IsEmailVerifiedAsync(string userId);
}
