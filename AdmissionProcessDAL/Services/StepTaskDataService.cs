using AdmissionProcessDAL.Models;

namespace AdmissionProcessDAL.Services;

public class StepTaskDataService : IStepTaskDataService
{
    // Mock database - all storage in DAL services
    private readonly Dictionary<int, StepTask> _stepTasks = new();
    private readonly Dictionary<string, StepTask> _stepTaskLookup = new(); // Key: "stepId_taskId"
    private int _nextId = 1;

    public async Task<StepTask> AssignTaskToStepAsync(int stepId, int taskId, int order, bool isRequired = true)
    {
        var key = $"{stepId}_{taskId}";
        if (_stepTaskLookup.TryGetValue(key, out var existing))
        {
            existing.Order = order;
            existing.IsRequired = isRequired;
            return await Task.FromResult(existing);
        }

        var stepTask = new StepTask
        {
            Id = _nextId++,
            StepId = stepId,
            TaskId = taskId,
            Order = order,
            IsRequired = isRequired,
            CreatedAt = DateTime.UtcNow
        };

        _stepTasks[stepTask.Id] = stepTask;
        _stepTaskLookup[key] = stepTask;
        return await Task.FromResult(stepTask);
    }

    public async Task<StepTask?> GetStepTaskAsync(int stepId, int taskId)
    {
        _stepTaskLookup.TryGetValue($"{stepId}_{taskId}", out var stepTask);
        return await Task.FromResult(stepTask);
    }

    public async Task<List<int>> GetTaskIdsForStepAsync(int stepId)
    {
        var taskIds = _stepTasks.Values
            .Where(st => st.StepId == stepId)
            .OrderBy(st => st.Order)
            .Select(st => st.TaskId)
            .ToList();
        return await Task.FromResult(taskIds);
    }

    public async Task<List<int>> GetStepIdsForTaskAsync(int taskId)
    {
        var stepIds = _stepTasks.Values
            .Where(st => st.TaskId == taskId)
            .Select(st => st.StepId)
            .ToList();
        return await Task.FromResult(stepIds);
    }

    public async Task<bool> RemoveTaskFromStepAsync(int stepId, int taskId)
    {
        var key = $"{stepId}_{taskId}";
        if (_stepTaskLookup.TryGetValue(key, out var stepTask))
        {
            _stepTasks.Remove(stepTask.Id);
            _stepTaskLookup.Remove(key);
            return await Task.FromResult(true);
        }
        return await Task.FromResult(false);
    }

    public async Task<bool> UpdateStepTaskOrderAsync(int stepId, int taskId, int newOrder)
    {
        var key = $"{stepId}_{taskId}";
        if (_stepTaskLookup.TryGetValue(key, out var stepTask))
        {
            stepTask.Order = newOrder;
            return await Task.FromResult(true);
        }
        return await Task.FromResult(false);
    }
}
