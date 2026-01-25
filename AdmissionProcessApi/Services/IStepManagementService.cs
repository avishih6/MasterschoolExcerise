using AdmissionProcessDAL.Models;
using AdmissionProcessApi.Models.DTOs;

namespace AdmissionProcessApi.Services;

public interface IStepManagementService
{
    Task<Step> CreateStepAsync(CreateStepRequest request);
    Task<Step> UpdateStepAsync(int stepId, UpdateStepRequest request);
    Task<bool> DeleteStepAsync(int stepId);
    Task<Step?> GetStepAsync(int stepId);
    Task<List<Step>> GetAllStepsAsync();
}
