using System.Text.Json;

namespace MasterschoolExercise.Services;

public class ConditionEvaluator : IConditionEvaluator
{
    public bool EvaluatePassingCondition(string conditionType, string conditionConfig, Dictionary<string, object> payload)
    {
        return conditionType switch
        {
            "always" => true,
            "score_threshold" => EvaluateScoreThreshold(conditionConfig, payload),
            "decision_match" => EvaluateDecisionMatch(conditionConfig, payload),
            "custom" => EvaluateCustomCondition(conditionConfig, payload),
            _ => true // Default: always pass
        };
    }

    public bool EvaluateVisibilityCondition(string conditionType, string conditionConfig, string userId, Dictionary<string, object>? contextData = null)
    {
        return conditionType switch
        {
            "always" => true,
            "score_range" => EvaluateScoreRange(conditionConfig, contextData),
            "user_specific" => EvaluateUserSpecific(conditionConfig, userId),
            "custom" => EvaluateCustomVisibility(conditionConfig, userId, contextData),
            _ => true // Default: always visible
        };
    }

    private bool EvaluateScoreThreshold(string config, Dictionary<string, object> payload)
    {
        try
        {
            var configObj = JsonSerializer.Deserialize<Dictionary<string, object>>(config);
            if (configObj != null && configObj.TryGetValue("threshold", out var thresholdObj))
            {
                var threshold = Convert.ToInt32(thresholdObj);
                if (payload.TryGetValue("score", out var scoreObj))
                {
                    var score = Convert.ToInt32(scoreObj);
                    return score > threshold;
                }
            }
        }
        catch { }
        return false;
    }

    private bool EvaluateDecisionMatch(string config, Dictionary<string, object> payload)
    {
        try
        {
            var configObj = JsonSerializer.Deserialize<Dictionary<string, object>>(config);
            if (configObj != null && configObj.TryGetValue("expectedValue", out var expectedObj))
            {
                var expected = expectedObj?.ToString()?.ToLower();
                if (payload.TryGetValue("decision", out var decisionObj))
                {
                    var decision = decisionObj?.ToString()?.ToLower();
                    return decision == expected;
                }
            }
        }
        catch { }
        return false;
    }

    private bool EvaluateCustomCondition(string config, Dictionary<string, object> payload)
    {
        // For custom conditions, you could implement a script evaluator
        // For now, return true
        return true;
    }

    private bool EvaluateScoreRange(string config, Dictionary<string, object>? contextData)
    {
        if (contextData == null) return false;
        
        try
        {
            var configObj = JsonSerializer.Deserialize<Dictionary<string, object>>(config);
            if (configObj != null && 
                configObj.TryGetValue("min", out var minObj) &&
                configObj.TryGetValue("max", out var maxObj))
            {
                var min = Convert.ToInt32(minObj);
                var max = Convert.ToInt32(maxObj);
                
                if (contextData.TryGetValue("score", out var scoreObj))
                {
                    var score = Convert.ToInt32(scoreObj);
                    return score >= min && score <= max;
                }
            }
        }
        catch { }
        return false;
    }

    private bool EvaluateUserSpecific(string config, string userId)
    {
        try
        {
            var configObj = JsonSerializer.Deserialize<Dictionary<string, object>>(config);
            if (configObj != null && configObj.TryGetValue("userIds", out var userIdsObj))
            {
                var userIds = JsonSerializer.Deserialize<List<string>>(userIdsObj?.ToString() ?? "[]");
                return userIds?.Contains(userId) ?? false;
            }
        }
        catch { }
        return false;
    }

    private bool EvaluateCustomVisibility(string config, string userId, Dictionary<string, object>? contextData)
    {
        // For custom visibility, you could implement a script evaluator
        // For now, return true
        return true;
    }
}
