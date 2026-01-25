using AdmissionProcessDAL.Models;

namespace AdmissionProcessDAL.Services;

public class StepDataService : IStepDataService
{
    // Mock database - all storage in DAL services
    private readonly Dictionary<int, Step> _steps = new();
    private int _nextId = 1;

    public async Task<Step> CreateStepAsync(Step step)
    {
        step.Id = _nextId++;
        step.CreatedAt = DateTime.UtcNow;
        _steps[step.Id] = step;
        return await Task.FromResult(step);
    }

    public async Task<Step?> GetStepByIdAsync(int stepId)
    {
        _steps.TryGetValue(stepId, out var step);
        return await Task.FromResult(step);
    }

    public async Task<Step?> GetStepByNameAsync(string stepName)
    {
        var step = _steps.Values.FirstOrDefault(s => 
            s.Name.Equals(stepName, StringComparison.OrdinalIgnoreCase));
        return await Task.FromResult(step);
    }

    public async Task<List<Step>> GetAllStepsAsync()
    {
        return await Task.FromResult(_steps.Values.OrderBy(s => s.Order).ToList());
    }

    public async Task<List<Step>> GetActiveStepsAsync()
    {
        return await Task.FromResult(_steps.Values
            .Where(s => s.IsActive)
            .OrderBy(s => s.Order)
            .ToList());
    }

    public async Task<Step> UpdateStepAsync(Step step)
    {
        if (!_steps.ContainsKey(step.Id))
            throw new KeyNotFoundException($"Step with ID {step.Id} not found");
        
        step.UpdatedAt = DateTime.UtcNow;
        _steps[step.Id] = step;
        return await Task.FromResult(step);
    }

    public async Task<bool> DeleteStepAsync(int stepId)
    {
        if (_steps.TryGetValue(stepId, out var step))
        {
            step.IsActive = false;
            step.UpdatedAt = DateTime.UtcNow;
            return await Task.FromResult(true);
        }
        return await Task.FromResult(false);
    }
}
