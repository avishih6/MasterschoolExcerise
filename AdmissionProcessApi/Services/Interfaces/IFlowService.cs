using AdmissionProcessApi.Models.DTOs;

namespace AdmissionProcessApi.Services;

public interface IFlowService
{
    Task<ServiceResult<FlowResponse>> GetFlowForUserAsync(string userId);
}
