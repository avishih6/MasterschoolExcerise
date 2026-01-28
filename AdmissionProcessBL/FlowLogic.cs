using AdmissionProcessBL.Interfaces;
using AdmissionProcessDAL.Models;
using AdmissionProcessDAL.Repositories.Interfaces;
using AdmissionProcessModels.DTOs;
using AdmissionProcessModels.Enums;
using Microsoft.Extensions.Logging;

namespace AdmissionProcessBL;

public class FlowLogic : IFlowLogic
{
    private readonly IFlowRepository _flowRepository;
    private readonly IProgressRepository _progressRepository;
    private readonly ILogger<FlowLogic> _logger;

    public FlowLogic(
        IFlowRepository flowRepository,
        IProgressRepository progressRepository,
        ILogger<FlowLogic> logger)
    {
        _flowRepository = flowRepository;
        _progressRepository = progressRepository;
        _logger = logger;
    }

    public async Task<LogicResult<FlowResponse>> GetEntireFlowForUserAsync(string userId)
    {
        try
        {
            var (rootSteps, userProgress) = await LoadFlowDataAsync(userId).ConfigureAwait(false);
            var response = BuildFlowResponse(rootSteps, userProgress);
            await EnrichWithCurrentPositionAsync(response, rootSteps, userProgress).ConfigureAwait(false);
            
            return LogicResult<FlowResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"GetEntireFlowForUserAsync failed for user {userId}");
            return LogicResult<FlowResponse>.Failure("An error occurred while retrieving the flow");
        }
    }

    private async Task<(List<FlowNode> RootSteps, UserProgress Progress)> LoadFlowDataAsync(string userId)
    {
        var rootStepsTask = _flowRepository.GetRootStepsAsync();
        var progressTask = _progressRepository.GetOrCreateProgressAsync(userId);
        
        await Task.WhenAll(rootStepsTask, progressTask).ConfigureAwait(false);
        
        return (rootStepsTask.Result, progressTask.Result);
    }

    private FlowResponse BuildFlowResponse(List<FlowNode> rootSteps, UserProgress userProgress)
    {
        var steps = new List<FlowStepDto>();

        foreach (var stepNode in rootSteps)
        {
            var stepDto = BuildStepDto(stepNode, userProgress);
            steps.Add(stepDto);
        }

        return new FlowResponse
        {
            Steps = steps,
            TotalSteps = rootSteps.Count
        };
    }

    private FlowStepDto BuildStepDto(FlowNode stepNode, UserProgress userProgress)
    {
        var tasks = _flowRepository.GetChildNodesAsync(stepNode.Id).GetAwaiter().GetResult();
        var visibleTasks = BuildVisibleTasksList(stepNode, tasks, userProgress);

        return new FlowStepDto
        {
            Name = stepNode.Name,
            Order = stepNode.Order,
            Tasks = visibleTasks
        };
    }

    private List<FlowTaskDto> BuildVisibleTasksList(FlowNode stepNode, List<FlowNode> tasks, UserProgress userProgress)
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

    private async Task EnrichWithCurrentPositionAsync(FlowResponse response, List<FlowNode> rootSteps, UserProgress userProgress)
    {
        var (currentStepName, currentTaskName, currentStepNumber) = await CalculateCurrentPositionAsync(rootSteps, userProgress).ConfigureAwait(false);
        
        response.CurrentStepName = currentStepName;
        response.CurrentTaskName = currentTaskName;
        response.CurrentStepNumber = currentStepNumber;
    }

    private async Task<(string? StepName, string? TaskName, int StepNumber)> CalculateCurrentPositionAsync(
        List<FlowNode> rootSteps, 
        UserProgress userProgress)
    {
        int stepNumber = 0;
        
        foreach (var stepNode in rootSteps.OrderBy(s => s.Order))
        {
            stepNumber++;
            var tasks = await _flowRepository.GetChildNodesAsync(stepNode.Id).ConfigureAwait(false);
            var visibleTasks = tasks.Where(t => t.IsVisibleForUser(userProgress)).ToList();

            if (tasks.Count == 0)
            {
                var stepStatus = userProgress.NodeStatuses.GetValueOrDefault(stepNode.Id);
                if (stepStatus?.Status != ProgressStatus.Accepted)
                {
                    return (stepNode.Name, null, stepNumber);
                }
                continue;
            }

            foreach (var task in visibleTasks.OrderBy(t => t.Order))
            {
                var taskStatus = userProgress.NodeStatuses.GetValueOrDefault(task.Id);
                if (taskStatus?.Status != ProgressStatus.Accepted)
                {
                    return (stepNode.Name, task.Name, stepNumber);
                }
            }
        }

        return (null, null, rootSteps.Count);
    }
}
