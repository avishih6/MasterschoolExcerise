using AdmissionProcessDAL.Models;

namespace AdmissionProcessDAL.Repositories.Interfaces;

public interface IProgressRepository
{
    Task<UserProgress> GetOrCreateProgressAsync(string userId);
    Task<UserProgress?> GetProgressAsync(string userId);
    Task SaveProgressAsync(UserProgress progress);
}
