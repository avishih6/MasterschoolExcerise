using MasterschoolExercise.Models;

namespace MasterschoolExercise.Repositories;

public class MockUserTaskAssignmentRepository : IUserTaskAssignmentRepository
{
    private readonly Dictionary<int, UserTaskAssignment> _assignments = new();
    private readonly Dictionary<string, List<int>> _userTasks = new(); // userId -> list of taskIds
    private int _nextId = 1;

    public Task<UserTaskAssignment> AssignTaskToUserAsync(string userId, int taskId)
    {
        var key = $"{userId}_{taskId}";
        var existing = _assignments.Values.FirstOrDefault(a => 
            a.UserId == userId && a.TaskId == taskId && a.IsActive);
        
        if (existing != null)
        {
            return Task.FromResult(existing);
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

        return Task.FromResult(assignment);
    }

    public Task<bool> RemoveTaskFromUserAsync(string userId, int taskId)
    {
        var assignment = _assignments.Values.FirstOrDefault(a => 
            a.UserId == userId && a.TaskId == taskId && a.IsActive);
        
        if (assignment != null)
        {
            assignment.IsActive = false;
            if (_userTasks.ContainsKey(userId))
                _userTasks[userId].Remove(taskId);
            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }

    public Task<List<FlowTask>> GetTasksForUserAsync(string userId)
    {
        // This will need to be called with the task repository to get full task objects
        // For now, return empty - will be handled by service layer
        return Task.FromResult(new List<FlowTask>());
    }

    public Task<bool> IsTaskAssignedToUserAsync(string userId, int taskId)
    {
        var exists = _assignments.Values.Any(a => 
            a.UserId == userId && a.TaskId == taskId && a.IsActive);
        return Task.FromResult(exists);
    }

    public Task<List<UserTaskAssignment>> GetAllUserTaskAssignmentsAsync()
    {
        return Task.FromResult(_assignments.Values.Where(a => a.IsActive).ToList());
    }
}
