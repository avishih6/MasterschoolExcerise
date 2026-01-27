using AdmissionProcessDAL.Models;
using AdmissionProcessDAL.Repositories.Interfaces;

namespace AdmissionProcessDAL.Repositories.Mock;

public class MockUserRepository : IUserRepository
{
    private readonly Dictionary<string, User> _users = new();
    private int _nextId = 1;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public async Task<User> CreateUserAsync(string email)
    {
        await _lock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_users.Values.Any(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException($"User with email '{email}' already exists");
            }

            var id = Interlocked.Increment(ref _nextId).ToString();
            var user = new User
            {
                Id = id,
                Email = email,
                CreatedAt = DateTime.UtcNow
            };
            _users[user.Id] = user;
            return user;
        }
        finally
        {
            _lock.Release();
        }
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
