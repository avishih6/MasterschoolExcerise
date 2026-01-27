using AdmissionProcessApi.Models.DTOs;
using AdmissionProcessDAL.Models;
using AdmissionProcessDAL.Repositories.Interfaces;
using Microsoft.Extensions.Logging;

namespace AdmissionProcessApi.Services;

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

    public async Task<ServiceResult<StatusResponse>> GetStatusAsync(string userId)
    {
        try
        {
            var userProgress = await _progressRepository.GetProgressAsync(userId).ConfigureAwait(false);

            if (userProgress == null)
            {
                return ServiceResult<StatusResponse>.Success(new StatusResponse 
                { 
                    Status = "in_progress" 
                });
            }

            var statusString = ConvertProgressStatusToString(userProgress.CachedOverallStatus);
            
            return ServiceResult<StatusResponse>.Success(new StatusResponse 
            { 
                Status = statusString 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting status for user: {UserId}", userId);
            return ServiceResult<StatusResponse>.Failure("An error occurred while retrieving the status");
        }
    }

    private string ConvertProgressStatusToString(ProgressStatus status)
    {
        return status switch
        {
            ProgressStatus.Accepted => "accepted",
            ProgressStatus.Rejected => "rejected",
            _ => "in_progress"
        };
    }
}
