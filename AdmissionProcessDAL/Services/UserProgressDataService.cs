using AdmissionProcessDAL.Models;

namespace AdmissionProcessDAL.Services;

public class UserProgressDataService : IUserProgressDataService
{
    // Mock database - all storage in DAL services
    private readonly Dictionary<string, UserProgress> _userProgress = new();

    public async Task<UserProgress> GetOrCreateUserProgressAsync(string userId)
    {
        if (!_userProgress.TryGetValue(userId, out var progress))
        {
            progress = new UserProgress
            {
                UserId = userId
            };
            _userProgress[userId] = progress;
        }

        return await Task.FromResult(progress);
    }

    public async Task<UserProgress?> GetUserProgressAsync(string userId)
    {
        _userProgress.TryGetValue(userId, out var progress);
        return await Task.FromResult(progress);
    }

    public async Task<UserProgress> UpdateUserProgressAsync(UserProgress progress)
    {
        _userProgress[progress.UserId] = progress;
        return await Task.FromResult(progress);
    }

    public async Task<bool> MarkStepCompletedAsync(string userId, string stepName, Dictionary<string, object> payload)
    {
        var progress = await GetOrCreateUserProgressAsync(userId);
        
        if (!progress.CompletedSteps.ContainsKey(stepName))
        {
            progress.CompletedSteps[stepName] = new StepCompletion
            {
                StepName = stepName,
                CompletedAt = DateTime.UtcNow,
                Payload = payload
            };
        }
        
        return await Task.FromResult(true);
    }

    public async Task<bool> MarkTaskCompletedAsync(string userId, string taskName, Dictionary<string, object> payload, bool passed)
    {
        var progress = await GetOrCreateUserProgressAsync(userId);
        
        if (!progress.CompletedTasks.ContainsKey(taskName))
        {
            progress.CompletedTasks[taskName] = new TaskCompletion
            {
                TaskName = taskName,
                CompletedAt = DateTime.UtcNow,
                Passed = passed,
                Payload = payload
            };
        }
        
        return await Task.FromResult(true);
    }
}
