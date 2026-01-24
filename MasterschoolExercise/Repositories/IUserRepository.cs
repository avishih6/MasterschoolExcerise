using MasterschoolExercise.Models;

namespace MasterschoolExercise.Repositories;

public interface IUserRepository
{
    Task<User> CreateUserAsync(string email);
    Task<User?> GetUserByIdAsync(string userId);
    Task<bool> UserExistsAsync(string userId);
}
