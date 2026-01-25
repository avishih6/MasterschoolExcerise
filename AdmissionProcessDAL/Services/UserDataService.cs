using AdmissionProcessDAL.Models;

namespace AdmissionProcessDAL.Services;

public class UserDataService : IUserDataService
{
    // Mock database - all storage in DAL services
    private readonly Dictionary<string, User> _users = new();
    private int _nextId = 1;

    public async Task<User> CreateUserAsync(string email)
    {
        var user = new User
        {
            Id = _nextId++.ToString(),
            Email = email,
            CreatedAt = DateTime.UtcNow
        };

        _users[user.Id] = user;
        return await Task.FromResult(user);
    }

    public async Task<User?> GetUserByIdAsync(string userId)
    {
        _users.TryGetValue(userId, out var user);
        return await Task.FromResult(user);
    }

    public async Task<bool> UserExistsAsync(string userId)
    {
        return await Task.FromResult(_users.ContainsKey(userId));
    }

    public async Task MarkEmailAsVerifiedAsync(string userId)
    {
        if (_users.TryGetValue(userId, out var user))
        {
            user.IsEmailVerified = true;
            user.EmailVerifiedAt = DateTime.UtcNow;
        }
        await Task.CompletedTask;
    }

    public async Task<bool> IsEmailVerifiedAsync(string userId)
    {
        var user = await GetUserByIdAsync(userId);
        return user?.IsEmailVerified ?? false;
    }
}
