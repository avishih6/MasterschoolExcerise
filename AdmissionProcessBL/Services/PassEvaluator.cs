using AdmissionProcessBL.Services.Interfaces;
using AdmissionProcessDAL.Models;

namespace AdmissionProcessBL.Services;

public class PassEvaluator : IPassEvaluator
{
    public Task<bool> EvaluateAsync(FlowNode node, Dictionary<string, object> payload)
    {
        if (node.PassCondition == null)
            return Task.FromResult(true);

        var result = node.PassCondition.Evaluate(payload);
        return Task.FromResult(result);
    }
}
