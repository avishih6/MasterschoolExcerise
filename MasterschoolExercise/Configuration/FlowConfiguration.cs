using MasterschoolExercise.Models;

namespace MasterschoolExercise.Configuration;

public class FlowConfiguration : IFlowConfiguration
{
    private readonly List<Step> _flow;

    public FlowConfiguration()
    {
        _flow = InitializeFlow();
    }

    public List<Step> GetFlow()
    {
        return _flow.OrderBy(s => s.Order).ToList();
    }

    public Step? GetStepByName(string stepName)
    {
        return _flow.FirstOrDefault(s => s.Name.Equals(stepName, StringComparison.OrdinalIgnoreCase));
    }

    public Task? GetTaskByName(string stepName, string taskName)
    {
        var step = GetStepByName(stepName);
        return step?.Tasks.FirstOrDefault(t => t.Name.Equals(taskName, StringComparison.OrdinalIgnoreCase));
    }

    private List<Step> InitializeFlow()
    {
        return new List<Step>
        {
            new Step
            {
                Name = "Personal Details Form",
                Order = 1,
                Tasks = new List<Task>
                {
                    new Task
                    {
                        Name = "complete_personal_details",
                        StepName = "Personal Details Form"
                        // No passing condition - just completion
                    }
                }
            },
            new Step
            {
                Name = "IQ Test",
                Order = 2,
                Tasks = new List<Task>
                {
                    new Task
                    {
                        Name = "take_iq_test",
                        StepName = "IQ Test",
                        PassingCondition = (payload) =>
                        {
                            if (payload.TryGetValue("score", out var scoreObj) && scoreObj is int score)
                            {
                                return score > 75;
                            }
                            return false;
                        }
                    },
                    // Conditional task: Second chance IQ test (only visible if score is 60-75)
                    new Task
                    {
                        Name = "take_second_chance_iq_test",
                        StepName = "IQ Test",
                        PassingCondition = (payload) =>
                        {
                            if (payload.TryGetValue("score", out var scoreObj) && scoreObj is int score)
                            {
                                return score > 75;
                            }
                            return false;
                        },
                        ConditionalVisibility = (userId, allPayloads) =>
                        {
                            // This task is only visible if the first test score was between 60-75
                            // We'll check this in the service layer based on user progress
                            return false; // Will be evaluated dynamically
                        }
                    }
                }
            },
            new Step
            {
                Name = "Interview",
                Order = 3,
                Tasks = new List<Task>
                {
                    new Task
                    {
                        Name = "schedule_interview",
                        StepName = "Interview"
                        // No passing condition - just completion
                    },
                    new Task
                    {
                        Name = "perform_interview",
                        StepName = "Interview",
                        PassingCondition = (payload) =>
                        {
                            if (payload.TryGetValue("decision", out var decisionObj))
                            {
                                return decisionObj?.ToString()?.ToLower() == "passed_interview";
                            }
                            return false;
                        }
                    }
                }
            },
            new Step
            {
                Name = "Sign Contract",
                Order = 4,
                Tasks = new List<Task>
                {
                    new Task
                    {
                        Name = "upload_identification_document",
                        StepName = "Sign Contract"
                        // No passing condition - just completion
                    },
                    new Task
                    {
                        Name = "sign_contract",
                        StepName = "Sign Contract"
                        // No passing condition - just completion
                    }
                }
            },
            new Step
            {
                Name = "Payment",
                Order = 5,
                Tasks = new List<Task>
                {
                    new Task
                    {
                        Name = "complete_payment",
                        StepName = "Payment"
                        // No passing condition - just completion
                    }
                }
            },
            new Step
            {
                Name = "Join Slack",
                Order = 6,
                Tasks = new List<Task>
                {
                    new Task
                    {
                        Name = "join_slack",
                        StepName = "Join Slack"
                        // No passing condition - just completion
                    }
                }
            }
        };
    }
}
