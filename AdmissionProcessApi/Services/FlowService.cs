using AdmissionProcessDAL.Models;
using AdmissionProcessDAL.Services;
using AdmissionProcessApi.Models.DTOs;

namespace AdmissionProcessApi.Services;

public class FlowService : IFlowService
{
    private readonly IStepDataService _stepDataService;
    private readonly ITaskDataService _taskDataService;
    private readonly IStepTaskDataService _stepTaskDataService;
    private readonly IUserProgressDataService _userProgressDataService;
    private readonly IUserTaskAssignmentDataService _userTaskAssignmentDataService;
    private readonly IConditionEvaluator _conditionEvaluator;

    public FlowService(
        IStepDataService stepDataService,
        ITaskDataService taskDataService,
        IStepTaskDataService stepTaskDataService,
        IUserProgressDataService userProgressDataService,
        IUserTaskAssignmentDataService userTaskAssignmentDataService,
        IConditionEvaluator conditionEvaluator)
    {
        _stepDataService = stepDataService;
        _taskDataService = taskDataService;
        _stepTaskDataService = stepTaskDataService;
        _userProgressDataService = userProgressDataService;
        _userTaskAssignmentDataService = userTaskAssignmentDataService;
        _conditionEvaluator = conditionEvaluator;
    }

    public async Task<FlowResponse> GetFlowAsync(string? userId = null)
    {
        var steps = await _stepDataService.GetActiveStepsAsync();
        var userProgress = userId != null ? await _userProgressDataService.GetUserProgressAsync(userId) : null;

        var response = new FlowResponse
        {
            Steps = new List<FlowStepDto>()
        };

        foreach (var step in steps)
        {
            var taskIds = await _stepTaskDataService.GetTaskIdsForStepAsync(step.Id);
            var tasks = new List<FlowTask>();
            
            foreach (var taskId in taskIds)
            {
                var task = await _taskDataService.GetTaskByIdAsync(taskId);
                if (task != null && task.IsActive)
                {
                    tasks.Add(task);
                }
            }

            var visibleTasks = await GetVisibleTasksAsync(step, tasks, userId, userProgress);

            response.Steps.Add(new FlowStepDto
            {
                Name = step.Name,
                Order = step.Order,
                Tasks = visibleTasks.Select(t => new FlowTaskDto
                {
                    Name = t.Name,
                    StepName = step.Name
                }).ToList()
            });
        }

        return response;
    }

    private async Task<List<FlowTask>> GetVisibleTasksAsync(
        Step step, 
        List<FlowTask> tasks, 
        string? userId, 
        UserProgress? userProgress)
    {
        var visibleTasks = new List<FlowTask>();

        foreach (var task in tasks.OrderBy(t => 
            _stepTaskDataService.GetStepTaskAsync(step.Id, t.Id).Result?.Order ?? 0))
        {
            // Check if task is user-specific
            if (!string.IsNullOrEmpty(userId))
            {
                var isUserSpecific = await _userTaskAssignmentDataService.IsTaskAssignedToUserAsync(userId, task.Id);
                if (isUserSpecific)
                {
                    visibleTasks.Add(task);
                    continue;
                }
            }

            // Check conditional visibility
            if (!string.IsNullOrEmpty(task.ConditionalVisibilityType) && userProgress != null)
            {
                Dictionary<string, object>? contextData = null;
                
                // For score_range visibility, get context from user progress
                if (task.ConditionalVisibilityType == "score_range")
                {
                    // Find related task completion for context
                    var relatedTaskKey = userProgress.CompletedTasks.Keys
                        .FirstOrDefault(k => k.Contains("IQ Test") && k.Contains("take_iq_test"));
                    
                    if (relatedTaskKey != null && 
                        userProgress.CompletedTasks.TryGetValue(relatedTaskKey, out var relatedCompletion))
                    {
                        contextData = relatedCompletion.Payload;
                    }
                }

                var isVisible = await _conditionEvaluator.EvaluateVisibilityConditionAsync(
                    task.ConditionalVisibilityType,
                    task.ConditionalVisibilityConfig,
                    userId ?? "",
                    contextData);

                if (isVisible)
                {
                    visibleTasks.Add(task);
                }
            }
            else
            {
                // Default: show all tasks without conditional visibility
                visibleTasks.Add(task);
            }
        }

        return visibleTasks;
    }
}
