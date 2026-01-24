using MasterschoolExercise.Models;
using MasterschoolExercise.Models.DTOs;
using MasterschoolExercise.Repositories;

namespace MasterschoolExercise.Services;

public class TaskManagementService : ITaskManagementService
{
    private readonly IFlowTaskRepository _taskRepository;
    private readonly IStepTaskRepository _stepTaskRepository;
    private readonly IUserTaskAssignmentRepository _userTaskAssignmentRepository;

    public TaskManagementService(
        IFlowTaskRepository taskRepository,
        IStepTaskRepository stepTaskRepository,
        IUserTaskAssignmentRepository userTaskAssignmentRepository)
    {
        _taskRepository = taskRepository;
        _stepTaskRepository = stepTaskRepository;
        _userTaskAssignmentRepository = userTaskAssignmentRepository;
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
        return await _taskRepository.CreateTaskAsync(task);
    }

    public async Task<FlowTask> UpdateTaskAsync(int taskId, UpdateTaskRequest request)
    {
        var task = await _taskRepository.GetTaskByIdAsync(taskId);
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

        return await _taskRepository.UpdateTaskAsync(task);
    }

    public async Task<bool> DeleteTaskAsync(int taskId)
    {
        return await _taskRepository.DeleteTaskAsync(taskId);
    }

    public async Task<FlowTask?> GetTaskAsync(int taskId)
    {
        return await _taskRepository.GetTaskByIdAsync(taskId);
    }

    public async Task<List<FlowTask>> GetAllTasksAsync()
    {
        return await _taskRepository.GetAllTasksAsync();
    }

    public async Task<bool> AssignTaskToStepAsync(int stepId, int taskId, int order, bool isRequired = true)
    {
        await _stepTaskRepository.AssignTaskToStepAsync(stepId, taskId, order, isRequired);
        return true;
    }

    public async Task<bool> RemoveTaskFromStepAsync(int stepId, int taskId)
    {
        return await _stepTaskRepository.RemoveTaskFromStepAsync(stepId, taskId);
    }

    public async Task<bool> AssignTaskToUserAsync(string userId, int taskId)
    {
        await _userTaskAssignmentRepository.AssignTaskToUserAsync(userId, taskId);
        return true;
    }

    public async Task<bool> RemoveTaskFromUserAsync(string userId, int taskId)
    {
        return await _userTaskAssignmentRepository.RemoveTaskFromUserAsync(userId, taskId);
    }
}
