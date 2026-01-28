using AdmissionProcessModels.DTOs;

namespace AdmissionProcessBL.Interfaces;

public interface IUserLogic
{
    Task<LogicResult<CreateUserResponse>> CreateUserAsync(string email);
}
