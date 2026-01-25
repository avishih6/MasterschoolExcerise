using AdmissionProcessDAL.Models;
using AdmissionProcessDAL.Services;
using AdmissionProcessApi.Models.DTOs;

namespace AdmissionProcessApi.Services;

public class ProgressService : IProgressService
{
    private readonly IStepDataService _stepDataService;
    private readonly ITaskDataService _taskDataService;
    private readonly IStepTaskDataService _stepTaskDataService;
    private readonly IUserProgressDataService _userProgressDataService;
    private readonly IUserDataService _userDataService;
    private readonly IUserTaskAssignmentDataService _userTaskAssignmentDataService;
    private readonly IConditionEvaluator _conditionEvaluator;

    public ProgressService(
        IStepDataService stepDataService,
        ITaskDataService taskDataService,
        IStepTaskDataService stepTaskDataService,
        IUserProgressDataService userProgressDataService,
        IUserDataService userDataService,
        IUserTaskAssignmentDataService userTaskAssignmentDataService,
        IConditionEvaluator conditionEvaluator)
    {
        _stepDataService = stepDataService;
        _taskDataService = taskDataService;
        _stepTaskDataService = stepTaskDataService;
        _userProgressDataService = userProgressDataService;
        _userDataService = userDataService;
        _userTaskAssignmentDataService = userTaskAssignmentDataService;
        _conditionEvaluator = conditionEvaluator;
    }

    public async Task<UserProgressResponse> GetUserProgressAsync(string userId)
    {
        if (!await _userDataService.UserExistsAsync(userId))
        {
            throw new KeyNotFoundException($"User with ID {userId} not found");
        }

        var steps = await _stepDataService.GetActiveStepsAsync();
        var userProgress = await _userProgressDataService.GetOrCreateUserProgressAsync(userId);

        var (currentStep, currentTask) = await DetermineCurrentStepAndTaskAsync(steps, userProgress);

        return new UserProgressResponse
        {
            CurrentStep = currentStep?.Name,
            CurrentTask = currentTask?.Name,
            CurrentStepNumber = currentStep != null ? currentStep.Order : 0,
            TotalSteps = steps.Count
        };
    }

    public async System.Threading.Tasks.Task CompleteStepAsync(CompleteStepRequest request)
    {
        if (!await _userDataService.UserExistsAsync(request.UserId))
        {
            throw new KeyNotFoundException($"User with ID {request.UserId} not found");
        }

        var step = await _stepDataService.GetStepByNameAsync(request.StepName);
        if (step == null)
        {
            throw new ArgumentException($"Step '{request.StepName}' not found");
        }

        var userProgress = await _userProgressDataService.GetOrCreateUserProgressAsync(request.UserId);

        // Determine which task this payload corresponds to
        var task = await DetermineTaskFromPayloadAsync(step, request.StepPayload, userProgress);

        if (task != null)
        {
            // Mark task as completed
            var taskKey = $"{step.Name}_{task.Name}";
            var passed = await _conditionEvaluator.EvaluatePassingConditionAsync(
                task.PassingConditionType,
                task.PassingConditionConfig,
                request.StepPayload);

            userProgress.CompletedTasks[taskKey] = new TaskCompletion
            {
                TaskName = task.Name,
                StepName = step.Name,
                CompletedAt = DateTime.UtcNow,
                Passed = passed,
                Payload = request.StepPayload
            };
        }

        // Check if step is complete (all tasks are done)
        var taskIds = await _stepTaskDataService.GetTaskIdsForStepAsync(step.Id);
        var tasks = new List<FlowTask>();
        foreach (var taskId in taskIds)
        {
            var t = await _taskDataService.GetTaskByIdAsync(taskId);
            if (t != null && t.IsActive)
                tasks.Add(t);
        }

        var visibleTasks = await GetVisibleTasksForUserAsync(step, tasks, request.UserId, userProgress);
        var allTasksCompleted = visibleTasks.All(t =>
        {
            var taskKey = $"{step.Name}_{t.Name}";
            return userProgress.CompletedTasks.ContainsKey(taskKey);
        });

        if (allTasksCompleted)
        {
            // Check if all tasks passed
            var allTasksPassed = visibleTasks.All(t =>
            {
                var taskKey = $"{step.Name}_{t.Name}";
                if (userProgress.CompletedTasks.TryGetValue(taskKey, out var completion))
                {
                    return completion.Passed;
                }
                return false;
            });

            userProgress.CompletedSteps[step.Name] = new StepCompletion
            {
                StepName = step.Name,
                CompletedAt = DateTime.UtcNow,
                Passed = allTasksPassed,
                Payload = request.StepPayload
            };
        }

        await _userProgressDataService.UpdateUserProgressAsync(userProgress);
    }

