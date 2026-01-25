using AdmissionProcessDAL.Models;

namespace AdmissionProcessDAL.Services;

public interface ITaskDataService
{
    Task<FlowTask> CreateTaskAsync(FlowTask task);
    Task<FlowTask?> GetTaskByIdAsync(int taskId);
    Task<FlowTask?> GetTaskByNameAsync(string taskName);
    Task<List<FlowTask>> GetAllTasksAsync();
    Task<List<FlowTask>> GetActiveTasksAsync();
    Task<FlowTask> UpdateTaskAsync(FlowTask task);
    Task<bool> DeleteTaskAsync(int taskId);
}
