using AdmissionProcessApi.Models.DTOs;
using AdmissionProcessDAL.Models;
using AdmissionProcessDAL.Repositories.Interfaces;
using Microsoft.Extensions.Logging;

namespace AdmissionProcessApi.Services;

public class ProgressService : IProgressService
{
    private readonly IFlowRepository _flowRepository;
    private readonly IProgressRepository _progressRepository;
    private readonly IPassEvaluator _passEvaluator;
    private readonly ILogger<ProgressService> _logger;

    public ProgressService(
        IFlowRepository flowRepository,
        IProgressRepository progressRepository,
        IPassEvaluator passEvaluator,
        ILogger<ProgressService> logger)
    {
        _flowRepository = flowRepository;
        _progressRepository = progressRepository;
        _passEvaluator = passEvaluator;
        _logger = logger;
    }

    public async Task<ServiceResult<CurrentProgressResponse>> GetCurrentProgressAsync(string userId)
    {
        try
        {
            var userProgress = await _progressRepository.GetOrCreateProgressAsync(userId).ConfigureAwait(false);
            
            if (userProgress.CurrentStepId.HasValue)
            {
                var currentStep = await _flowRepository.GetNodeByIdAsync(userProgress.CurrentStepId.Value).ConfigureAwait(false);
                FlowNode? currentTask = null;
                
                if (userProgress.CurrentTaskId.HasValue)
                {
                    currentTask = await _flowRepository.GetNodeByIdAsync(userProgress.CurrentTaskId.Value).ConfigureAwait(false);
                }
                
                return ServiceResult<CurrentProgressResponse>.Success(new CurrentProgressResponse
                {
                    CurrentStep = currentStep?.Name,
                    CurrentTask = currentTask?.Name
                });
            }
            
            var calculatedProgress = await CalculateCurrentProgressAsync(userProgress).ConfigureAwait(false);
            return ServiceResult<CurrentProgressResponse>.Success(calculatedProgress);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current progress for user: {UserId}", userId);
            return ServiceResult<CurrentProgressResponse>.Failure("An error occurred while retrieving progress");
        }
    }

    public async Task<ServiceResult> CompleteStepAsync(string userId, string stepName, Dictionary<string, object> payload)
    {
        try
        {
            var stepFindResult = await FindStepByNameAsync(stepName).ConfigureAwait(false);
            if (!stepFindResult.IsSuccess)
            {
                return ServiceResult.Failure(stepFindResult.ErrorMessage ?? "Step not found");
            }

            var stepNode = stepFindResult.Data!;
            var userProgress = await _progressRepository.GetOrCreateProgressAsync(userId).ConfigureAwait(false);
            var tasks = await _flowRepository.GetChildNodesAsync(stepNode.Id).ConfigureAwait(false);

            if (tasks.Count == 0)
            {
                await ProcessStepWithoutTasksAsync(stepNode, payload, userProgress).ConfigureAwait(false);
            }
            else
            {
                await ProcessStepWithTasksAsync(stepNode, tasks, payload, userProgress).ConfigureAwait(false);
            }

            await UpdateProgressCacheAsync(userProgress).ConfigureAwait(false);
            await _progressRepository.SaveProgressAsync(userProgress).ConfigureAwait(false);

            _logger.LogInformation("Step '{StepName}' completed for user {UserId}", stepName, userId);
            return ServiceResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing step '{StepName}' for user: {UserId}", stepName, userId);
            return ServiceResult.Failure("An error occurred while completing the step");
        }
    }

    private async Task<ServiceResult<FlowNode>> FindStepByNameAsync(string stepName)
    {
        var rootSteps = await _flowRepository.GetRootStepsAsync().ConfigureAwait(false);
        var stepNode = rootSteps.FirstOrDefault(s => s.Name.Equals(stepName, StringComparison.OrdinalIgnoreCase));
        
        if (stepNode == null)
        {
            _logger.LogWarning("Step '{StepName}' not found", stepName);
            return ServiceResult<FlowNode>.Failure($"Step '{stepName}' not found");
        }
        
        return ServiceResult<FlowNode>.Success(stepNode);
    }

