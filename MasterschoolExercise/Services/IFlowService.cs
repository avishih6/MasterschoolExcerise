using MasterschoolExercise.Models.DTOs;

namespace MasterschoolExercise.Services;

public interface IFlowService
{
    Task<FlowResponse> GetFlowAsync(string? userId = null);
}
