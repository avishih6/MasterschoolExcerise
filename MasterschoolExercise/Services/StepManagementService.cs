using MasterschoolExercise.Models;
using MasterschoolExercise.Models.DTOs;
using MasterschoolExercise.Repositories;

namespace MasterschoolExercise.Services;

public class StepManagementService : IStepManagementService
{
    private readonly IStepRepository _stepRepository;

    public StepManagementService(IStepRepository stepRepository)
    {
        _stepRepository = stepRepository;
    }

    public async Task<Step> CreateStepAsync(CreateStepRequest request)
    {
        var step = new Step
        {
            Name = request.Name,
            Order = request.Order
        };
        return await _stepRepository.CreateStepAsync(step);
    }

    public async Task<Step> UpdateStepAsync(int stepId, UpdateStepRequest request)
    {
        var step = await _stepRepository.GetStepByIdAsync(stepId);
        if (step == null)
            throw new KeyNotFoundException($"Step with ID {stepId} not found");

        if (request.Name != null)
            step.Name = request.Name;
        if (request.Order.HasValue)
            step.Order = request.Order.Value;
        if (request.IsActive.HasValue)
            step.IsActive = request.IsActive.Value;

        return await _stepRepository.UpdateStepAsync(step);
    }

    public async Task<bool> DeleteStepAsync(int stepId)
    {
        return await _stepRepository.DeleteStepAsync(stepId);
    }

    public async Task<Step?> GetStepAsync(int stepId)
    {
        return await _stepRepository.GetStepByIdAsync(stepId);
    }

    public async Task<List<Step>> GetAllStepsAsync()
    {
        return await _stepRepository.GetAllStepsAsync();
    }
}
