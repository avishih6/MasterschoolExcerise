using AdmissionProcessDAL.Models;

namespace AdmissionProcessDAL.Services;

public class WorkflowDataService : IWorkflowDataService
{
    // Mock database - all storage in DAL services
    private readonly Dictionary<int, WorkflowDefinition> _workflowDefinitions = new();
    private readonly Dictionary<int, WorkflowNode> _workflowNodes = new();
    private readonly Dictionary<int, Phase> _phases = new();
    private readonly Dictionary<string, UserNodeStatus> _userNodeStatuses = new(); // Key: "userId_workflowDefinitionId_nodeId"
    
    // Lookup dictionaries for efficient queries
    private readonly Dictionary<string, WorkflowDefinition> _scopeLookup = new(); // Key: "ScopeLevel_ScopeEntityId"
    private readonly Dictionary<int, List<WorkflowNode>> _nodesByDefinition = new();
    private readonly Dictionary<int, List<WorkflowNode>> _childNodesByParent = new();
    
    private int _nextDefinitionId = 1;
    private int _nextNodeId = 1;
    private int _nextPhaseId = 1;

    // WorkflowDefinition operations
    public async Task<WorkflowDefinition> CreateWorkflowDefinitionAsync(WorkflowDefinition definition)
    {
        definition.Id = _nextDefinitionId++;
        _workflowDefinitions[definition.Id] = definition;
        
        // Update scope lookup
        var scopeKey = GetScopeKey(definition.ScopeLevel, definition.ScopeEntityId);
        _scopeLookup[scopeKey] = definition;
        
        return await Task.FromResult(definition);
    }

    public async Task<WorkflowDefinition?> GetWorkflowDefinitionByIdAsync(int id)
    {
        _workflowDefinitions.TryGetValue(id, out var definition);
        return await Task.FromResult(definition);
    }

    public async Task<WorkflowDefinition?> GetWorkflowDefinitionByScopeAsync(ScopeLevel scopeLevel, int? scopeEntityId)
    {
        var scopeKey = GetScopeKey(scopeLevel, scopeEntityId);
        _scopeLookup.TryGetValue(scopeKey, out var definition);
        return await Task.FromResult(definition);
    }

    public async Task<List<WorkflowDefinition>> GetAllWorkflowDefinitionsAsync()
    {
        return await Task.FromResult(_workflowDefinitions.Values.ToList());
    }

    // WorkflowNode operations
    public async Task<WorkflowNode> CreateWorkflowNodeAsync(WorkflowNode node)
    {
        node.Id = _nextNodeId++;
        _workflowNodes[node.Id] = node;
        
        // Update lookup dictionaries
        if (!_nodesByDefinition.ContainsKey(node.WorkflowDefinitionId))
            _nodesByDefinition[node.WorkflowDefinitionId] = new List<WorkflowNode>();
        _nodesByDefinition[node.WorkflowDefinitionId].Add(node);
        
        if (node.ParentNodeId.HasValue)
        {
            if (!_childNodesByParent.ContainsKey(node.ParentNodeId.Value))
                _childNodesByParent[node.ParentNodeId.Value] = new List<WorkflowNode>();
            _childNodesByParent[node.ParentNodeId.Value].Add(node);
        }
        
        return await Task.FromResult(node);
    }

    public async Task<WorkflowNode?> GetWorkflowNodeByIdAsync(int id)
    {
        _workflowNodes.TryGetValue(id, out var node);
        return await Task.FromResult(node);
    }

    public async Task<List<WorkflowNode>> GetNodesByWorkflowDefinitionIdAsync(int workflowDefinitionId)
    {
        if (_nodesByDefinition.TryGetValue(workflowDefinitionId, out var nodes))
        {
            return await Task.FromResult(nodes.OrderBy(n => n.Order).ToList());
        }
        return await Task.FromResult(new List<WorkflowNode>());
    }

    public async Task<List<WorkflowNode>> GetChildNodesAsync(int parentNodeId)
    {
        if (_childNodesByParent.TryGetValue(parentNodeId, out var nodes))
        {
            return await Task.FromResult(nodes.OrderBy(n => n.Order).ToList());
        }
        return await Task.FromResult(new List<WorkflowNode>());
    }

    // Phase operations
    public async Task<Phase> CreatePhaseAsync(Phase phase)
    {
        phase.Id = _nextPhaseId++;
        _phases[phase.Id] = phase;
        return await Task.FromResult(phase);
    }

    public async Task<Phase?> GetPhaseByIdAsync(int id)
    {
        _phases.TryGetValue(id, out var phase);
        return await Task.FromResult(phase);
    }

    public async Task<List<Phase>> GetAllPhasesAsync()
    {
        return await Task.FromResult(_phases.Values.ToList());
    }

    // UserNodeStatus operations
    public async Task<UserNodeStatus> CreateOrUpdateUserNodeStatusAsync(UserNodeStatus status)
    {
        var key = GetUserNodeStatusKey(status.UserId, status.WorkflowDefinitionId, status.NodeId);
        status.UpdatedAtUtc = DateTime.UtcNow;
        _userNodeStatuses[key] = status;
        return await Task.FromResult(status);
    }

    public async Task<UserNodeStatus?> GetUserNodeStatusAsync(int userId, int workflowDefinitionId, int nodeId)
    {
        var key = GetUserNodeStatusKey(userId, workflowDefinitionId, nodeId);
        _userNodeStatuses.TryGetValue(key, out var status);
        return await Task.FromResult(status);
    }

    public async Task<List<UserNodeStatus>> GetUserNodeStatusesAsync(int userId, int workflowDefinitionId)
    {
        var prefix = $"{userId}_{workflowDefinitionId}_";
        var statuses = _userNodeStatuses.Values
            .Where(s => s.UserId == userId && s.WorkflowDefinitionId == workflowDefinitionId)
            .ToList();
        return await Task.FromResult(statuses);
    }

    public async Task<Dictionary<int, ProgressStatus>> GetUserProgressMapAsync(int userId, int workflowDefinitionId)
    {
        var statuses = await GetUserNodeStatusesAsync(userId, workflowDefinitionId);
        return await Task.FromResult(statuses.ToDictionary(s => s.NodeId, s => s.Status));
    }

    // Effective definition resolution
    public async Task<WorkflowDefinition?> GetEffectiveDefinitionAsync(int? countryId, int? universityId)
    {
        // Fallback order: University -> Country -> Global
        if (universityId.HasValue)
        {
            var universityDef = await GetWorkflowDefinitionByScopeAsync(ScopeLevel.University, universityId);
            if (universityDef != null)
                return universityDef;
        }
        
        if (countryId.HasValue)
        {
            var countryDef = await GetWorkflowDefinitionByScopeAsync(ScopeLevel.Country, countryId);
            if (countryDef != null)
                return countryDef;
        }
        
        return await GetWorkflowDefinitionByScopeAsync(ScopeLevel.Global, null);
    }

    // Helper methods
    private string GetScopeKey(ScopeLevel scopeLevel, int? scopeEntityId)
    {
        return scopeEntityId.HasValue 
            ? $"{(byte)scopeLevel}_{scopeEntityId.Value}" 
            : $"{(byte)scopeLevel}_null";
    }

    private string GetUserNodeStatusKey(int userId, int workflowDefinitionId, int nodeId)
    {
        return $"{userId}_{workflowDefinitionId}_{nodeId}";
    }
}
