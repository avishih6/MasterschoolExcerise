using AdmissionProcessDAL.Models;

namespace AdmissionProcessDAL.Repositories.Interfaces;

public interface IUserRepository
{
    Task<(User? User, bool AlreadyExists)> CreateUserAsync(string email);
    Task<User?> GetUserByIdAsync(string userId);
    Task<bool> UserExistsAsync(string userId);
}
