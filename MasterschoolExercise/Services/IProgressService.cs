using MasterschoolExercise.Models.DTOs;

namespace MasterschoolExercise.Services;

public interface IProgressService
{
    Task<UserProgressResponse> GetUserProgressAsync(string userId);
    Task CompleteStepAsync(CompleteStepRequest request);
    Task<UserStatusResponse> GetUserStatusAsync(string userId);
}
