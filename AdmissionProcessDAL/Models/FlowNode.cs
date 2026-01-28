using AdmissionProcessModels.Enums;

namespace AdmissionProcessDAL.Models;

/// <summary>
/// Represents a step or task in the admission flow.
/// 
/// FUTURE ENHANCEMENTS:
/// - Add OverrideKey property for hierarchy-based customization (country_code, university_id)
/// - Add IsDisabledForOverride to allow specific institutions to skip certain steps
/// - Add OrderOverride to allow reordering steps per institution
/// </summary>
public class FlowNode
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public NodeRole Role { get; set; }
    public int? ParentId { get; set; }
    public int Order { get; set; }
    public Condition? VisibilityCondition { get; set; }
    public Condition? PassCondition { get; set; }
    public List<string> PayloadIdentifiers { get; set; } = new();
    public int? RequiresPreviousTaskFailedId { get; set; }
    public List<DerivedFactMapping>? DerivedFactsToStore { get; set; }

    public bool IsVisibleForUser(UserProgress progress)
    {
        if (VisibilityCondition == null)
            return true;

        return VisibilityCondition.EvaluateVisibility(progress);
    }

    public bool EvaluatePassCondition(Dictionary<string, object> payload)
    {
        if (PassCondition == null)
            return true;

        return PassCondition.EvaluatePass(payload);
    }
}