    public async Task<UserStatusResponse> GetUserStatusAsync(string userId)
    {
        if (!await _userDataService.UserExistsAsync(userId))
        {
            throw new KeyNotFoundException($"User with ID {userId} not found");
        }

        var steps = await _stepDataService.GetActiveStepsAsync();
        var userProgress = await _userProgressDataService.GetOrCreateUserProgressAsync(userId);

        // Check if user completed all steps
        var allStepsCompleted = true;
        foreach (var step in steps)
        {
            var taskIds = await _stepTaskDataService.GetTaskIdsForStepAsync(step.Id);
            var tasks = new List<FlowTask>();
            foreach (var taskId in taskIds)
            {
                var t = await _taskDataService.GetTaskByIdAsync(taskId);
                if (t != null && t.IsActive)
                    tasks.Add(t);
            }

            var visibleTasks = await GetVisibleTasksForUserAsync(step, tasks, userId, userProgress);
            if (!visibleTasks.Any())
            {
                // Step with no tasks is considered complete if step is marked complete
                if (!userProgress.CompletedSteps.ContainsKey(step.Name))
                {
                    allStepsCompleted = false;
                    break;
                }
                continue;
            }

            // All tasks must be completed
            var allTasksCompleted = visibleTasks.All(t =>
            {
                var taskKey = $"{step.Name}_{t.Name}";
                return userProgress.CompletedTasks.ContainsKey(taskKey);
            });

            if (!allTasksCompleted)
            {
                allStepsCompleted = false;
                break;
            }

            // All tasks must have passed
            var allTasksPassed = visibleTasks.All(t =>
            {
                var taskKey = $"{step.Name}_{t.Name}";
                if (userProgress.CompletedTasks.TryGetValue(taskKey, out var completion))
                {
                    return completion.Passed;
                }
                return false; // Tasks not completed are not passed
            });

            if (!allTasksPassed)
            {
                allStepsCompleted = false;
                break;
            }
        }

        if (allStepsCompleted)
        {
            // Check if any step failed (rejected)
            var anyStepFailed = steps.Any(step =>
            {
                if (userProgress.CompletedSteps.TryGetValue(step.Name, out var stepCompletion))
                {
                    return !stepCompletion.Passed;
                }
                return false;
            });

            if (anyStepFailed)
            {
                return new UserStatusResponse { Status = "rejected" };
            }

            return new UserStatusResponse { Status = "accepted" };
        }

        // Check if user is rejected (failed a required step)
        var requiredStepsFailed = steps.Any(step =>
        {
            if (userProgress.CompletedSteps.TryGetValue(step.Name, out var stepCompletion))
            {
                return !stepCompletion.Passed;
            }
            return false;
        });

        if (requiredStepsFailed)
        {
            return new UserStatusResponse { Status = "rejected" };
        }

        return new UserStatusResponse { Status = "in_progress" };
    }

