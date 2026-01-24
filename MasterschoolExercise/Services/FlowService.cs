using MasterschoolExercise.Configuration;
using MasterschoolExercise.Models.DTOs;
using MasterschoolExercise.Repositories;

namespace MasterschoolExercise.Services;

public class FlowService : IFlowService
{
    private readonly IFlowConfiguration _flowConfiguration;
    private readonly IUserProgressRepository _progressRepository;

    public FlowService(IFlowConfiguration flowConfiguration, IUserProgressRepository progressRepository)
    {
        _flowConfiguration = flowConfiguration;
        _progressRepository = progressRepository;
    }

    public async Task<FlowResponse> GetFlowAsync(string? userId = null)
    {
        var flow = _flowConfiguration.GetFlow();
        var userProgress = userId != null ? await _progressRepository.GetUserProgressAsync(userId) : null;

        var response = new FlowResponse
        {
            Steps = flow.Select(step => new FlowStepDto
            {
                Name = step.Name,
                Order = step.Order,
                Tasks = GetVisibleTasks(step, userProgress)
                    .Select(task => new FlowTaskDto
                    {
                        Name = task.Name,
                        StepName = task.StepName
                    })
                    .ToList()
            }).ToList()
        };

        return response;
    }

    private List<Models.Task> GetVisibleTasks(Models.Step step, Models.UserProgress? userProgress)
    {
        var visibleTasks = new List<Models.Task>();

        foreach (var task in step.Tasks)
        {
            // Check conditional visibility
            if (task.ConditionalVisibility != null && userProgress != null)
            {
                // For second chance IQ test: only show if first test score was 60-75
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
