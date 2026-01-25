using AdmissionProcessDAL.Models;

namespace AdmissionProcessDAL.Repositories.Interfaces;

public interface IUserRepository
{
    Task<User> CreateUserAsync(string email);
    Task<User?> GetUserByIdAsync(string userId);
    Task<bool> UserExistsAsync(string userId);
    Task MarkEmailAsVerifiedAsync(string userId);
}