    private async Task<(Step? currentStep, FlowTask? currentTask)> DetermineCurrentStepAndTaskAsync(
        List<Step> steps,
        UserProgress userProgress)
    {
        foreach (var step in steps.OrderBy(s => s.Order))
        {
            var taskIds = await _stepTaskDataService.GetTaskIdsForStepAsync(step.Id);
            var tasks = new List<FlowTask>();
            foreach (var taskId in taskIds)
            {
                var t = await _taskDataService.GetTaskByIdAsync(taskId);
                if (t != null && t.IsActive)
                    tasks.Add(t);
            }

            // Check if step is incomplete
            if (!userProgress.CompletedSteps.ContainsKey(step.Name))
            {
                var visibleTasks = await GetVisibleTasksForUserAsync(step, tasks, "", userProgress);
                // Find first incomplete task
                foreach (var task in visibleTasks.OrderBy(t => 
                    _stepTaskDataService.GetStepTaskAsync(step.Id, t.Id).Result?.Order ?? 0))
                {
                    var taskKey = $"{step.Name}_{task.Name}";
                    if (!userProgress.CompletedTasks.ContainsKey(taskKey))
                    {
                        return (step, task);
                    }
                }

                // All tasks completed but step not marked complete
                return (step, null);
            }
        }

        // All steps completed
        return (null, null);
    }

    private async Task<FlowTask?> DetermineTaskFromPayloadAsync(
        Step step, 
        Dictionary<string, object> payload, 
        UserProgress userProgress)
    {
        var taskIds = await _stepTaskDataService.GetTaskIdsForStepAsync(step.Id);
        var tasks = new List<FlowTask>();
        foreach (var taskId in taskIds)
        {
            var t = await _taskDataService.GetTaskByIdAsync(taskId);
            if (t != null && t.IsActive)
                tasks.Add(t);
        }

        var visibleTasks = await GetVisibleTasksForUserAsync(step, tasks, "", userProgress);
        
        if (visibleTasks.Count == 1)
        {
            return visibleTasks.First();
        }

        // For multi-task steps, try to identify by payload keys
        if (step.Name == "Interview")
        {
            if (payload.ContainsKey("decision"))
                return tasks.FirstOrDefault(t => t.Name == "perform_interview");
            if (payload.ContainsKey("interview_date"))
                return tasks.FirstOrDefault(t => t.Name == "schedule_interview");
        }

        if (step.Name == "Sign Contract")
        {
            if (payload.ContainsKey("passport_number"))
                return tasks.FirstOrDefault(t => t.Name == "upload_identification_document");
            var uploadKey = $"{step.Name}_upload_identification_document";
            if (userProgress.CompletedTasks.ContainsKey(uploadKey))
                return tasks.FirstOrDefault(t => t.Name == "sign_contract");
            return tasks.FirstOrDefault(t => t.Name == "upload_identification_document");
        }

        if (step.Name == "IQ Test")
        {
            if (payload.ContainsKey("score"))
            {
                var firstTestKey = $"{step.Name}_take_iq_test";
                if (userProgress.CompletedTasks.ContainsKey(firstTestKey))
                {
                    if (userProgress.CompletedTasks.TryGetValue(firstTestKey, out var firstTestCompletion))
                    {
                        if (firstTestCompletion.Payload != null &&
                            firstTestCompletion.Payload.TryGetValue("score", out var scoreObj) &&
                            scoreObj is int firstScore &&
                            firstScore >= 60 && firstScore <= 75 && !firstTestCompletion.Passed)
                        {
                            return tasks.FirstOrDefault(t => t.Name == "take_second_chance_iq_test");
                        }
                    }
                }
                return tasks.FirstOrDefault(t => t.Name == "take_iq_test");
            }
        }

        // Default: return first incomplete visible task
        foreach (var task in visibleTasks.OrderBy(t => 
            _stepTaskDataService.GetStepTaskAsync(step.Id, t.Id).Result?.Order ?? 0))
        {
            var taskKey = $"{step.Name}_{task.Name}";
            if (!userProgress.CompletedTasks.ContainsKey(taskKey))
            {
                return task;
            }
        }

        return visibleTasks.FirstOrDefault();
    }

    private async Task<List<FlowTask>> GetVisibleTasksForUserAsync(
        Step step, 
        List<FlowTask> tasks, 
        string userId, 
        UserProgress userProgress)
    {
        var visibleTasks = new List<FlowTask>();

        foreach (var task in tasks)
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
            if (!string.IsNullOrEmpty(task.ConditionalVisibilityType))
            {
                Dictionary<string, object>? contextData = null;
                
                if (task.ConditionalVisibilityType == "score_range")
                {
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
                    userId,
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
