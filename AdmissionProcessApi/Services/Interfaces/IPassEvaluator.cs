using AdmissionProcessDAL.Models;

namespace AdmissionProcessApi.Services;

public interface IPassEvaluator
{
    Task<bool> EvaluateAsync(FlowNode node, Dictionary<string, object> payload);
}