    private async Task ProcessStepWithoutTasksAsync(FlowNode step, Dictionary<string, object> payload, UserProgress progress)
    {
        var passed = await _passEvaluator.EvaluateAsync(step, payload).ConfigureAwait(false);
        UpdateNodeStatus(progress, step.Id, passed);
    }

    private async Task ProcessStepWithTasksAsync(FlowNode step, List<FlowNode> tasks, Dictionary<string, object> payload, UserProgress progress)
    {
        var task = DetermineTaskFromPayload(tasks, payload, progress);
        
        if (task == null)
        {
            _logger.LogWarning("Could not determine task from payload for step: {StepName}", step.Name);
            return;
        }

        var passed = await _passEvaluator.EvaluateAsync(task, payload).ConfigureAwait(false);
        StoreDerivedFactsIfNeeded(task, payload, progress);
        UpdateNodeStatus(progress, task.Id, passed);
        
        TryMarkStepAsComplete(step, tasks, progress);
    }

    private FlowNode? DetermineTaskFromPayload(List<FlowNode> tasks, Dictionary<string, object> payload, UserProgress progress)
    {
        if (tasks.Count == 1)
            return tasks[0];

        foreach (var task in tasks.OrderBy(t => t.Order))
        {
            if (task.RequiresPreviousTaskFailedId.HasValue)
            {
                var previousStatus = progress.NodeStatuses.GetValueOrDefault(task.RequiresPreviousTaskFailedId.Value);
                if (previousStatus?.Status != ProgressStatus.Rejected)
                    continue;
            }

            if (task.PayloadIdentifiers.Count > 0 && task.PayloadIdentifiers.Any(id => payload.ContainsKey(id)))
            {
                if (!progress.NodeStatuses.ContainsKey(task.Id))
                    return task;
            }
        }

        return tasks.FirstOrDefault(t => !progress.NodeStatuses.ContainsKey(t.Id));
    }

    private void StoreDerivedFactsIfNeeded(FlowNode task, Dictionary<string, object> payload, UserProgress progress)
    {
        if (task.DerivedFactsToStore == null)
            return;

        foreach (var mapping in task.DerivedFactsToStore)
        {
            if (payload.TryGetValue(mapping.SourceField, out var value))
            {
                progress.DerivedFacts[mapping.TargetFactName] = value;
            }
        }
    }

    private void UpdateNodeStatus(UserProgress progress, int nodeId, bool passed)
    {
        progress.NodeStatuses[nodeId] = new NodeStatus
        {
            Status = passed ? ProgressStatus.Accepted : ProgressStatus.Rejected,
            UpdatedAt = DateTime.UtcNow
        };
    }

    private void TryMarkStepAsComplete(FlowNode step, List<FlowNode> tasks, UserProgress progress)
    {
        var visibleTasks = tasks.Where(t => t.IsVisibleForUser(progress)).ToList();
        var allTasksComplete = visibleTasks.All(t => progress.NodeStatuses.ContainsKey(t.Id));
        
        if (!allTasksComplete)
            return;

        var allTasksAccepted = visibleTasks.All(t =>
        {
            var status = progress.NodeStatuses.GetValueOrDefault(t.Id);
            return status?.Status == ProgressStatus.Accepted;
        });

        progress.NodeStatuses[step.Id] = new NodeStatus
        {
            Status = allTasksAccepted ? ProgressStatus.Accepted : ProgressStatus.Rejected,
            UpdatedAt = DateTime.UtcNow
        };
    }

