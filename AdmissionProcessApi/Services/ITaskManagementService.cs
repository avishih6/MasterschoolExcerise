using AdmissionProcessDAL.Models;
using AdmissionProcessApi.Models.DTOs;

namespace AdmissionProcessApi.Services;

public interface ITaskManagementService
{
    Task<FlowTask> CreateTaskAsync(CreateTaskRequest request);
    Task<FlowTask> UpdateTaskAsync(int taskId, UpdateTaskRequest request);
    Task<bool> DeleteTaskAsync(int taskId);
    Task<FlowTask?> GetTaskAsync(int taskId);
    Task<List<FlowTask>> GetAllTasksAsync();
    Task<bool> AssignTaskToStepAsync(int stepId, int taskId, int order, bool isRequired = true);
    Task<bool> RemoveTaskFromStepAsync(int stepId, int taskId);
    Task<bool> AssignTaskToUserAsync(string userId, int taskId);
    Task<bool> RemoveTaskFromUserAsync(string userId, int taskId);
}
