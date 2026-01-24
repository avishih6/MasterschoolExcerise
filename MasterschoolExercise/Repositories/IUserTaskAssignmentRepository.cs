using MasterschoolExercise.Models;

namespace MasterschoolExercise.Repositories;

public interface IUserTaskAssignmentRepository
{
    Task<UserTaskAssignment> AssignTaskToUserAsync(string userId, int taskId);
    Task<bool> RemoveTaskFromUserAsync(string userId, int taskId);
    Task<List<FlowTask>> GetTasksForUserAsync(string userId);
    Task<bool> IsTaskAssignedToUserAsync(string userId, int taskId);
    Task<List<UserTaskAssignment>> GetAllUserTaskAssignmentsAsync();
}
