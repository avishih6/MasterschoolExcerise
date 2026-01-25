using AdmissionProcessDAL.Models;
using AdmissionProcessDAL.Repositories.Interfaces;

namespace AdmissionProcessDAL.Repositories;

public class MockUserProgressRepository : IUserProgressRepository
{
    private readonly Dictionary<string, UserProgress> _userProgress = new();

    public Task<UserProgress> GetOrCreateUserProgressAsync(string userId)
    {
        if (!_userProgress.TryGetValue(userId, out var progress))
        {
            progress = new UserProgress
            {
                UserId = userId
            };
            _userProgress[userId] = progress;
        }

        return Task.FromResult(progress);
    }

    public Task<UserProgress> UpdateUserProgressAsync(UserProgress progress)
    {
        _userProgress[progress.UserId] = progress;
        return Task.FromResult(progress);
    }

    public Task<UserProgress?> GetUserProgressAsync(string userId)
    {
        _userProgress.TryGetValue(userId, out var progress);
        return Task.FromResult(progress);
    }
}
