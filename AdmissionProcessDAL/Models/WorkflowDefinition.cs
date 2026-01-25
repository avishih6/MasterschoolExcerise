namespace AdmissionProcessDAL.Models;

public class WorkflowDefinition
{
    public int Id { get; set; }
    public ScopeLevel ScopeLevel { get; set; }
    public int? ScopeEntityId { get; set; } // null for Global
    public string Name { get; set; } = string.Empty;
    public int? DerivedFromDefinitionId { get; set; } // optional, for UI copy-on-write semantics
}
