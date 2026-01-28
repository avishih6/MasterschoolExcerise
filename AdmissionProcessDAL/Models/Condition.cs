namespace AdmissionProcessDAL.Models;

/// <summary>
/// Unified condition class for visibility and pass evaluations.
/// 
/// FUTURE ENHANCEMENTS:
/// 1. Composite Conditions: Add "Logic" (and/or) and "Conditions" list properties
///    to support complex rules like: (score >= 60 AND score <= 75 AND previous_task_failed)
/// 
/// 2. Hierarchy Overrides: Add support for country/university-level flow customization.
///    Example: A "FlowOverride" entity with (OverrideLevel, OverrideKey, NodeId, NewOrder, IsDisabled)
///    could allow specific institutions to reorder steps or disable certain tasks.
/// </summary>
public class Condition
{
    public string? Type { get; set; }
    public string? Field { get; set; }
    public double? Min { get; set; }
    public double? Max { get; set; }
    public double? Threshold { get; set; }
    public string? ExpectedValue { get; set; }

    public bool EvaluateVisibility(UserProgress progress)
    {
        return Type?.ToLowerInvariant() switch
        {
            ConditionTypes.ScoreRange => EvaluateRange(GetDerivedFactValue(progress)),
            ConditionTypes.DerivedFactEquals => EvaluateDerivedFactEquals(progress),
            _ => true
        };
    }

    public bool EvaluatePass(Dictionary<string, object> payload)
    {
        return Type?.ToLowerInvariant() switch
        {
            ConditionTypes.ScoreThreshold => EvaluateGreaterThan(GetPayloadValue(payload)),
            ConditionTypes.DecisionEquals => EvaluateEquals(GetPayloadValue(payload)),
            _ => true
        };
    }

    private double? GetDerivedFactValue(UserProgress progress)
    {
        if (string.IsNullOrEmpty(Field) || !progress.DerivedFacts.TryGetValue(Field, out var value))
            return null;
        return ConvertToDouble(value);
    }

    private object? GetPayloadValue(Dictionary<string, object> payload)
    {
        if (string.IsNullOrEmpty(Field) || !payload.TryGetValue(Field, out var value))
            return null;
        return value;
    }

    private bool EvaluateRange(double? value)
    {
        if (!value.HasValue) return false;
        return value.Value >= (Min ?? double.MinValue) && value.Value <= (Max ?? double.MaxValue);
    }

    private bool EvaluateGreaterThan(object? value)
    {
        var numericValue = ConvertToDouble(value);
        if (!numericValue.HasValue) return false;
        return numericValue.Value > (Threshold ?? 0);
    }

    private bool EvaluateEquals(object? value)
    {
        return value?.ToString()?.Equals(ExpectedValue, StringComparison.OrdinalIgnoreCase) ?? false;
    }

    private bool EvaluateDerivedFactEquals(UserProgress progress)
    {
        if (string.IsNullOrEmpty(Field) || !progress.DerivedFacts.TryGetValue(Field, out var value))
            return false;
        return value?.ToString()?.Equals(ExpectedValue, StringComparison.OrdinalIgnoreCase) ?? false;
    }

    private static double? ConvertToDouble(object? value)
    {
        return value switch
        {
            null => null,
            int i => i,
            long l => l,
            double d => d,
            float f => f,
            decimal dec => (double)dec,
            string s when double.TryParse(s, out var result) => result,
            _ => null
        };
    }
}
