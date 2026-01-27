namespace AdmissionProcessDAL.Models;

public class VisibilityCondition
{
    public string Type { get; set; } = string.Empty;
    public string Field { get; set; } = string.Empty;
    public int? Min { get; set; }
    public int? Max { get; set; }
    public string? ExpectedValue { get; set; }

    public bool Evaluate(UserProgress progress)
    {
        return Type switch
        {
            "score_range" => EvaluateScoreRange(progress),
            "derived_fact_equals" => EvaluateDerivedFactEquals(progress),
            _ => true
        };
    }

    private bool EvaluateScoreRange(UserProgress progress)
    {
        if (!progress.DerivedFacts.TryGetValue(Field, out var value))
            return false;

        if (value is int intValue)
            return intValue >= (Min ?? int.MinValue) && intValue <= (Max ?? int.MaxValue);

        if (value is long longValue)
            return longValue >= (Min ?? int.MinValue) && longValue <= (Max ?? int.MaxValue);

        if (value is double doubleValue)
            return doubleValue >= (Min ?? int.MinValue) && doubleValue <= (Max ?? int.MaxValue);

        return false;
    }

    private bool EvaluateDerivedFactEquals(UserProgress progress)
    {
        if (!progress.DerivedFacts.TryGetValue(Field, out var value))
            return false;

        return value?.ToString()?.Equals(ExpectedValue, StringComparison.OrdinalIgnoreCase) ?? false;
    }
}
