using AdmissionProcessBL.Interfaces;
using AdmissionProcessDAL.Models;
using AdmissionProcessDAL.Repositories.Interfaces;
using AdmissionProcessModels.DTOs;
using AdmissionProcessModels.Enums;
using Microsoft.Extensions.Logging;

namespace AdmissionProcessBL;

public class ProgressLogic : IProgressLogic
{
    private readonly IFlowRepository _flowRepository;
    private readonly IProgressRepository _progressRepository;
    private readonly IPassEvaluator _passEvaluator;
    private readonly ILogger<ProgressLogic> _logger;

    public ProgressLogic(
        IFlowRepository flowRepository,
        IProgressRepository progressRepository,
        IPassEvaluator passEvaluator,
        ILogger<ProgressLogic> logger)
    {
        _flowRepository = flowRepository;
        _progressRepository = progressRepository;
        _passEvaluator = passEvaluator;
        _logger = logger;
    }

    public async Task<LogicResult<CurrentProgressResponse>> GetCurrentStepAndTaskForUserAsync(string userId)
    {
        try
        {
            var userProgress = await _progressRepository.GetOrCreateProgressAsync(userId).ConfigureAwait(false);
            var currentProgress = await CalculateCurrentProgressAsync(userProgress).ConfigureAwait(false);
            
            return LogicResult<CurrentProgressResponse>.Success(currentProgress);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"GetCurrentStepAndTaskForUserAsync failed for user {userId}");
            return LogicResult<CurrentProgressResponse>.Failure("An error occurred while retrieving progress");
        }
    }

    public async Task<LogicResult> CompleteStepAsync(string userId, string stepName, Dictionary<string, object> payload)
    {
        try
        {
            var stepNode = await FindStepByNameAsync(stepName).ConfigureAwait(false);
            if (stepNode == null)
            {
                _logger.LogError($"CompleteStepAsync failed: step '{stepName}' not found");
                return LogicResult.Failure($"Step '{stepName}' not found", 404);
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
            return LogicResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"CompleteStepAsync failed for step '{stepName}' and user {userId}");
            return LogicResult.Failure("An error occurred while completing the step");
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
                progress.DerivedFacts[mapping.TargetFactName] = ConvertJsonElement(value);
            }
        }
    }

    private static object ConvertJsonElement(object value)
    {
        if (value is System.Text.Json.JsonElement je)
        {
            return je.ValueKind switch
            {
                System.Text.Json.JsonValueKind.Number => je.TryGetInt64(out var l) ? l : je.GetDouble(),
                System.Text.Json.JsonValueKind.String => je.GetString() ?? string.Empty,
                System.Text.Json.JsonValueKind.True => true,
                System.Text.Json.JsonValueKind.False => false,
                _ => je.ToString()
            };
        }
        return value;
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

        return CheckStepWithTasks(stepNode, visibleTasks, tasks, userProgress);
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

    private CurrentProgressResponse? CheckStepWithTasks(FlowNode stepNode, List<FlowNode> visibleTasks, List<FlowNode> allTasks, UserProgress userProgress)
    {
        // If no visible tasks, check if step itself is complete
        if (visibleTasks.Count == 0)
        {
            var stepStatus = userProgress.NodeStatuses.GetValueOrDefault(stepNode.Id);
            if (stepStatus?.Status != ProgressStatus.Accepted)
            {
                return new CurrentProgressResponse { CurrentStep = stepNode.Name, CurrentTask = null };
            }
            return null;
        }

        foreach (var task in visibleTasks.OrderBy(t => t.Order))
        {
            var taskResult = GetTaskEffectiveResult(task, userProgress, allTasks);
            
            if (taskResult != TaskResult.Complete)
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
        
        // For steps without tasks, check the step status directly
        if (tasks.Count == 0)
        {
            var stepStatus = progress.NodeStatuses.GetValueOrDefault(stepNode.Id);
            if (stepStatus?.Status == ProgressStatus.Rejected)
                return (true, false);
            if (stepStatus?.Status != ProgressStatus.Accepted)
                return (false, false);
            return (false, true);
        }

        // For steps with tasks, check all visible tasks
        var visibleTasks = tasks.Where(t => t.IsVisibleForUser(progress)).ToList();
        
        // If no tasks are visible, check the step status directly (edge case)
        if (visibleTasks.Count == 0)
        {
            var stepStatus = progress.NodeStatuses.GetValueOrDefault(stepNode.Id);
            if (stepStatus?.Status == ProgressStatus.Rejected)
                return (true, false);
            return (stepStatus?.Status == ProgressStatus.Accepted, stepStatus?.Status == ProgressStatus.Accepted);
        }

        foreach (var task in visibleTasks)
        {
            var taskResult = GetTaskEffectiveResult(task, progress, tasks);
            
            if (taskResult == TaskResult.FinalRejection)
                return (true, false);
            
            if (taskResult != TaskResult.Complete)
                return (false, false);
        }

        return (false, true);
    }

    /// <summary>
    /// Determines the effective result of a task, considering any recovery tasks.
    /// Recovery tasks are identified by RequiresPreviousTaskFailedId pointing to this task.
    /// </summary>
    private TaskResult GetTaskEffectiveResult(FlowNode task, UserProgress progress, List<FlowNode> allTasks)
    {
        var status = progress.NodeStatuses.GetValueOrDefault(task.Id);
        
        // Not attempted yet
        if (status == null)
            return TaskResult.NotCompleted;
        
        // Passed
        if (status.Status == ProgressStatus.Accepted)
            return TaskResult.Complete;
        
        // Failed - check for recovery task (any task with RequiresPreviousTaskFailedId = this task's ID)
        var recoveryTask = allTasks.FirstOrDefault(t => t.RequiresPreviousTaskFailedId == task.Id);
        
        if (recoveryTask == null)
            return TaskResult.FinalRejection; // No recovery option exists
        
        // Check recovery task status
        if (progress.NodeStatuses.TryGetValue(recoveryTask.Id, out var recoveryStatus))
        {
            // Recovery was attempted
            return recoveryStatus.Status == ProgressStatus.Accepted 
                ? TaskResult.Complete      // Recovered successfully
                : TaskResult.FinalRejection; // Recovery also failed
        }
        
        // Recovery task exists but not attempted - check if it's available
        if (recoveryTask.IsVisibleForUser(progress))
            return TaskResult.PendingRecovery; // Can still recover
        
        return TaskResult.FinalRejection; // Recovery not available (visibility condition not met)
    }

    private enum TaskResult
    {
        NotCompleted,     // Task not yet attempted
        Complete,         // Task passed (or recovered)
        PendingRecovery,  // Task failed but recovery is available
        FinalRejection    // Task failed with no recovery option
    }
}
