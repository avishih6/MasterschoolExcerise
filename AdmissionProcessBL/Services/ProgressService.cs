using AdmissionProcessBL.Services.Interfaces;
using AdmissionProcessDAL.Models;
using AdmissionProcessDAL.Repositories.Interfaces;
using AdmissionProcessModels.DTOs;
using AdmissionProcessModels.Enums;
using Microsoft.Extensions.Logging;

namespace AdmissionProcessBL.Services;

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

    public async Task<ServiceResult<CurrentProgressResponse>> GetCurrentStepAndTaskForUserAsync(string userId)
    {
        try
        {
            var userProgress = await _progressRepository.GetOrCreateProgressAsync(userId).ConfigureAwait(false);
            var currentProgress = await CalculateCurrentProgressAsync(userProgress).ConfigureAwait(false);
            
            return ServiceResult<CurrentProgressResponse>.Success(currentProgress);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"GetCurrentStepAndTaskForUserAsync failed for user {userId}");
            return ServiceResult<CurrentProgressResponse>.Failure("An error occurred while retrieving progress");
        }
    }

    public async Task<ServiceResult> CompleteStepAsync(string userId, string stepName, Dictionary<string, object> payload)
    {
        try
        {
            var stepNode = await FindStepByNameAsync(stepName).ConfigureAwait(false);
            if (stepNode == null)
            {
                _logger.LogError($"CompleteStepAsync failed: step '{stepName}' not found");
                return ServiceResult.Failure($"Step '{stepName}' not found", 404);
            }

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

            _logger.LogInformation($"CompleteStepAsync: step '{stepName}' completed for user {userId}");
            return ServiceResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"CompleteStepAsync failed for step '{stepName}' and user {userId}");
            return ServiceResult.Failure("An error occurred while completing the step");
        }
    }

    private async Task<FlowNode?> FindStepByNameAsync(string stepName)
    {
        return await _flowRepository.GetNodeByNameAsync(stepName).ConfigureAwait(false);
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
            _logger.LogError($"ProcessStepWithTasksAsync: could not determine task from payload for step {step.Name}");
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
            if (!IsTaskEligible(task, progress))
                continue;

            if (DoesPayloadMatchTask(task, payload, progress))
                return task;
        }

        return GetFirstIncompleteTask(tasks, progress);
    }

    private bool IsTaskEligible(FlowNode task, UserProgress progress)
    {
        if (!task.RequiresPreviousTaskFailedId.HasValue)
            return true;

        var previousStatus = progress.NodeStatuses.GetValueOrDefault(task.RequiresPreviousTaskFailedId.Value);
        return previousStatus?.Status == ProgressStatus.Rejected;
    }

    private bool DoesPayloadMatchTask(FlowNode task, Dictionary<string, object> payload, UserProgress progress)
    {
        if (task.PayloadIdentifiers.Count == 0)
            return false;

        var hasMatchingPayload = task.PayloadIdentifiers.Any(id => payload.ContainsKey(id));
        var isNotCompleted = !progress.NodeStatuses.ContainsKey(task.Id);
        
        return hasMatchingPayload && isNotCompleted;
    }

    private FlowNode? GetFirstIncompleteTask(List<FlowNode> tasks, UserProgress progress)
    {
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
        
        if (!AreAllTasksComplete(visibleTasks, progress))
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

    private bool AreAllTasksComplete(List<FlowNode> visibleTasks, UserProgress progress)
    {
        return visibleTasks.All(t => progress.NodeStatuses.ContainsKey(t.Id));
    }

    private async Task<CurrentProgressResponse> CalculateCurrentProgressAsync(UserProgress userProgress)
    {
        var rootSteps = await _flowRepository.GetRootStepsAsync().ConfigureAwait(false);

        foreach (var stepNode in rootSteps.OrderBy(s => s.Order))
        {
            var result = await CheckStepProgressAsync(stepNode, userProgress).ConfigureAwait(false);
            if (result != null)
                return result;
        }

        return new CurrentProgressResponse { CurrentStep = null, CurrentTask = null };
    }

    private async Task<CurrentProgressResponse?> CheckStepProgressAsync(FlowNode stepNode, UserProgress userProgress)
    {
        var tasks = await _flowRepository.GetChildNodesAsync(stepNode.Id).ConfigureAwait(false);
        var visibleTasks = tasks.Where(t => t.IsVisibleForUser(userProgress)).ToList();

        if (tasks.Count == 0)
        {
            return CheckStepWithoutTasks(stepNode, userProgress);
        }

        return CheckStepWithTasks(stepNode, visibleTasks, userProgress);
    }

    private CurrentProgressResponse? CheckStepWithoutTasks(FlowNode stepNode, UserProgress userProgress)
    {
        var stepStatus = userProgress.NodeStatuses.GetValueOrDefault(stepNode.Id);
        if (stepStatus?.Status != ProgressStatus.Accepted)
        {
            return new CurrentProgressResponse { CurrentStep = stepNode.Name, CurrentTask = null };
        }
        return null;
    }

    private CurrentProgressResponse? CheckStepWithTasks(FlowNode stepNode, List<FlowNode> visibleTasks, UserProgress userProgress)
    {
        foreach (var task in visibleTasks.OrderBy(t => t.Order))
        {
            var taskStatus = userProgress.NodeStatuses.GetValueOrDefault(task.Id);
            if (taskStatus?.Status != ProgressStatus.Accepted)
            {
                return new CurrentProgressResponse { CurrentStep = stepNode.Name, CurrentTask = task.Name };
            }
        }
        return null;
    }

    private async Task UpdateProgressCacheAsync(UserProgress progress)
    {
        var currentProgressTask = CalculateCurrentProgressAsync(progress);
        var overallStatusTask = CalculateOverallStatusInternalAsync(progress);
        
        await Task.WhenAll(currentProgressTask, overallStatusTask).ConfigureAwait(false);
        
        var currentProgress = await currentProgressTask.ConfigureAwait(false);
        var overallStatus = await overallStatusTask.ConfigureAwait(false);

        await UpdateCacheValuesAsync(progress, currentProgress).ConfigureAwait(false);
        progress.CachedOverallStatus = overallStatus;
        progress.CacheUpdatedAt = DateTime.UtcNow;
    }

    private async Task UpdateCacheValuesAsync(UserProgress progress, CurrentProgressResponse currentProgress)
    {
        if (!string.IsNullOrEmpty(currentProgress.CurrentStep))
        {
            var currentStep = await _flowRepository.GetNodeByNameAsync(currentProgress.CurrentStep).ConfigureAwait(false);
            progress.CurrentStepId = currentStep?.Id;

            if (!string.IsNullOrEmpty(currentProgress.CurrentTask))
            {
                var currentTask = await _flowRepository.GetNodeByNameAsync(currentProgress.CurrentTask).ConfigureAwait(false);
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
    }

    private async Task<UserStatus> CalculateOverallStatusInternalAsync(UserProgress progress)
    {
        var rootSteps = await _flowRepository.GetRootStepsAsync().ConfigureAwait(false);
        bool allComplete = true;

        foreach (var stepNode in rootSteps)
        {
            var (isRejected, isComplete) = await CheckStepStatusAsync(stepNode, progress).ConfigureAwait(false);
            
            if (isRejected)
                return UserStatus.Rejected;
            
            if (!isComplete)
                allComplete = false;
        }

        return allComplete ? UserStatus.Accepted : UserStatus.InProgress;
    }

    private async Task<(bool IsRejected, bool IsComplete)> CheckStepStatusAsync(FlowNode stepNode, UserProgress progress)
    {
        var tasks = await _flowRepository.GetChildNodesAsync(stepNode.Id).ConfigureAwait(false);
        var visibleTasks = tasks.Where(t => t.IsVisibleForUser(progress)).ToList();

        foreach (var task in visibleTasks)
        {
            var status = progress.NodeStatuses.GetValueOrDefault(task.Id);
            
            if (IsRejectionCondition(task, status, progress))
                return (true, false);
            
            if (status?.Status != ProgressStatus.Accepted)
                return (false, false);
        }

        if (tasks.Count == 0)
        {
            var stepStatus = progress.NodeStatuses.GetValueOrDefault(stepNode.Id);
            if (stepStatus?.Status == ProgressStatus.Rejected)
                return (true, false);
            if (stepStatus?.Status != ProgressStatus.Accepted)
                return (false, false);
        }

        return (false, true);
    }

    private bool IsRejectionCondition(FlowNode task, NodeStatus? status, UserProgress progress)
    {
        if (status?.Status != ProgressStatus.Rejected)
            return false;

        if (IsIqTestWithSecondChance(task, progress))
            return false;

        return true;
    }

    private bool IsIqTestWithSecondChance(FlowNode task, UserProgress progress)
    {
        if (!task.Name.Contains("IQ test", StringComparison.OrdinalIgnoreCase))
            return false;

        if (!progress.DerivedFacts.TryGetValue("iq_score", out var scoreObj))
            return false;

        var score = Convert.ToInt32(scoreObj);
        return score >= 60 && score <= 75;
    }
}
