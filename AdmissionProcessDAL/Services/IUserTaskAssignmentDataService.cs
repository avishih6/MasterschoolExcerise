using AdmissionProcessDAL.Models;

namespace AdmissionProcessDAL.Services;

public interface IUserTaskAssignmentDataService
{
    Task<UserTaskAssignment> AssignTaskToUserAsync(string userId, int taskId);
    Task<bool> RemoveTaskFromUserAsync(string userId, int taskId);
    Task<bool> IsTaskAssignedToUserAsync(string userId, int taskId);
    Task<List<int>> GetTaskIdsForUserAsync(string userId);
}
