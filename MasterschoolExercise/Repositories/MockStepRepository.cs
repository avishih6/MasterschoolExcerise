using MasterschoolExercise.Models;

namespace MasterschoolExercise.Repositories;

public class MockStepRepository : IStepRepository
{
    private readonly Dictionary<int, Step> _steps = new();
    private int _nextId = 1;

    public Task<Step> CreateStepAsync(Step step)
    {
        step.Id = _nextId++;
        step.CreatedAt = DateTime.UtcNow;
        _steps[step.Id] = step;
        return Task.FromResult(step);
    }

    public Task<Step?> GetStepByIdAsync(int stepId)
    {
        _steps.TryGetValue(stepId, out var step);
        return Task.FromResult(step);
    }

    public Task<Step?> GetStepByNameAsync(string stepName)
    {
        var step = _steps.Values.FirstOrDefault(s => 
            s.Name.Equals(stepName, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(step);
    }

    public Task<List<Step>> GetAllStepsAsync()
    {
        return Task.FromResult(_steps.Values.OrderBy(s => s.Order).ToList());
    }

    public Task<List<Step>> GetActiveStepsAsync()
    {
        return Task.FromResult(_steps.Values
            .Where(s => s.IsActive)
            .OrderBy(s => s.Order)
            .ToList());
    }

    public Task<Step> UpdateStepAsync(Step step)
    {
        if (!_steps.ContainsKey(step.Id))
            throw new KeyNotFoundException($"Step with ID {step.Id} not found");
        
        step.UpdatedAt = DateTime.UtcNow;
        _steps[step.Id] = step;
        return Task.FromResult(step);
    }

    public Task<bool> DeleteStepAsync(int stepId)
    {
        if (_steps.TryGetValue(stepId, out var step))
        {
            step.IsActive = false;
            step.UpdatedAt = DateTime.UtcNow;
            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }
}
