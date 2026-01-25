using AdmissionProcessApi.Models.DTOs;

namespace AdmissionProcessApi.Services;

public interface IFlowService
{
    Task<FlowResponse> GetFlowAsync(string? userId = null);
}
