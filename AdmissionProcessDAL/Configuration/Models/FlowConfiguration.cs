using AdmissionProcessDAL.Models;

namespace AdmissionProcessDAL.Configuration.Models;

public class FlowConfiguration
{
    public List<StepConfiguration> Steps { get; set; } = new();
}

public class StepConfiguration
{
    public int NodeId { get; set; }
    public List<TaskConfiguration> Tasks { get; set; } = new();
}

public class TaskConfiguration
{
    public int NodeId { get; set; }
    public VisibilityCondition? VisibilityCondition { get; set; }
    public PassCondition? PassCondition { get; set; }
    public List<string>? PayloadIdentifiers { get; set; }
    public int? RequiresPreviousTaskFailedId { get; set; }
    public List<DerivedFactMapping>? DerivedFactsToStore { get; set; }
}
