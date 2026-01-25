using AdmissionProcessDAL.Models;

namespace AdmissionProcessDAL.Services;

public class TaskDataService : ITaskDataService
{
    // Mock database - all storage in DAL services
    private readonly Dictionary<int, FlowTask> _tasks = new();
    private int _nextId = 1;

    public async Task<FlowTask> CreateTaskAsync(FlowTask task)
    {
        task.Id = _nextId++;
        task.CreatedAt = DateTime.UtcNow;
        _tasks[task.Id] = task;
        return await Task.FromResult(task);
    }

    public async Task<FlowTask?> GetTaskByIdAsync(int taskId)
    {
        _tasks.TryGetValue(taskId, out var task);
        return await Task.FromResult(task);
    }

    public async Task<FlowTask?> GetTaskByNameAsync(string taskName)
    {
        var task = _tasks.Values.FirstOrDefault(t => 
            t.Name.Equals(taskName, StringComparison.OrdinalIgnoreCase));
        return await Task.FromResult(task);
    }

    public async Task<List<FlowTask>> GetAllTasksAsync()
    {
        return await Task.FromResult(_tasks.Values.ToList());
    }

    public async Task<List<FlowTask>> GetActiveTasksAsync()
    {
        return await Task.FromResult(_tasks.Values.Where(t => t.IsActive).ToList());
    }

    public async Task<FlowTask> UpdateTaskAsync(FlowTask task)
    {
        if (!_tasks.ContainsKey(task.Id))
            throw new KeyNotFoundException($"Task with ID {task.Id} not found");
        
        task.UpdatedAt = DateTime.UtcNow;
        _tasks[task.Id] = task;
        return await Task.FromResult(task);
    }

    public async Task<bool> DeleteTaskAsync(int taskId)
    {
        if (_tasks.TryGetValue(taskId, out var task))
        {
            task.IsActive = false;
            task.UpdatedAt = DateTime.UtcNow;
            return await Task.FromResult(true);
        }
        return await Task.FromResult(false);
    }
}
