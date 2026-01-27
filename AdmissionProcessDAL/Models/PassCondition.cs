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
            "score_threshold" => EvaluateScoreThreshold(payload),
            "decision_equals" => EvaluateDecisionEquals(payload),
            _ => true
        };
    }

    private bool EvaluateScoreThreshold(Dictionary<string, object> payload)
    {
        if (!payload.TryGetValue(Field, out var value))
            return false;

        if (value is int intValue)
            return intValue > (Threshold ?? 0);

        if (value is long longValue)
            return longValue > (Threshold ?? 0);

        if (value is double doubleValue)
            return doubleValue > (Threshold ?? 0);

        return false;
    }

    private bool EvaluateDecisionEquals(Dictionary<string, object> payload)
    {
        if (!payload.TryGetValue(Field, out var value))
            return false;

        return value?.ToString()?.Equals(ExpectedValue, StringComparison.OrdinalIgnoreCase) ?? false;
    }
}
