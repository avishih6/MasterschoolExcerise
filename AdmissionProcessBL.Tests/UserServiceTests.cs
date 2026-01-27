using AdmissionProcessBL.Services;
using AdmissionProcessDAL.Models;
using AdmissionProcessDAL.Repositories.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AdmissionProcessBL.Tests;

public class UserServiceTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<ILogger<UserService>> _loggerMock;
    private readonly UserService _service;

    public UserServiceTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _loggerMock = new Mock<ILogger<UserService>>();
        _service = new UserService(_userRepositoryMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task CreateUserAsync_WithValidEmail_ReturnsSuccess()
    {
        var email = "test@example.com";
        var expectedUser = new User { Id = "1", Email = email };
        _userRepositoryMock
            .Setup(r => r.CreateUserAsync(email))
            .ReturnsAsync((expectedUser, false));

        var result = await _service.CreateUserAsync(email);

        Assert.True(result.IsSuccess);
        Assert.Equal("1", result.Data?.UserId);
    }

    [Fact]
    public async Task CreateUserAsync_WithExistingEmail_ReturnsConflict()
    {
        var email = "existing@example.com";
        var existingUser = new User { Id = "existing-id", Email = email };
        _userRepositoryMock
            .Setup(r => r.CreateUserAsync(email))
            .ReturnsAsync((existingUser, true));

        var result = await _service.CreateUserAsync(email);

        Assert.False(result.IsSuccess);
        Assert.Equal(409, result.HttpStatusCode);
        Assert.Equal("existing-id", result.Data?.UserId);
    }

    [Fact]
    public async Task CreateUserAsync_WithEmptyEmail_ReturnsFailure()
    {
        var result = await _service.CreateUserAsync("");

        Assert.False(result.IsSuccess);
        Assert.Equal("Email is required", result.ErrorMessage);
    }

    [Fact]
    public async Task CreateUserAsync_WithNullEmail_ReturnsFailure()
    {
        var result = await _service.CreateUserAsync(null!);

        Assert.False(result.IsSuccess);
        Assert.Equal("Email is required", result.ErrorMessage);
    }

    [Fact]
    public async Task CreateUserAsync_WhenRepositoryReturnsNull_ReturnsFailure()
    {
        _userRepositoryMock
            .Setup(r => r.CreateUserAsync(It.IsAny<string>()))
            .ReturnsAsync((null, false));

        var result = await _service.CreateUserAsync("test@example.com");

        Assert.False(result.IsSuccess);
        Assert.Equal("Failed to create user", result.ErrorMessage);
    }
}
