using AdmissionProcessDAL.Models;
using AdmissionProcessDAL.Repositories.Interfaces;

namespace AdmissionProcessDAL.Repositories;

public class MockFlowTaskRepository : IFlowTaskRepository
{
    private readonly Dictionary<int, FlowTask> _tasks = new();
    private int _nextId = 1;

    public Task<FlowTask> CreateTaskAsync(FlowTask task)
    {
        task.Id = _nextId++;
        task.CreatedAt = DateTime.UtcNow;
        _tasks[task.Id] = task;
        return Task.FromResult(task);
    }

    public Task<FlowTask?> GetTaskByIdAsync(int taskId)
    {
        _tasks.TryGetValue(taskId, out var task);
        return Task.FromResult(task);
    }

    public Task<FlowTask?> GetTaskByNameAsync(string taskName)
    {
        var task = _tasks.Values.FirstOrDefault(t => 
            t.Name.Equals(taskName, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(task);
    }

    public Task<List<FlowTask>> GetAllTasksAsync()
    {
        return Task.FromResult(_tasks.Values.ToList());
    }

    public Task<List<FlowTask>> GetActiveTasksAsync()
    {
        return Task.FromResult(_tasks.Values.Where(t => t.IsActive).ToList());
    }

    public Task<FlowTask> UpdateTaskAsync(FlowTask task)
    {
        if (!_tasks.ContainsKey(task.Id))
            throw new KeyNotFoundException($"Task with ID {task.Id} not found");
        
        task.UpdatedAt = DateTime.UtcNow;
        _tasks[task.Id] = task;
        return Task.FromResult(task);
    }

    public Task<bool> DeleteTaskAsync(int taskId)
    {
        if (_tasks.TryGetValue(taskId, out var task))
        {
            task.IsActive = false;
            task.UpdatedAt = DateTime.UtcNow;
            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }
}
