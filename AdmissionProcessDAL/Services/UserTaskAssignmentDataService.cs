using AdmissionProcessDAL.Models;

namespace AdmissionProcessDAL.Services;

public class UserTaskAssignmentDataService : IUserTaskAssignmentDataService
{
    // Mock database - all storage in DAL services
    private readonly Dictionary<int, UserTaskAssignment> _assignments = new();
    private readonly Dictionary<string, List<int>> _userTasks = new(); // userId -> list of taskIds
    private int _nextId = 1;

    public async Task<UserTaskAssignment> AssignTaskToUserAsync(string userId, int taskId)
    {
        var existing = _assignments.Values.FirstOrDefault(a => 
            a.UserId == userId && a.TaskId == taskId && a.IsActive);
        
        if (existing != null)
        {
            return await Task.FromResult(existing);
        }

        var assignment = new UserTaskAssignment
        {
            Id = _nextId++,
            UserId = userId,
            TaskId = taskId,
            AssignedAt = DateTime.UtcNow,
            IsActive = true
        };

        _assignments[assignment.Id] = assignment;
        
        if (!_userTasks.ContainsKey(userId))
            _userTasks[userId] = new List<int>();
        _userTasks[userId].Add(taskId);

        return await Task.FromResult(assignment);
    }

    public async Task<bool> RemoveTaskFromUserAsync(string userId, int taskId)
    {
        var assignment = _assignments.Values.FirstOrDefault(a => 
            a.UserId == userId && a.TaskId == taskId && a.IsActive);
        
        if (assignment != null)
        {
            assignment.IsActive = false;
            if (_userTasks.ContainsKey(userId))
                _userTasks[userId].Remove(taskId);
            return await Task.FromResult(true);
        }
        return await Task.FromResult(false);
    }

    public async Task<bool> IsTaskAssignedToUserAsync(string userId, int taskId)
    {
        var exists = _assignments.Values.Any(a => 
            a.UserId == userId && a.TaskId == taskId && a.IsActive);
        return await Task.FromResult(exists);
    }

    public async Task<List<int>> GetTaskIdsForUserAsync(string userId)
    {
        if (_userTasks.TryGetValue(userId, out var taskIds))
        {
            return await Task.FromResult(taskIds);
        }
        return await Task.FromResult(new List<int>());
    }
}
