using MasterschoolExercise.Models.DTOs;
using MasterschoolExercise.Repositories;

namespace MasterschoolExercise.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;

    public UserService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<CreateUserResponse> CreateUserAsync(CreateUserRequest request)
    {
        var user = await _userRepository.CreateUserAsync(request.Email);
        return new CreateUserResponse { UserId = user.Id };
    }
}
