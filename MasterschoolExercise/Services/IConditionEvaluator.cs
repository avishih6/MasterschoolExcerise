namespace MasterschoolExercise.Services;

public interface IConditionEvaluator
{
    bool EvaluatePassingCondition(string conditionType, string conditionConfig, Dictionary<string, object> payload);
    bool EvaluateVisibilityCondition(string conditionType, string conditionConfig, string userId, Dictionary<string, object>? contextData = null);
}
