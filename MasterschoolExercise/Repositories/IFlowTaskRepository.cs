using MasterschoolExercise.Models;

namespace MasterschoolExercise.Repositories;

public interface IFlowTaskRepository
{
    Task<FlowTask> CreateTaskAsync(FlowTask task);
    Task<FlowTask?> GetTaskByIdAsync(int taskId);
    Task<FlowTask?> GetTaskByNameAsync(string taskName);
    Task<List<FlowTask>> GetAllTasksAsync();
    Task<List<FlowTask>> GetActiveTasksAsync();
    Task<FlowTask> UpdateTaskAsync(FlowTask task);
    Task<bool> DeleteTaskAsync(int taskId);
}
