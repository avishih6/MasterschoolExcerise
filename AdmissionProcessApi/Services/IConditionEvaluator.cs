namespace AdmissionProcessApi.Services;

public interface IConditionEvaluator
{
    bool EvaluatePassingCondition(string conditionType, string conditionConfig, Dictionary<string, object> payload);
    Task<bool> EvaluatePassingConditionAsync(string conditionType, string conditionConfig, Dictionary<string, object> payload);
    bool EvaluateVisibilityCondition(string conditionType, string conditionConfig, string userId, Dictionary<string, object>? contextData = null);
    Task<bool> EvaluateVisibilityConditionAsync(string conditionType, string conditionConfig, string userId, Dictionary<string, object>? contextData = null);
}
