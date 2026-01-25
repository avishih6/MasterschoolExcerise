using AdmissionProcessDAL.Models;
using AdmissionProcessApi.Models.DTOs;

namespace AdmissionProcessApi.Services;

public interface IUserService
{
    Task<CreateUserResponse> CreateUserAsync(CreateUserRequest request);
}
