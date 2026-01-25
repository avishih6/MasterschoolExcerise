using AdmissionProcessDAL.Models;
using AdmissionProcessDAL.Services;
using AdmissionProcessApi.Models.DTOs;

namespace AdmissionProcessApi.Services;

public class StepManagementService : IStepManagementService
{
    private readonly IStepDataService _stepDataService;

    public StepManagementService(IStepDataService stepDataService)
    {
        _stepDataService = stepDataService;
    }

    public async Task<Step> CreateStepAsync(CreateStepRequest request)
    {
        var step = new Step
        {
            Name = request.Name,
            Order = request.Order
        };
        return await _stepDataService.CreateStepAsync(step);
    }

    public async Task<Step> UpdateStepAsync(int stepId, UpdateStepRequest request)
    {
        var step = await _stepDataService.GetStepByIdAsync(stepId);
        if (step == null)
            throw new KeyNotFoundException($"Step with ID {stepId} not found");

        if (request.Name != null)
            step.Name = request.Name;
        if (request.Order.HasValue)
            step.Order = request.Order.Value;
        if (request.IsActive.HasValue)
            step.IsActive = request.IsActive.Value;

        return await _stepDataService.UpdateStepAsync(step);
    }

    public async Task<bool> DeleteStepAsync(int stepId)
    {
        return await _stepDataService.DeleteStepAsync(stepId);
    }

    public async Task<Step?> GetStepAsync(int stepId)
    {
        return await _stepDataService.GetStepByIdAsync(stepId);
    }

    public async Task<List<Step>> GetAllStepsAsync()
    {
        return await _stepDataService.GetAllStepsAsync();
    }
}
