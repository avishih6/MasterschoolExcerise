using AdmissionProcessDAL.Models;

namespace AdmissionProcessDAL.Services;

public interface IUserProgressDataService
{
    Task<UserProgress> GetOrCreateUserProgressAsync(string userId);
    Task<UserProgress?> GetUserProgressAsync(string userId);
    Task<UserProgress> UpdateUserProgressAsync(UserProgress progress);
    Task<bool> MarkStepCompletedAsync(string userId, string stepName, Dictionary<string, object> payload);
    Task<bool> MarkTaskCompletedAsync(string userId, string taskName, Dictionary<string, object> payload, bool passed);
}
