using AdmissionProcessDAL.Models;

namespace AdmissionProcessDAL.Repositories.Interfaces;

public interface IStepTaskRepository
{
    Task<StepTask> AssignTaskToStepAsync(int stepId, int taskId, int order, bool isRequired = true);
    Task<bool> RemoveTaskFromStepAsync(int stepId, int taskId);
    Task<List<int>> GetTaskIdsForStepAsync(int stepId);
    Task<List<int>> GetStepIdsForTaskAsync(int taskId);
    Task<List<FlowTask>> GetTasksForStepAsync(int stepId);
    Task<List<Step>> GetStepsForTaskAsync(int taskId);
    Task<StepTask?> GetStepTaskAsync(int stepId, int taskId);
    Task<List<StepTask>> GetAllStepTasksAsync();
    Task<bool> UpdateStepTaskOrderAsync(int stepId, int taskId, int newOrder);
}