    private async Task<CurrentProgressResponse> CalculateCurrentProgressAsync(UserProgress userProgress)
    {
        var rootSteps = await _flowRepository.GetRootStepsAsync().ConfigureAwait(false);

        foreach (var stepNode in rootSteps.OrderBy(s => s.Order))
        {
            var tasks = await _flowRepository.GetChildNodesAsync(stepNode.Id).ConfigureAwait(false);
            var visibleTasks = tasks.Where(t => t.IsVisibleForUser(userProgress)).ToList();

            if (tasks.Count == 0)
            {
                var stepStatus = userProgress.NodeStatuses.GetValueOrDefault(stepNode.Id);
                if (stepStatus?.Status != ProgressStatus.Accepted)
                {
                    return new CurrentProgressResponse
                    {
                        CurrentStep = stepNode.Name,
                        CurrentTask = null
                    };
                }
                continue;
            }

            foreach (var task in visibleTasks.OrderBy(t => t.Order))
            {
                var taskStatus = userProgress.NodeStatuses.GetValueOrDefault(task.Id);
                if (taskStatus?.Status != ProgressStatus.Accepted)
                {
                    return new CurrentProgressResponse
                    {
                        CurrentStep = stepNode.Name,
                        CurrentTask = task.Name
                    };
                }
            }
        }

        return new CurrentProgressResponse
        {
            CurrentStep = null,
            CurrentTask = null
        };
    }

    private async Task UpdateProgressCacheAsync(UserProgress progress)
    {
        var currentProgress = await CalculateCurrentProgressAsync(progress).ConfigureAwait(false);
        var overallStatus = await CalculateOverallStatusAsync(progress).ConfigureAwait(false);
        
        var rootSteps = await _flowRepository.GetRootStepsAsync().ConfigureAwait(false);
        
        if (!string.IsNullOrEmpty(currentProgress.CurrentStep))
        {
            var currentStep = rootSteps.FirstOrDefault(s => s.Name == currentProgress.CurrentStep);
            progress.CurrentStepId = currentStep?.Id;
            
            if (!string.IsNullOrEmpty(currentProgress.CurrentTask) && currentStep != null)
            {
                var tasks = await _flowRepository.GetChildNodesAsync(currentStep.Id).ConfigureAwait(false);
                var currentTask = tasks.FirstOrDefault(t => t.Name == currentProgress.CurrentTask);
                progress.CurrentTaskId = currentTask?.Id;
            }
            else
            {
                progress.CurrentTaskId = null;
            }
        }
        else
        {
            progress.CurrentStepId = null;
            progress.CurrentTaskId = null;
        }
        
        progress.CachedOverallStatus = overallStatus;
        progress.CacheUpdatedAt = DateTime.UtcNow;
    }

    private async Task<ProgressStatus> CalculateOverallStatusAsync(UserProgress progress)
    {
        var rootSteps = await _flowRepository.GetRootStepsAsync().ConfigureAwait(false);
        bool allComplete = true;

        foreach (var stepNode in rootSteps)
        {
            var tasks = await _flowRepository.GetChildNodesAsync(stepNode.Id).ConfigureAwait(false);
            var visibleTasks = tasks.Where(t => t.IsVisibleForUser(progress)).ToList();

            foreach (var task in visibleTasks)
            {
                var status = progress.NodeStatuses.GetValueOrDefault(task.Id);
                
                if (IsRejectionCondition(task, status, progress))
                {
                    return ProgressStatus.Rejected;
                }
                
                if (status?.Status != ProgressStatus.Accepted)
                {
                    allComplete = false;
                }
            }

            if (tasks.Count == 0)
            {
                var stepStatus = progress.NodeStatuses.GetValueOrDefault(stepNode.Id);
                if (stepStatus?.Status == ProgressStatus.Rejected)
                {
                    return ProgressStatus.Rejected;
                }
                if (stepStatus?.Status != ProgressStatus.Accepted)
                {
                    allComplete = false;
                }
            }
        }

        return allComplete ? ProgressStatus.Accepted : ProgressStatus.NotStarted;
    }

    private bool IsRejectionCondition(FlowNode task, NodeStatus? status, UserProgress progress)
    {
        if (status?.Status != ProgressStatus.Rejected)
            return false;

        if (task.Name.Contains("IQ test", StringComparison.OrdinalIgnoreCase))
        {
            if (progress.DerivedFacts.TryGetValue("iq_score", out var scoreObj))
            {
                var score = Convert.ToInt32(scoreObj);
                if (score >= 60 && score <= 75)
                {
                    return false;
                }
                return score < 60;
            }
        }

        if (task.Name.Equals("Perform interview", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return true;
    }
}
