using AdmissionProcessDAL.Models;

namespace AdmissionProcessDAL.Repositories.Interfaces;

public interface IUserProgressRepository
{
    Task<UserProgress> GetOrCreateUserProgressAsync(string userId);
    Task<UserProgress> UpdateUserProgressAsync(UserProgress progress);
    Task<UserProgress?> GetUserProgressAsync(string userId);
}
