using MasterschoolExercise.Configuration;
using MasterschoolExercise.Models;
using MasterschoolExercise.Models.DTOs;
using MasterschoolExercise.Repositories;

namespace MasterschoolExercise.Services;

public class ProgressService : IProgressService
{
    private readonly IFlowConfiguration _flowConfiguration;
    private readonly IUserProgressRepository _progressRepository;
    private readonly IUserRepository _userRepository;

    public ProgressService(
        IFlowConfiguration flowConfiguration,
        IUserProgressRepository progressRepository,
        IUserRepository userRepository)
    {
        _flowConfiguration = flowConfiguration;
        _progressRepository = progressRepository;
        _userRepository = userRepository;
    }

    public async Task<UserProgressResponse> GetUserProgressAsync(string userId)
    {
        if (!await _userRepository.UserExistsAsync(userId))
        {
            throw new KeyNotFoundException($"User with ID {userId} not found");
        }

        var flow = _flowConfiguration.GetFlow();
        var userProgress = await _progressRepository.GetOrCreateUserProgressAsync(userId);

        var (currentStep, currentTask) = DetermineCurrentStepAndTask(flow, userProgress);

        return new UserProgressResponse
        {
            CurrentStep = currentStep?.Name,
            CurrentTask = currentTask?.Name,
            CurrentStepNumber = currentStep != null ? currentStep.Order : 0,
            TotalSteps = flow.Count
        };
    }

