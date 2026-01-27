using AdmissionProcessDAL.Models;

namespace AdmissionProcessBL.Services.Interfaces;

public interface IPassEvaluator
{
    Task<bool> EvaluateAsync(FlowNode node, Dictionary<string, object> payload);
}
