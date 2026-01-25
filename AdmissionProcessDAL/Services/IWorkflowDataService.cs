using AdmissionProcessDAL.Models;

namespace AdmissionProcessDAL.Services;

public interface IWorkflowDataService
{
    // WorkflowDefinition operations
    Task<WorkflowDefinition> CreateWorkflowDefinitionAsync(WorkflowDefinition definition);
    Task<WorkflowDefinition?> GetWorkflowDefinitionByIdAsync(int id);
    Task<WorkflowDefinition?> GetWorkflowDefinitionByScopeAsync(ScopeLevel scopeLevel, int? scopeEntityId);
    Task<List<WorkflowDefinition>> GetAllWorkflowDefinitionsAsync();
    
    // WorkflowNode operations
    Task<WorkflowNode> CreateWorkflowNodeAsync(WorkflowNode node);
    Task<WorkflowNode?> GetWorkflowNodeByIdAsync(int id);
    Task<List<WorkflowNode>> GetNodesByWorkflowDefinitionIdAsync(int workflowDefinitionId);
    Task<List<WorkflowNode>> GetChildNodesAsync(int parentNodeId);
    
    // Phase operations
    Task<Phase> CreatePhaseAsync(Phase phase);
    Task<Phase?> GetPhaseByIdAsync(int id);
    Task<List<Phase>> GetAllPhasesAsync();
    
    // UserNodeStatus operations
    Task<UserNodeStatus> CreateOrUpdateUserNodeStatusAsync(UserNodeStatus status);
    Task<UserNodeStatus?> GetUserNodeStatusAsync(int userId, int workflowDefinitionId, int nodeId);
    Task<List<UserNodeStatus>> GetUserNodeStatusesAsync(int userId, int workflowDefinitionId);
    Task<Dictionary<int, ProgressStatus>> GetUserProgressMapAsync(int userId, int workflowDefinitionId);
    
    // Effective definition resolution
    Task<WorkflowDefinition?> GetEffectiveDefinitionAsync(int? countryId, int? universityId);
}
