using AdmissionProcessDAL.Models;
using AdmissionProcessDAL.Repositories.Interfaces;

namespace AdmissionProcessDAL.Repositories.Mock;

public class MockProgressRepository : IProgressRepository
{
    private readonly Dictionary<string, UserProgress> _progress = new();

    public Task<UserProgress> GetOrCreateProgressAsync(string userId)
    {
        if (!_progress.TryGetValue(userId, out var progress))
        {
            progress = new UserProgress 
            { 
                UserId = userId,
                CachedOverallStatus = ProgressStatus.NotStarted,
                CacheUpdatedAt = DateTime.UtcNow
            };
            _progress[userId] = progress;
        }
        return Task.FromResult(progress);
    }

    public Task<UserProgress?> GetProgressAsync(string userId)
    {
        _progress.TryGetValue(userId, out var progress);
        return Task.FromResult(progress);
    }

    public Task SaveProgressAsync(UserProgress progress)
    {
        _progress[progress.UserId] = progress;
        return Task.CompletedTask;
    }
}
