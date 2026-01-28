using AdmissionProcessDAL.Models;
using AdmissionProcessDAL.Repositories.Mock;
using AdmissionProcessModels.Enums;
using Xunit;

namespace AdmissionProcessDAL.Tests;

public class MockProgressRepositoryTests
{
    private readonly MockProgressRepository _repository;

    public MockProgressRepositoryTests()
    {
        _repository = new MockProgressRepository();
    }

    [Fact]
    public async Task GetOrCreateProgressAsync_WithNewUser_CreatesProgress()
    {
        var userId = "new-user-1";

        var progress = await _repository.GetOrCreateProgressAsync(userId);

        Assert.NotNull(progress);
        Assert.Equal(userId, progress.UserId);
        Assert.Equal(UserStatus.InProgress, progress.CachedOverallStatus);
        Assert.Empty(progress.NodeStatuses);
    }

    [Fact]
    public async Task GetOrCreateProgressAsync_WithExistingUser_ReturnsSameProgress()
    {
        var userId = "existing-user-1";

        var progress1 = await _repository.GetOrCreateProgressAsync(userId);
        progress1.NodeStatuses[1] = new NodeStatus { Status = ProgressStatus.Accepted, UpdatedAt = DateTime.UtcNow };

        var progress2 = await _repository.GetOrCreateProgressAsync(userId);

        Assert.Same(progress1, progress2);
        Assert.Single(progress2.NodeStatuses);
    }
}
