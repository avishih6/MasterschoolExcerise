namespace AdmissionProcessApi.Services;

public interface IUserService
{
    Task<ServiceResult<string>> CreateUserAsync(string email);
}
