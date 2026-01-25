using AdmissionProcessApi.Models.DTOs;

namespace AdmissionProcessApi.Services;

public interface IProgressService
{
    Task<UserProgressResponse> GetUserProgressAsync(string userId);
    Task CompleteStepAsync(CompleteStepRequest request);
    Task<UserStatusResponse> GetUserStatusAsync(string userId);
}
