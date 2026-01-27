using AdmissionProcessModels.DTOs;

namespace AdmissionProcessBL.Services.Interfaces;

public interface IUserService
{
    Task<ServiceResult<CreateUserResponse>> CreateUserAsync(string email);
}
