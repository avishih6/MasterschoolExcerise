using AdmissionProcessApi.Models.DTOs;
using AdmissionProcessDAL.Models;
using AdmissionProcessDAL.Repositories.Interfaces;
using Microsoft.Extensions.Logging;

namespace AdmissionProcessApi.Services;

public class FlowService : IFlowService
{
    private readonly IFlowRepository _flowRepository;
    private readonly IProgressRepository _progressRepository;
    private readonly ILogger<FlowService> _logger;

    public FlowService(
        IFlowRepository flowRepository,
        IProgressRepository progressRepository,
        ILogger<FlowService> logger)
    {
        _flowRepository = flowRepository;
        _progressRepository = progressRepository;
        _logger = logger;
    }

    public async Task<ServiceResult<FlowResponse>> GetFlowForUserAsync(string userId)
    {
        try
        {
            var rootSteps = await _flowRepository.GetRootStepsAsync().ConfigureAwait(false);
            var userProgress = await _progressRepository.GetOrCreateProgressAsync(userId).ConfigureAwait(false);

            var steps = new List<FlowStepDto>();
            int totalVisibleTasks = 0;

            foreach (var stepNode in rootSteps)
            {
                var stepTasks = await _flowRepository.GetChildNodesAsync(stepNode.Id).ConfigureAwait(false);
                var visibleTasks = BuildVisibleTasksListForStep(stepNode, stepTasks, userProgress);
                
                totalVisibleTasks += visibleTasks.Count;
                
                if (stepTasks.Count == 0)
                {
                    totalVisibleTasks++;
                }

                steps.Add(new FlowStepDto
                {
                    Name = stepNode.Name,
                    Order = stepNode.Order,
                    Tasks = visibleTasks
                });
            }

            var response = new FlowResponse
            {
                Steps = steps,
                TotalSteps = rootSteps.Count,
                TotalTasks = totalVisibleTasks
            };

            return ServiceResult<FlowResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving flow for user: {UserId}", userId);
            return ServiceResult<FlowResponse>.Failure("An error occurred while retrieving the flow");
        }
    }

    private List<FlowTaskDto> BuildVisibleTasksListForStep(FlowNode stepNode, List<FlowNode> tasks, UserProgress userProgress)
    {
        var visibleTasks = new List<FlowTaskDto>();
        
        foreach (var task in tasks)
        {
            if (task.IsVisibleForUser(userProgress))
            {
                visibleTasks.Add(new FlowTaskDto
                {
                    Name = task.Name,
                    StepName = stepNode.Name
                });
            }
        }
        
        return visibleTasks;
    }
}
