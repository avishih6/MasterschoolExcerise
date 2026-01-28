using AdmissionProcessBL.Interfaces;
using AdmissionProcessDAL.Repositories.Interfaces;
using AdmissionProcessModels.DTOs;
using AdmissionProcessModels.Enums;
using Microsoft.Extensions.Logging;

namespace AdmissionProcessBL;

public class StatusLogic : IStatusLogic
{
    private readonly IProgressRepository _progressRepository;
    private readonly ILogger<StatusLogic> _logger;

    public StatusLogic(
        IProgressRepository progressRepository,
        ILogger<StatusLogic> logger)
    {
        _progressRepository = progressRepository;
        _logger = logger;
    }

    public async Task<LogicResult<StatusResponse>> GetUserStatusAsync(string userId)
    {
        try
        {
            var userProgress = await _progressRepository.GetProgressAsync(userId).ConfigureAwait(false);

            if (userProgress == null)
            {
                return LogicResult<StatusResponse>.Success(new StatusResponse 
                { 
                    Status = UserStatus.InProgress 
                });
            }

            return LogicResult<StatusResponse>.Success(new StatusResponse 
            { 
                Status = userProgress.CachedOverallStatus 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"GetUserStatusAsync failed for user {userId}");
            return LogicResult<StatusResponse>.Failure("An error occurred while retrieving the status");
        }
    }
}
