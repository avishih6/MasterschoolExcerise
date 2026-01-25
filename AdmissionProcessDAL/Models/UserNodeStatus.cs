namespace AdmissionProcessDAL.Models;

public class UserNodeStatus
{
    public int UserId { get; set; }
    public int WorkflowDefinitionId { get; set; }
    public int NodeId { get; set; }
    public ProgressStatus Status { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    
    // Uniqueness: (UserId, WorkflowDefinitionId, NodeId)
}
