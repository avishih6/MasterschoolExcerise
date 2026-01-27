using AdmissionProcessDAL.Repositories.Mock;
using Xunit;

namespace AdmissionProcessDAL.Tests;

public class MockUserRepositoryTests
{
    private readonly MockUserRepository _repository;

    public MockUserRepositoryTests()
    {
        _repository = new MockUserRepository();
    }

    [Fact]
    public async Task CreateUserAsync_WithValidEmail_ReturnsNewUser()
    {
        var email = "test@example.com";

        var (user, alreadyExists) = await _repository.CreateUserAsync(email);

        Assert.NotNull(user);
        Assert.False(alreadyExists);
        Assert.Equal(email, user.Email);
        Assert.False(string.IsNullOrEmpty(user.Id));
    }

    [Fact]
    public async Task CreateUserAsync_WithDuplicateEmail_ReturnsExistingUser()
    {
        var email = "duplicate@example.com";

        var (firstUser, firstExists) = await _repository.CreateUserAsync(email);
        var (secondUser, secondExists) = await _repository.CreateUserAsync(email);

        Assert.False(firstExists);
        Assert.True(secondExists);
        Assert.Equal(firstUser!.Id, secondUser!.Id);
    }

    [Fact]
    public async Task CreateUserAsync_ConcurrentCalls_DoNotCreateDuplicates()
    {
        var email = "concurrent@example.com";
        var tasks = new List<Task<(AdmissionProcessDAL.Models.User? User, bool AlreadyExists)>>();

        for (int i = 0; i < 10; i++)
        {
            tasks.Add(_repository.CreateUserAsync(email));
        }

        var results = await Task.WhenAll(tasks);

        var newUsers = results.Where(r => !r.AlreadyExists).ToList();
        var existingUsers = results.Where(r => r.AlreadyExists).ToList();

        Assert.Single(newUsers);
        Assert.Equal(9, existingUsers.Count);
        Assert.All(results, r => Assert.Equal(newUsers[0].User!.Id, r.User!.Id));
    }

    [Fact]
    public async Task GetUserByIdAsync_WithExistingUser_ReturnsUser()
    {
        var email = "existing@example.com";
        var (createdUser, _) = await _repository.CreateUserAsync(email);

        var user = await _repository.GetUserByIdAsync(createdUser!.Id);

        Assert.NotNull(user);
        Assert.Equal(createdUser.Id, user.Id);
        Assert.Equal(email, user.Email);
    }

    [Fact]
    public async Task GetUserByIdAsync_WithNonExistingUser_ReturnsNull()
    {
        var user = await _repository.GetUserByIdAsync("non-existing-id");

        Assert.Null(user);
    }

    [Fact]
    public async Task UserExistsAsync_WithExistingUser_ReturnsTrue()
    {
        var (createdUser, _) = await _repository.CreateUserAsync("exists@example.com");

        var exists = await _repository.UserExistsAsync(createdUser!.Id);

        Assert.True(exists);
    }

    [Fact]
    public async Task UserExistsAsync_WithNonExistingUser_ReturnsFalse()
    {
        var exists = await _repository.UserExistsAsync("non-existing-id");

        Assert.False(exists);
    }
}
