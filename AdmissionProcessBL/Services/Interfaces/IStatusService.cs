using AdmissionProcessModels.DTOs;

namespace AdmissionProcessBL.Services.Interfaces;

public interface IStatusService
{
    Task<ServiceResult<StatusResponse>> GetUserStatusAsync(string userId);
}
