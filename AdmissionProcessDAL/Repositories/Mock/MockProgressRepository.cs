using System.Collections.Concurrent;
using AdmissionProcessDAL.Models;
using AdmissionProcessDAL.Repositories.Interfaces;
using AdmissionProcessModels.Enums;

namespace AdmissionProcessDAL.Repositories.Mock;

public class MockProgressRepository : IProgressRepository
{
    private readonly ConcurrentDictionary<string, UserProgress> _progressByUserId = new();

    public Task<UserProgress> GetOrCreateProgressAsync(string userId)
    {
        var progress = _progressByUserId.GetOrAdd(userId, _ => new UserProgress
        {
            UserId = userId,
            CachedOverallStatus = UserStatus.InProgress,
            CacheUpdatedAt = DateTime.UtcNow
        });
        
        return Task.FromResult(progress);
    }

    public Task<UserProgress?> GetProgressAsync(string userId)
    {
        _progressByUserId.TryGetValue(userId, out var progress);
        return Task.FromResult(progress);
    }

    public Task SaveProgressAsync(UserProgress progress)
    {
        _progressByUserId[progress.UserId] = progress;
        return Task.CompletedTask;
    }
}
