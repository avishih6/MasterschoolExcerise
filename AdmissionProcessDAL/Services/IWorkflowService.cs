using AdmissionProcessDAL.Models;

namespace AdmissionProcessDAL.Services;

public interface IWorkflowService
{
    Task<WorkflowDefinition?> GetEffectiveDefinitionAsync(int? countryId, int? universityId);
    Task<WorkflowTreeDto> BuildWorkflowWithProgressAsync(int userId, int? countryId, int? universityId);
}

// DTOs for workflow tree with progress
public class WorkflowTreeDto
{
    public WorkflowDefinitionDto Definition { get; set; } = null!;
    public List<StepDto> Steps { get; set; } = new();
}

public class WorkflowDefinitionDto
{
    public int Id { get; set; }
    public ScopeLevel ScopeLevel { get; set; }
    public int? ScopeEntityId { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class StepDto
{
    public int NodeId { get; set; }
    public int PhaseId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Order { get; set; }
    public ProgressStatus Status { get; set; }
    public List<TaskDto> Tasks { get; set; } = new();
}

public class TaskDto
{
    public int NodeId { get; set; }
    public int PhaseId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Order { get; set; }
    public ProgressStatus Status { get; set; }
}
