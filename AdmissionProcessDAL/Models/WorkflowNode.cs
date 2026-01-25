namespace AdmissionProcessDAL.Models;

public class WorkflowNode
{
    public int Id { get; set; }
    public int WorkflowDefinitionId { get; set; }
    public int PhaseId { get; set; }
    public NodeRole Role { get; set; }
    public int? ParentNodeId { get; set; } // null for root nodes (top-level Steps)
    public int Order { get; set; }
    public string Name { get; set; } = string.Empty; // Actual name of the node (e.g., "Upload documents", "Book slot")
    public string? EnableConditionOverride { get; set; }
    public string? VisibilityConditionOverride { get; set; }
}
