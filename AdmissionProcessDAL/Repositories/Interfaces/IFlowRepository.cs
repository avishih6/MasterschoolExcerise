using AdmissionProcessDAL.Models;

namespace AdmissionProcessDAL.Repositories.Interfaces;

public interface IFlowRepository
{
    Task<List<FlowNode>> GetAllNodesAsync();
    Task<FlowNode?> GetNodeByIdAsync(int nodeId);
    Task<List<FlowNode>> GetChildNodesAsync(int parentId);
    Task<List<FlowNode>> GetRootStepsAsync();
}
