namespace AdmissionProcessDAL.Models;

public class UserProgress
{
    public string UserId { get; set; } = string.Empty;
    public Dictionary<int, NodeStatus> NodeStatuses { get; set; } = new();
    public Dictionary<string, object> DerivedFacts { get; set; } = new();
    
    public int? CurrentStepId { get; set; }
    public int? CurrentTaskId { get; set; }
    public ProgressStatus CachedOverallStatus { get; set; } = ProgressStatus.NotStarted;
    public DateTime CacheUpdatedAt { get; set; }
}

public class NodeStatus
{
    public ProgressStatus Status { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public enum ProgressStatus
{
    NotStarted = 0,
    Accepted = 1,
    Rejected = 2
}
