using MasterschoolExercise.Models;
using MasterschoolExercise.Models.DTOs;

namespace MasterschoolExercise.Services;

public interface IUserService
{
    Task<CreateUserResponse> CreateUserAsync(CreateUserRequest request);
}
