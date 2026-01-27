namespace AdmissionProcessDAL.Models;

public class PassCondition
{
    public string Type { get; set; } = string.Empty;
    public string Field { get; set; } = string.Empty;
    public int? Threshold { get; set; }
    public string? ExpectedValue { get; set; }

    public bool Evaluate(Dictionary<string, object> payload)
    {
        return Type switch
        {
            ConditionTypes.ScoreThreshold => EvaluateScoreThreshold(payload),
            ConditionTypes.DecisionEquals => EvaluateDecisionEquals(payload),
            _ => true
        };
    }

    private bool EvaluateScoreThreshold(Dictionary<string, object> payload)
    {
        if (!payload.TryGetValue(Field, out var value))
            return false;

        var numericValue = ConvertToDouble(value);
        if (!numericValue.HasValue)
            return false;

        return numericValue.Value > (Threshold ?? 0);
    }

    private bool EvaluateDecisionEquals(Dictionary<string, object> payload)
    {
        if (!payload.TryGetValue(Field, out var value))
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
