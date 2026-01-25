using AdmissionProcessDAL.Models;
using AdmissionProcessDAL.Services;
using AdmissionProcessApi.Models.DTOs;

namespace AdmissionProcessApi.Services;

public class TaskManagementService : ITaskManagementService
{
    private readonly ITaskDataService _taskDataService;
    private readonly IStepTaskDataService _stepTaskDataService;
    private readonly IUserTaskAssignmentDataService _userTaskAssignmentDataService;

    public TaskManagementService(
        ITaskDataService taskDataService,
        IStepTaskDataService stepTaskDataService,
        IUserTaskAssignmentDataService userTaskAssignmentDataService)
    {
        _taskDataService = taskDataService;
        _stepTaskDataService = stepTaskDataService;
        _userTaskAssignmentDataService = userTaskAssignmentDataService;
    }

    public async Task<FlowTask> CreateTaskAsync(CreateTaskRequest request)
    {
        var task = new FlowTask
        {
            Name = request.Name,
            Description = request.Description,
            PassingConditionType = request.PassingConditionType,
            PassingConditionConfig = request.PassingConditionConfig,
            ConditionalVisibilityType = request.ConditionalVisibilityType,
            ConditionalVisibilityConfig = request.ConditionalVisibilityConfig
        };
        return await _taskDataService.CreateTaskAsync(task);
    }

    public async Task<FlowTask> UpdateTaskAsync(int taskId, UpdateTaskRequest request)
    {
        var task = await _taskDataService.GetTaskByIdAsync(taskId);
        if (task == null)
            throw new KeyNotFoundException($"Task with ID {taskId} not found");

        if (request.Name != null)
            task.Name = request.Name;
        if (request.Description != null)
            task.Description = request.Description;
        if (request.PassingConditionType != null)
            task.PassingConditionType = request.PassingConditionType;
        if (request.PassingConditionConfig != null)
            task.PassingConditionConfig = request.PassingConditionConfig;
        if (request.ConditionalVisibilityType != null)
            task.ConditionalVisibilityType = request.ConditionalVisibilityType;
        if (request.ConditionalVisibilityConfig != null)
            task.ConditionalVisibilityConfig = request.ConditionalVisibilityConfig;
        if (request.IsActive.HasValue)
            task.IsActive = request.IsActive.Value;

        return await _taskDataService.UpdateTaskAsync(task);
    }

    public async Task<bool> DeleteTaskAsync(int taskId)
    {
        return await _taskDataService.DeleteTaskAsync(taskId);
    }

    public async Task<FlowTask?> GetTaskAsync(int taskId)
    {
        return await _taskDataService.GetTaskByIdAsync(taskId);
    }

    public async Task<List<FlowTask>> GetAllTasksAsync()
    {
        return await _taskDataService.GetAllTasksAsync();
    }

    public async Task<bool> AssignTaskToStepAsync(int stepId, int taskId, int order, bool isRequired = true)
    {
        await _stepTaskDataService.AssignTaskToStepAsync(stepId, taskId, order, isRequired);
        return true;
    }

    public async Task<bool> RemoveTaskFromStepAsync(int stepId, int taskId)
    {
        return await _stepTaskDataService.RemoveTaskFromStepAsync(stepId, taskId);
    }

    public async Task<bool> AssignTaskToUserAsync(string userId, int taskId)
    {
        await _userTaskAssignmentDataService.AssignTaskToUserAsync(userId, taskId);
        return true;
    }

    public async Task<bool> RemoveTaskFromUserAsync(string userId, int taskId)
    {
        return await _userTaskAssignmentDataService.RemoveTaskFromUserAsync(userId, taskId);
    }
}
