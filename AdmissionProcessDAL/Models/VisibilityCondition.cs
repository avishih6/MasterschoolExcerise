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
            ConditionTypes.ScoreRange => EvaluateScoreRange(progress),
            ConditionTypes.DerivedFactEquals => EvaluateDerivedFactEquals(progress),
            _ => true
        };
    }

    private bool EvaluateScoreRange(UserProgress progress)
    {
        if (!progress.DerivedFacts.TryGetValue(Field, out var value))
            return false;

        var numericValue = ConvertToDouble(value);
        if (!numericValue.HasValue)
            return false;

        return numericValue.Value >= (Min ?? int.MinValue) && numericValue.Value <= (Max ?? int.MaxValue);
    }

    private bool EvaluateDerivedFactEquals(UserProgress progress)
    {
        if (!progress.DerivedFacts.TryGetValue(Field, out var value))
            return false;

        return value?.ToString()?.Equals(ExpectedValue, StringComparison.OrdinalIgnoreCase) ?? false;
    }

    private static double? ConvertToDouble(object value)
    {
        return value switch
        {
            int i => i,
            long l => l,
            double d => d,
            float f => f,
            _ => null
        };
    }
}
