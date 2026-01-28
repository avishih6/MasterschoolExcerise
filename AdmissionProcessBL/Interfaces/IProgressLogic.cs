using AdmissionProcessModels.DTOs;

namespace AdmissionProcessBL.Interfaces;

public interface IProgressLogic
{
    Task<LogicResult<CurrentProgressResponse>> GetCurrentStepAndTaskForUserAsync(string userId);
    Task<LogicResult> CompleteStepAsync(string userId, string stepName, Dictionary<string, object> payload);
}
