using AdmissionProcessBL;
using AdmissionProcessDAL.Models;
using AdmissionProcessDAL.Repositories.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AdmissionProcessBL.Tests;

public class UserLogicTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<ILogger<UserLogic>> _loggerMock;
    private readonly UserLogic _logic;

    public UserLogicTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _loggerMock = new Mock<ILogger<UserLogic>>();
        _logic = new UserLogic(_userRepositoryMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task CreateUserAsync_WithValidEmail_ReturnsSuccess()
    {
        var email = "test@example.com";
        var expectedUser = new User { Id = "1", Email = email };
        _userRepositoryMock
            .Setup(r => r.CreateUserAsync(email))
            .ReturnsAsync((expectedUser, false));

        var result = await _logic.CreateUserAsync(email);

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

        var result = await _logic.CreateUserAsync(email);

        Assert.False(result.IsSuccess);
        Assert.Equal(409, result.HttpStatusCode);
        Assert.Equal("existing-id", result.Data?.UserId);
    }

    [Fact]
    public async Task CreateUserAsync_WithEmptyEmail_ReturnsFailure()
    {
        var result = await _logic.CreateUserAsync("");

        Assert.False(result.IsSuccess);
        Assert.Equal("Email is required", result.ErrorMessage);
    }
}
