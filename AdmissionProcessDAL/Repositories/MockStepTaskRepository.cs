using AdmissionProcessDAL.Models;
using AdmissionProcessDAL.Repositories.Interfaces;

namespace AdmissionProcessDAL.Repositories;

public class MockStepTaskRepository : IStepTaskRepository
{
    private readonly Dictionary<int, StepTask> _stepTasks = new();
    private readonly Dictionary<string, StepTask> _stepTaskLookup = new(); // Key: "stepId_taskId"
    private int _nextId = 1;

    public Task<StepTask> AssignTaskToStepAsync(int stepId, int taskId, int order, bool isRequired = true)
    {
        var key = $"{stepId}_{taskId}";
        if (_stepTaskLookup.ContainsKey(key))
        {
            var existing = _stepTaskLookup[key];
            existing.Order = order;
            existing.IsRequired = isRequired;
            return Task.FromResult(existing);
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
        return Task.FromResult(stepTask);
    }

    public Task<bool> RemoveTaskFromStepAsync(int stepId, int taskId)
    {
        var key = $"{stepId}_{taskId}";
        if (_stepTaskLookup.TryGetValue(key, out var stepTask))
        {
            _stepTasks.Remove(stepTask.Id);
            _stepTaskLookup.Remove(key);
            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }

    public Task<List<int>> GetTaskIdsForStepAsync(int stepId)
    {
        var taskIds = _stepTasks.Values
            .Where(st => st.StepId == stepId)
            .OrderBy(st => st.Order)
            .Select(st => st.TaskId)
            .ToList();
        return Task.FromResult(taskIds);
    }

    public Task<List<int>> GetStepIdsForTaskAsync(int taskId)
    {
        var stepIds = _stepTasks.Values
            .Where(st => st.TaskId == taskId)
            .Select(st => st.StepId)
            .ToList();
        return Task.FromResult(stepIds);
    }

    public Task<List<FlowTask>> GetTasksForStepAsync(int stepId)
    {
        // This will need to be called with the task repository to get full task objects
        // For now, return empty - will be handled by service layer
        return Task.FromResult(new List<FlowTask>());
    }

    public Task<List<Step>> GetStepsForTaskAsync(int taskId)
    {
        // This will need to be called with the step repository to get full step objects
        // For now, return empty - will be handled by service layer
        return Task.FromResult(new List<Step>());
    }

    public Task<StepTask?> GetStepTaskAsync(int stepId, int taskId)
    {
        var key = $"{stepId}_{taskId}";
        _stepTaskLookup.TryGetValue(key, out var stepTask);
        return Task.FromResult(stepTask);
    }

    public Task<List<StepTask>> GetAllStepTasksAsync()
    {
        return Task.FromResult(_stepTasks.Values.ToList());
    }

    public Task<bool> UpdateStepTaskOrderAsync(int stepId, int taskId, int newOrder)
    {
        var key = $"{stepId}_{taskId}";
        if (_stepTaskLookup.TryGetValue(key, out var stepTask))
        {
            stepTask.Order = newOrder;
            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }
}
