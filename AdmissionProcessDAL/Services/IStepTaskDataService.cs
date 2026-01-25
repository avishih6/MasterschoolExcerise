using AdmissionProcessDAL.Models;

namespace AdmissionProcessDAL.Services;

public interface IStepTaskDataService
{
    Task<StepTask> AssignTaskToStepAsync(int stepId, int taskId, int order, bool isRequired = true);
    Task<StepTask?> GetStepTaskAsync(int stepId, int taskId);
    Task<List<int>> GetTaskIdsForStepAsync(int stepId);
    Task<List<int>> GetStepIdsForTaskAsync(int taskId);
    Task<bool> RemoveTaskFromStepAsync(int stepId, int taskId);
    Task<bool> UpdateStepTaskOrderAsync(int stepId, int taskId, int newOrder);
}
