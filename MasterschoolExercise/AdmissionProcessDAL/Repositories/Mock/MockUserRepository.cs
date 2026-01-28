using System.Collections.Concurrent;
using AdmissionProcessDAL.Models;
using AdmissionProcessDAL.Repositories.Interfaces;

namespace AdmissionProcessDAL.Repositories.Mock;

public class MockUserRepository : IUserRepository
{
    private readonly ConcurrentDictionary<string, User> _usersById = new();
    private readonly ConcurrentDictionary<string, User> _usersByEmail = new();
    private int _nextId;

    public Task<(User? User, bool AlreadyExists)> CreateUserAsync(string email)
    {
        var normalizedEmail = email.ToLowerInvariant();
        
        if (_usersByEmail.TryGetValue(normalizedEmail, out var existingUser))
        {
            return Task.FromResult<(User?, bool)>((existingUser, true));
        }

        var id = Interlocked.Increment(ref _nextId).ToString();
        var user = new User
        {
            Id = id,
            Email = email,
            CreatedAt = DateTime.UtcNow
        };

        if (_usersByEmail.TryAdd(normalizedEmail, user))
        {
            _usersById[id] = user;
            return Task.FromResult<(User?, bool)>((user, false));
        }

        if (_usersByEmail.TryGetValue(normalizedEmail, out existingUser))
        {
            return Task.FromResult<(User?, bool)>((existingUser, true));
        }

        return Task.FromResult<(User?, bool)>((null, false));
    }

    public Task<User?> GetUserByIdAsync(string userId)
    {
        _usersById.TryGetValue(userId, out var user);
        return Task.FromResult(user);
    }

    public Task<bool> UserExistsAsync(string userId)
    {
        return Task.FromResult(_usersById.ContainsKey(userId));
    }
}
