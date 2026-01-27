using AdmissionProcessBL.Services;
using AdmissionProcessDAL.Models;
using AdmissionProcessDAL.Repositories.Interfaces;
using AdmissionProcessModels.Enums;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AdmissionProcessBL.Tests;

public class StatusServiceTests
{
    private readonly Mock<IProgressRepository> _progressRepositoryMock;
    private readonly Mock<ILogger<StatusService>> _loggerMock;
    private readonly StatusService _service;

    public StatusServiceTests()
    {
        _progressRepositoryMock = new Mock<IProgressRepository>();
        _loggerMock = new Mock<ILogger<StatusService>>();
        _service = new StatusService(_progressRepositoryMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GetUserStatusAsync_WithNewUser_ReturnsInProgress()
    {
        _progressRepositoryMock
            .Setup(r => r.GetProgressAsync(It.IsAny<string>()))
            .ReturnsAsync((UserProgress?)null);

        var result = await _service.GetUserStatusAsync("new-user");

        Assert.True(result.IsSuccess);
        Assert.Equal(UserStatus.InProgress, result.Data?.Status);
    }

    [Fact]
    public async Task GetUserStatusAsync_WithAcceptedUser_ReturnsAccepted()
    {
        var progress = new UserProgress
        {
            UserId = "accepted-user",
            CachedOverallStatus = UserStatus.Accepted
        };
        _progressRepositoryMock
            .Setup(r => r.GetProgressAsync("accepted-user"))
            .ReturnsAsync(progress);

        var result = await _service.GetUserStatusAsync("accepted-user");

        Assert.True(result.IsSuccess);
        Assert.Equal(UserStatus.Accepted, result.Data?.Status);
    }

    [Fact]
    public async Task GetUserStatusAsync_WithRejectedUser_ReturnsRejected()
    {
        var progress = new UserProgress
        {
            UserId = "rejected-user",
            CachedOverallStatus = UserStatus.Rejected
        };
        _progressRepositoryMock
            .Setup(r => r.GetProgressAsync("rejected-user"))
            .ReturnsAsync(progress);

        var result = await _service.GetUserStatusAsync("rejected-user");

        Assert.True(result.IsSuccess);
        Assert.Equal(UserStatus.Rejected, result.Data?.Status);
    }

    [Fact]
    public async Task GetUserStatusAsync_WhenExceptionThrown_ReturnsFailure()
    {
        _progressRepositoryMock
            .Setup(r => r.GetProgressAsync(It.IsAny<string>()))
            .ThrowsAsync(new Exception("Database error"));

        var result = await _service.GetUserStatusAsync("error-user");

        Assert.False(result.IsSuccess);
        Assert.Contains("error", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }
}
