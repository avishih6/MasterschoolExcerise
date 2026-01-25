namespace AdmissionProcessApi.Models;

public class Step
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Order { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsActive { get; set; } = true;
}

public class FlowTask
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string PassingConditionType { get; set; } = string.Empty; // "always", "score_threshold", "decision_match", "custom"
    public string PassingConditionConfig { get; set; } = string.Empty; // JSON config for the condition
    public string ConditionalVisibilityType { get; set; } = string.Empty; // "always", "score_range", "user_specific", "custom"
    public string ConditionalVisibilityConfig { get; set; } = string.Empty; // JSON config for visibility
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsActive { get; set; } = true;
}

// Junction table for many-to-many relationship between Steps and Tasks
public class StepTask
{
    public int Id { get; set; }
    public int StepId { get; set; }
    public int TaskId { get; set; }
    public int Order { get; set; } // Order of task within the step
    public bool IsRequired { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

// User-specific task assignments (for conditional tasks)
public class UserTaskAssignment
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int TaskId { get; set; }
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
}
