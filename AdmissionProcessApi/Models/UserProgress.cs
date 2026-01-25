namespace AdmissionProcessApi.Models;

public class UserProgress
{
    public string UserId { get; set; } = string.Empty;
    public Dictionary<string, StepCompletion> CompletedSteps { get; set; } = new();
    public Dictionary<string, TaskCompletion> CompletedTasks { get; set; } = new();
}

public class StepCompletion
{
    public string StepName { get; set; } = string.Empty;
    public DateTime CompletedAt { get; set; }
    public bool Passed { get; set; }
    public Dictionary<string, object>? Payload { get; set; }
}

public class TaskCompletion
{
    public string TaskName { get; set; } = string.Empty;
    public string StepName { get; set; } = string.Empty;
    public DateTime CompletedAt { get; set; }
    public bool Passed { get; set; }
    public Dictionary<string, object>? Payload { get; set; }
}
