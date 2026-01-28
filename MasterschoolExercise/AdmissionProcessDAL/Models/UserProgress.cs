using AdmissionProcessModels.Enums;

namespace AdmissionProcessDAL.Models;

public class UserProgress
{
    public string UserId { get; set; } = string.Empty;
    public Dictionary<int, NodeStatus> NodeStatuses { get; set; } = new();
    public Dictionary<string, object> DerivedFacts { get; set; } = new();
    
    public int? CurrentStepId { get; set; }
    public int? CurrentTaskId { get; set; }
    public UserStatus CachedOverallStatus { get; set; } = UserStatus.InProgress;
    public DateTime CacheUpdatedAt { get; set; }
}
