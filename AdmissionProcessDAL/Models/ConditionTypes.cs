namespace AdmissionProcessDAL.Models;

/// <summary>
/// Predefined condition types for flow configuration.
/// New types can be added here and handled in Condition.EvaluateVisibility/EvaluatePass.
/// </summary>
public static class ConditionTypes
{
    // Visibility conditions (evaluated against UserProgress.DerivedFacts)
    public const string ScoreRange = "score_range";
    public const string DerivedFactEquals = "derived_fact_equals";
    
    // Pass conditions (evaluated against step payload)
    public const string ScoreThreshold = "score_threshold";
    public const string DecisionEquals = "decision_equals";
}
