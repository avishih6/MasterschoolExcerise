namespace AdmissionProcessDAL.Models;

public class FlowNode
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public NodeRole Role { get; set; }
    public int? ParentId { get; set; }
    public int Order { get; set; }
    public VisibilityCondition? VisibilityCondition { get; set; }
    public PassCondition? PassCondition { get; set; }
    public List<string> PayloadIdentifiers { get; set; } = new();
    public int? RequiresPreviousTaskFailedId { get; set; }
    public List<DerivedFactMapping>? DerivedFactsToStore { get; set; }

    public bool IsVisibleForUser(UserProgress progress)
    {
        if (VisibilityCondition == null)
            return true;

        return VisibilityCondition.Evaluate(progress);
    }
}

public enum NodeRole
{
    Step = 1,
    Task = 2
}

public class DerivedFactMapping
{
    public string SourceField { get; set; } = string.Empty;
    public string TargetFactName { get; set; } = string.Empty;
}
