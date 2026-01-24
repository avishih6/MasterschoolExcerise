using MasterschoolExercise.Models;

namespace MasterschoolExercise.Repositories;

public interface IUserProgressRepository
{
    Task<UserProgress> GetOrCreateUserProgressAsync(string userId);
    Task<UserProgress> UpdateUserProgressAsync(UserProgress progress);
    Task<UserProgress?> GetUserProgressAsync(string userId);
}
