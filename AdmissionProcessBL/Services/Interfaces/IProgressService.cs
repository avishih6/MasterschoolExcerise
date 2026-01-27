using AdmissionProcessModels.DTOs;

namespace AdmissionProcessBL.Services.Interfaces;

public interface IProgressService
{
    Task<ServiceResult<CurrentProgressResponse>> GetCurrentStepAndTaskForUserAsync(string userId);
    Task<ServiceResult> CompleteStepAsync(string userId, string stepName, Dictionary<string, object> payload);
}
