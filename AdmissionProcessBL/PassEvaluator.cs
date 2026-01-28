using AdmissionProcessBL.Interfaces;
using AdmissionProcessDAL.Models;

namespace AdmissionProcessBL;

public class PassEvaluator : IPassEvaluator
{
    public Task<bool> EvaluateAsync(FlowNode node, Dictionary<string, object> payload)
    {
        var result = node.EvaluatePassCondition(payload);
        return Task.FromResult(result);
    }
}
