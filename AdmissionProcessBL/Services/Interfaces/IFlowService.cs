using AdmissionProcessModels.DTOs;

namespace AdmissionProcessBL.Services.Interfaces;

public interface IFlowService
{
    Task<ServiceResult<FlowResponse>> GetEntireFlowForUserAsync(string userId);
}
