using AdmissionProcessBL.Services.Interfaces;
using AdmissionProcessDAL.Repositories.Interfaces;
using AdmissionProcessModels.DTOs;
using AdmissionProcessModels.Enums;
using Microsoft.Extensions.Logging;

namespace AdmissionProcessBL.Services;

public class StatusService : IStatusService
{
    private readonly IProgressRepository _progressRepository;
    private readonly ILogger<StatusService> _logger;

    public StatusService(
        IProgressRepository progressRepository,
        ILogger<StatusService> logger)
    {
        _progressRepository = progressRepository;
        _logger = logger;
    }

    public async Task<ServiceResult<StatusResponse>> GetUserStatusAsync(string userId)
    {
        try
        {
            var userProgress = await _progressRepository.GetProgressAsync(userId).ConfigureAwait(false);

            if (userProgress == null)
            {
                return ServiceResult<StatusResponse>.Success(new StatusResponse 
                { 
                    Status = UserStatus.InProgress 
                });
            }

            return ServiceResult<StatusResponse>.Success(new StatusResponse 
            { 
                Status = userProgress.CachedOverallStatus 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"GetUserStatusAsync failed for user {userId}");
            return ServiceResult<StatusResponse>.Failure("An error occurred while retrieving the status");
        }
    }
}
