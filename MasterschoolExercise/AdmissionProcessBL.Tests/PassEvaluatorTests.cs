using AdmissionProcessBL;
using AdmissionProcessDAL.Models;
using Xunit;

namespace AdmissionProcessBL.Tests;

public class PassEvaluatorTests
{
    private readonly PassEvaluator _evaluator;

    public PassEvaluatorTests()
    {
        _evaluator = new PassEvaluator();
    }

    [Fact]
    public async Task EvaluateAsync_WithNoPassCondition_ReturnsTrue()
    {
        var node = new FlowNode { PassCondition = null };
        var payload = new Dictionary<string, object>();

        var result = await _evaluator.EvaluateAsync(node, payload);

        Assert.True(result);
    }

    [Fact]
    public async Task EvaluateAsync_IqTest_ScoreAboveThreshold_ReturnsTrue()
    {
        var node = new FlowNode
        {
            PassCondition = new Condition
            {
                Type = ConditionTypes.ScoreThreshold,
                Field = "score",
                Threshold = 75
            }
        };
        var payload = new Dictionary<string, object> { { "score", 80 } };

        var result = await _evaluator.EvaluateAsync(node, payload);

        Assert.True(result);
    }

    [Fact]
    public async Task EvaluateAsync_IqTest_ScoreBelowThreshold_ReturnsFalse()
    {
        var node = new FlowNode
        {
            PassCondition = new Condition
            {
                Type = ConditionTypes.ScoreThreshold,
                Field = "score",
                Threshold = 75
            }
        };
        var payload = new Dictionary<string, object> { { "score", 70 } };

        var result = await _evaluator.EvaluateAsync(node, payload);

        Assert.False(result);
    }

    [Fact]
    public async Task EvaluateAsync_Interview_PassedDecision_ReturnsTrue()
    {
        var node = new FlowNode
        {
            PassCondition = new Condition
            {
                Type = ConditionTypes.DecisionEquals,
                Field = "decision",
                ExpectedValue = "passed_interview"
            }
        };
        var payload = new Dictionary<string, object> { { "decision", "passed_interview" } };

        var result = await _evaluator.EvaluateAsync(node, payload);

        Assert.True(result);
    }

    [Fact]
    public async Task EvaluateAsync_Interview_FailedDecision_ReturnsFalse()
    {
        var node = new FlowNode
        {
            PassCondition = new Condition
            {
                Type = ConditionTypes.DecisionEquals,
                Field = "decision",
                ExpectedValue = "passed_interview"
            }
        };
        var payload = new Dictionary<string, object> { { "decision", "failed_interview" } };

        var result = await _evaluator.EvaluateAsync(node, payload);

        Assert.False(result);
    }
}
