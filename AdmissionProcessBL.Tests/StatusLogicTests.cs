using AdmissionProcessBL;
using AdmissionProcessDAL.Models;
using AdmissionProcessDAL.Repositories.Interfaces;
using AdmissionProcessModels.Enums;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AdmissionProcessBL.Tests;

public class StatusLogicTests
{
    private readonly Mock<IProgressRepository> _progressRepositoryMock;
    private readonly Mock<ILogger<StatusLogic>> _loggerMock;
    private readonly StatusLogic _logic;

    public StatusLogicTests()
    {
        _progressRepositoryMock = new Mock<IProgressRepository>();
        _loggerMock = new Mock<ILogger<StatusLogic>>();
        _logic = new StatusLogic(_progressRepositoryMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GetUserStatusAsync_WithNewUser_ReturnsInProgress()
    {
        _progressRepositoryMock
            .Setup(r => r.GetProgressAsync(It.IsAny<string>()))
            .ReturnsAsync((UserProgress?)null);

        var result = await _logic.GetUserStatusAsync("new-user");

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

        var result = await _logic.GetUserStatusAsync("accepted-user");

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

        var result = await _logic.GetUserStatusAsync("rejected-user");

        Assert.True(result.IsSuccess);
        Assert.Equal(UserStatus.Rejected, result.Data?.Status);
    }
}
