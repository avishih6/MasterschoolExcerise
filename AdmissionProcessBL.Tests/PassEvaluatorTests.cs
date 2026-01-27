using AdmissionProcessBL.Services;
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
    public async Task EvaluateAsync_WithScoreAboveThreshold_ReturnsTrue()
    {
        var node = new FlowNode
        {
            PassCondition = new PassCondition
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
    public async Task EvaluateAsync_WithScoreBelowThreshold_ReturnsFalse()
    {
        var node = new FlowNode
        {
            PassCondition = new PassCondition
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
    public async Task EvaluateAsync_WithScoreEqualToThreshold_ReturnsFalse()
    {
        var node = new FlowNode
        {
            PassCondition = new PassCondition
            {
                Type = ConditionTypes.ScoreThreshold,
                Field = "score",
                Threshold = 75
            }
        };
        var payload = new Dictionary<string, object> { { "score", 75 } };

        var result = await _evaluator.EvaluateAsync(node, payload);

        Assert.False(result);
    }

    [Fact]
    public async Task EvaluateAsync_WithExpectedDecision_ReturnsTrue()
    {
        var node = new FlowNode
        {
            PassCondition = new PassCondition
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
    public async Task EvaluateAsync_WithUnexpectedDecision_ReturnsFalse()
    {
        var node = new FlowNode
        {
            PassCondition = new PassCondition
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

    [Fact]
    public async Task EvaluateAsync_WithMissingField_ReturnsFalse()
    {
        var node = new FlowNode
        {
            PassCondition = new PassCondition
            {
                Type = ConditionTypes.ScoreThreshold,
                Field = "score",
                Threshold = 75
            }
        };
        var payload = new Dictionary<string, object>();

        var result = await _evaluator.EvaluateAsync(node, payload);

        Assert.False(result);
    }
}