    public async Task CompleteStepAsync(CompleteStepRequest request)
    {
        if (!await _userRepository.UserExistsAsync(request.UserId))
        {
            throw new KeyNotFoundException($"User with ID {request.UserId} not found");
        }

        var step = _flowConfiguration.GetStepByName(request.StepName);
        if (step == null)
        {
            throw new ArgumentException($"Step '{request.StepName}' not found");
        }

        var userProgress = await _progressRepository.GetOrCreateUserProgressAsync(request.UserId);

        // Determine which task this payload corresponds to based on step name and payload content
        var task = DetermineTaskFromPayload(step, request.StepPayload, userProgress);

        if (task != null)
        {
            // Mark task as completed
            var taskKey = $"{step.Name}_{task.Name}";
            var passed = task.PassingCondition?.Invoke(request.StepPayload) ?? true;

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
        var visibleTasks = GetVisibleTasksForUser(step, userProgress);
        var allTasksCompleted = visibleTasks.All(t =>
        {
            var taskKey = $"{step.Name}_{t.Name}";
            return userProgress.CompletedTasks.ContainsKey(taskKey);
        });

        if (allTasksCompleted)
        {
            // Check if all tasks passed (if they have passing conditions)
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

        await _progressRepository.UpdateUserProgressAsync(userProgress);
    }

    public async Task<UserStatusResponse> GetUserStatusAsync(string userId)
    {
        if (!await _userRepository.UserExistsAsync(userId))
        {
            throw new KeyNotFoundException($"User with ID {userId} not found");
        }

        var flow = _flowConfiguration.GetFlow();
        var userProgress = await _progressRepository.GetOrCreateUserProgressAsync(userId);

        // Check if user completed all steps
        var allStepsCompleted = flow.All(step =>
        {
            var visibleTasks = GetVisibleTasksForUser(step, userProgress);
            if (!visibleTasks.Any())
            {
                // Step with no tasks is considered complete if step is marked complete
                return userProgress.CompletedSteps.ContainsKey(step.Name);
            }

            // All tasks must be completed
            var allTasksCompleted = visibleTasks.All(t =>
            {
                var taskKey = $"{step.Name}_{t.Name}";
                return userProgress.CompletedTasks.ContainsKey(taskKey);
            });

            if (!allTasksCompleted)
                return false;

            // All tasks must have passed (if they have passing conditions)
            return visibleTasks.All(t =>
            {
                var taskKey = $"{step.Name}_{t.Name}";
                if (userProgress.CompletedTasks.TryGetValue(taskKey, out var completion))
                {
                    return completion.Passed;
                }
                return true; // Tasks without passing conditions are considered passed
            });
        });

        if (allStepsCompleted)
        {
            // Check if any step failed (rejected)
            var anyStepFailed = flow.Any(step =>
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
        var requiredStepsFailed = flow.Any(step =>
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

    private (Step? currentStep, Task? currentTask) DetermineCurrentStepAndTask(
        List<Step> flow,
        UserProgress userProgress)
    {
        foreach (var step in flow.OrderBy(s => s.Order))
        {
            var visibleTasks = GetVisibleTasksForUser(step, userProgress);

            // Check if step is incomplete
            if (!userProgress.CompletedSteps.ContainsKey(step.Name))
            {
                // Find first incomplete task
                foreach (var task in visibleTasks)
                {
                    var taskKey = $"{step.Name}_{task.Name}";
                    if (!userProgress.CompletedTasks.ContainsKey(taskKey))
                    {
                        return (step, task);
                    }
                }

                // All tasks completed but step not marked complete (shouldn't happen, but handle it)
                return (step, null);
            }
        }

        // All steps completed
        return (null, null);
    }

    private Task? DetermineTaskFromPayload(Step step, Dictionary<string, object> payload, UserProgress userProgress)
    {
        // Try to determine task from payload structure
        // This is a simplified approach - in a real system, you might have a task identifier in the payload

        // For steps with single tasks, return that task
        var visibleTasks = GetVisibleTasksForUser(step, userProgress);
        if (visibleTasks.Count == 1)
        {
            return visibleTasks.First();
        }

        // For multi-task steps, try to identify by payload keys
        // Interview: schedule_interview has "interview_date", perform_interview has "decision"
        if (step.Name == "Interview")
        {
            if (payload.ContainsKey("decision"))
                return step.Tasks.FirstOrDefault(t => t.Name == "perform_interview");
            if (payload.ContainsKey("interview_date"))
                return step.Tasks.FirstOrDefault(t => t.Name == "schedule_interview");
        }

        // Sign Contract: upload_identification_document has "passport_number", sign_contract doesn't
        if (step.Name == "Sign Contract")
        {
            if (payload.ContainsKey("passport_number"))
                return step.Tasks.FirstOrDefault(t => t.Name == "upload_identification_document");
            // Check if upload is already done, then this must be sign_contract
            var uploadKey = $"{step.Name}_upload_identification_document";
            if (userProgress.CompletedTasks.ContainsKey(uploadKey))
                return step.Tasks.FirstOrDefault(t => t.Name == "sign_contract");
            // Default to upload if not done yet
            return step.Tasks.FirstOrDefault(t => t.Name == "upload_identification_document");
        }

        // IQ Test: check for score and determine if first or second chance
        if (step.Name == "IQ Test")
        {
            if (payload.ContainsKey("score"))
            {
                // Check if first test is already completed
                var firstTestKey = $"{step.Name}_take_iq_test";
                if (userProgress.CompletedTasks.ContainsKey(firstTestKey))
                {
                    // First test is done, check if second chance should be available
                    if (userProgress.CompletedTasks.TryGetValue(firstTestKey, out var firstTestCompletion))
                    {
                        if (firstTestCompletion.Payload != null &&
                            firstTestCompletion.Payload.TryGetValue("score", out var scoreObj) &&
                            scoreObj is int firstScore &&
                            firstScore >= 60 && firstScore <= 75 && !firstTestCompletion.Passed)
                        {
                            // Second chance is available and this is likely the second test
                            return step.Tasks.FirstOrDefault(t => t.Name == "take_second_chance_iq_test");
                        }
                    }
                }
                // First test not done or not eligible for second chance, so this is the first test
                return step.Tasks.FirstOrDefault(t => t.Name == "take_iq_test");
            }
        }

        // Default: return first incomplete visible task
        foreach (var task in visibleTasks)
        {
            var taskKey = $"{step.Name}_{task.Name}";
            if (!userProgress.CompletedTasks.ContainsKey(taskKey))
            {
                return task;
            }
        }

        // All tasks completed, return first visible task
        return visibleTasks.FirstOrDefault();
    }

    private List<Task> GetVisibleTasksForUser(Step step, UserProgress userProgress)
    {
        var visibleTasks = new List<Task>();

        foreach (var task in step.Tasks)
        {
            // Handle conditional visibility (e.g., second chance IQ test)
            if (task.Name == "take_second_chance_iq_test")
            {
                var firstTestTask = step.Tasks.FirstOrDefault(t => t.Name == "take_iq_test");
                if (firstTestTask != null)
                {
                    var taskKey = $"{step.Name}_{firstTestTask.Name}";
                    if (userProgress.CompletedTasks.TryGetValue(taskKey, out var firstTestCompletion))
                    {
                        if (firstTestCompletion.Payload != null &&
                            firstTestCompletion.Payload.TryGetValue("score", out var scoreObj) &&
                            scoreObj is int score)
                        {
                            // Only show if score is between 60-75 and first test was not passed
                            if (score >= 60 && score <= 75 && !firstTestCompletion.Passed)
                            {
                                visibleTasks.Add(task);
                            }
                        }
                    }
                }
                continue;
            }

            // Default: show all non-conditional tasks
            if (task.ConditionalVisibility == null)
            {
                visibleTasks.Add(task);
            }
        }

        return visibleTasks;
    }
}
