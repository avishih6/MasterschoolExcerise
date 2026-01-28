using AdmissionProcessModels.DTOs;

namespace AdmissionProcessBL.Interfaces;

public interface IStatusLogic
{
    Task<LogicResult<StatusResponse>> GetUserStatusAsync(string userId);
}
