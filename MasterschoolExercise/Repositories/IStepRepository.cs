using MasterschoolExercise.Models;

namespace MasterschoolExercise.Repositories;

public interface IStepRepository
{
    Task<Step> CreateStepAsync(Step step);
    Task<Step?> GetStepByIdAsync(int stepId);
    Task<Step?> GetStepByNameAsync(string stepName);
    Task<List<Step>> GetAllStepsAsync();
    Task<List<Step>> GetActiveStepsAsync();
    Task<Step> UpdateStepAsync(Step step);
    Task<bool> DeleteStepAsync(int stepId);
}
