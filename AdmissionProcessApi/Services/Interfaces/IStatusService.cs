using AdmissionProcessApi.Models.DTOs;

namespace AdmissionProcessApi.Services;

public interface IStatusService
{
    Task<ServiceResult<StatusResponse>> GetStatusAsync(string userId);
}
