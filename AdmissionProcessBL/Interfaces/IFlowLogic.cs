using AdmissionProcessModels.DTOs;

namespace AdmissionProcessBL.Interfaces;

public interface IFlowLogic
{
    Task<LogicResult<FlowResponse>> GetEntireFlowForUserAsync(string userId);
}
