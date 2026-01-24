using MasterschoolExercise.Models;

namespace MasterschoolExercise.Repositories;

public class MockUserRepository : IUserRepository
{
    private readonly Dictionary<string, User> _users = new();
    private int _nextId = 1;

    public Task<User> CreateUserAsync(string email)
    {
        var user = new User
        {
            Id = _nextId++.ToString(),
            Email = email,
            CreatedAt = DateTime.UtcNow
        };

        _users[user.Id] = user;
        return Task.FromResult(user);
    }

    public Task<User?> GetUserByIdAsync(string userId)
    {
        _users.TryGetValue(userId, out var user);
        return Task.FromResult(user);
    }

    public Task<bool> UserExistsAsync(string userId)
    {
        return Task.FromResult(_users.ContainsKey(userId));
    }
}
