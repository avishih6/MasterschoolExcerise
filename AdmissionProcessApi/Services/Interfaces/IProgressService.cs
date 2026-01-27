using AdmissionProcessApi.Models.DTOs;

namespace AdmissionProcessApi.Services;

public interface IProgressService
{
    Task<ServiceResult<CurrentProgressResponse>> GetCurrentProgressAsync(string userId);
    Task<ServiceResult> CompleteStepAsync(string userId, string stepName, Dictionary<string, object> payload);
}
