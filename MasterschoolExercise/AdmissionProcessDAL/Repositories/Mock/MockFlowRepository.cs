using System.Collections.Concurrent;
using AdmissionProcessDAL.Configuration.Models;
using AdmissionProcessDAL.Models;
using AdmissionProcessDAL.Repositories.Interfaces;
using AdmissionProcessModels.Enums;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace AdmissionProcessDAL.Repositories.Mock;

/// <summary>
/// Mock implementation that loads flow configuration from JSON files at startup.
/// 
/// FUTURE: Hot-reload options for runtime configuration updates:
/// 1. File watcher using IOptionsMonitor - auto-reload when JSON files change
/// 2. Admin API endpoint (POST /admin/reload-config) - manual trigger to reload
/// 3. Move configuration to database - changes available immediately via DB queries
/// </summary>
public class MockFlowRepository : IFlowRepository
{
    private readonly ConcurrentDictionary<int, FlowNode> _nodesById = new();
    private readonly ConcurrentDictionary<string, FlowNode> _nodesByName = new();
    private readonly ConcurrentDictionary<int, List<FlowNode>> _childrenByParentId = new();
    private readonly ILogger<MockFlowRepository> _logger;
    
    private List<FlowNode>? _cachedRootSteps;
    private readonly object _rootStepsLock = new();

    public MockFlowRepository(ILogger<MockFlowRepository> logger)
    {
        _logger = logger;
        InitializeAsync().GetAwaiter().GetResult();
    }

    private async Task InitializeAsync()
    {
        var nodesLoaded = await LoadNodesFromConfigurationAsync().ConfigureAwait(false);
        
        if (!nodesLoaded)
        {
            _logger.LogError("Failed to load nodes configuration, using fallback");
            LoadFallbackNodes();
        }

        var flowLoaded = await LoadFlowFromConfigurationAsync().ConfigureAwait(false);
        
        if (!flowLoaded)
        {
            _logger.LogError("Failed to load flow configuration, using fallback");
            LoadFallbackFlow();
        }

        BuildRootStepsCache();
    }

    private async Task<bool> LoadNodesFromConfigurationAsync()
    {
        var configPath = GetConfigurationPath("nodes.json");
        
        if (!File.Exists(configPath))
        {
            _logger.LogError($"Nodes configuration file not found at {configPath}");
            return false;
        }

        try
        {
            var jsonContent = await File.ReadAllTextAsync(configPath).ConfigureAwait(false);
            var nodesConfig = JsonConvert.DeserializeObject<NodesConfiguration>(jsonContent);

            if (nodesConfig?.Nodes == null || nodesConfig.Nodes.Count == 0)
            {
                _logger.LogError("Nodes configuration is empty");
                return false;
            }

            foreach (var nodeDef in nodesConfig.Nodes)
            {
                var node = CreateNodeFromDefinition(nodeDef);
                AddNodeToLookups(node);
            }

            _logger.LogInformation($"Successfully loaded {nodesConfig.Nodes.Count} nodes from configuration");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error loading nodes configuration from {configPath}");
            return false;
        }
    }

    private async Task<bool> LoadFlowFromConfigurationAsync()
    {
        var configPath = GetConfigurationPath("flow-config.json");
        
        if (!File.Exists(configPath))
        {
            _logger.LogError($"Flow configuration file not found at {configPath}");
            return false;
        }

        try
        {
            var jsonContent = await File.ReadAllTextAsync(configPath).ConfigureAwait(false);
            var flowConfig = JsonConvert.DeserializeObject<FlowConfiguration>(jsonContent);

            if (flowConfig?.Steps == null || flowConfig.Steps.Count == 0)
            {
                _logger.LogError("Flow configuration is empty");
                return false;
            }

            ProcessFlowConfiguration(flowConfig);
            
            _logger.LogInformation($"Successfully loaded {flowConfig.Steps.Count} steps from flow configuration");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error loading flow configuration from {configPath}");
            return false;
        }
    }

    private void ProcessFlowConfiguration(FlowConfiguration flowConfig)
    {
        foreach (var stepConfig in flowConfig.Steps)
        {
            ProcessStepConfiguration(stepConfig);
        }
    }

    private void ProcessStepConfiguration(StepConfiguration stepConfig)
    {
        if (!_nodesById.TryGetValue(stepConfig.NodeId, out var stepNode))
        {
            _logger.LogError($"Step node with ID {stepConfig.NodeId} not found in nodes configuration");
            return;
        }

        stepNode.Role = NodeRole.Step;

        foreach (var taskConfig in stepConfig.Tasks)
        {
            ProcessTaskConfiguration(taskConfig, stepNode.Id);
        }
    }

    private void ProcessTaskConfiguration(TaskConfiguration taskConfig, int parentStepId)
    {
        if (!_nodesById.TryGetValue(taskConfig.NodeId, out var taskNode))
        {
            _logger.LogError($"Task node with ID {taskConfig.NodeId} not found in nodes configuration");
            return;
        }

        taskNode.Role = NodeRole.Task;
        taskNode.ParentId = parentStepId;
        taskNode.VisibilityCondition = taskConfig.VisibilityCondition;
        taskNode.PassCondition = taskConfig.PassCondition;
        taskNode.PayloadIdentifiers = taskConfig.PayloadIdentifiers ?? new List<string>();
        taskNode.RequiresPreviousTaskFailedId = taskConfig.RequiresPreviousTaskFailedId;
        taskNode.DerivedFactsToStore = taskConfig.DerivedFactsToStore;

        AddChildToParent(parentStepId, taskNode);
    }

    private static string GetConfigurationPath(string fileName)
    {
        return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Configuration", fileName);
    }

    private static FlowNode CreateNodeFromDefinition(NodeDefinition nodeDef)
    {
        return new FlowNode
        {
            Id = nodeDef.Id,
            Name = nodeDef.Name,
            Order = nodeDef.Order
        };
    }

    private void AddNodeToLookups(FlowNode node)
    {
        _nodesById[node.Id] = node;
        _nodesByName[node.Name.ToLowerInvariant()] = node;
    }

    private void AddChildToParent(int parentId, FlowNode child)
    {
        _childrenByParentId.AddOrUpdate(
            parentId,
            _ => new List<FlowNode> { child },
            (_, existingList) =>
            {
                existingList.Add(child);
                return existingList;
            });
    }

    private void BuildRootStepsCache()
    {
        lock (_rootStepsLock)
        {
            _cachedRootSteps = _nodesById.Values
                .Where(n => n.Role == NodeRole.Step && n.ParentId == null)
                .OrderBy(n => n.Order)
                .ToList();
        }
    }

    private void LoadFallbackNodes()
    {
        var fallbackNodes = GetFallbackNodeDefinitions();
        foreach (var nodeDef in fallbackNodes)
        {
            var node = CreateNodeFromDefinition(nodeDef);
            AddNodeToLookups(node);
        }
    }

    private void LoadFallbackFlow()
    {
        var fallbackSteps = GetFallbackStepConfigurations();
        foreach (var stepConfig in fallbackSteps)
        {
            ProcessStepConfiguration(stepConfig);
        }
    }

    private static List<NodeDefinition> GetFallbackNodeDefinitions()
    {
        return new List<NodeDefinition>
        {
            new() { Id = 1, Name = "Personal Details Form", Order = 1 },
            new() { Id = 10, Name = "Complete personal details", Order = 1 },
            new() { Id = 2, Name = "IQ Test", Order = 2 },
            new() { Id = 20, Name = "Take IQ test", Order = 1 },
            new() { Id = 21, Name = "Take second chance IQ test", Order = 2 },
            new() { Id = 3, Name = "Interview", Order = 3 },
            new() { Id = 30, Name = "Schedule interview", Order = 1 },
            new() { Id = 31, Name = "Perform interview", Order = 2 },
            new() { Id = 4, Name = "Sign Contract", Order = 4 },
            new() { Id = 40, Name = "Upload identification document", Order = 1 },
            new() { Id = 41, Name = "Sign employment contract", Order = 2 },
            new() { Id = 5, Name = "Payment", Order = 5 },
            new() { Id = 50, Name = "Complete payment", Order = 1 },
            new() { Id = 6, Name = "Join Slack", Order = 6 },
            new() { Id = 60, Name = "Join Slack workspace", Order = 1 }
        };
    }

    private static List<StepConfiguration> GetFallbackStepConfigurations()
    {
        return new List<StepConfiguration>
        {
            new() { NodeId = 1, Tasks = new List<TaskConfiguration> { new() { NodeId = 10, PayloadIdentifiers = new List<string> { "first_name", "last_name", "email", "timestamp" } } } },
            new() { NodeId = 2, Tasks = new List<TaskConfiguration>
            {
                new() { NodeId = 20, PassCondition = new Condition { Type = ConditionTypes.ScoreThreshold, Field = "score", Threshold = 75 }, PayloadIdentifiers = new List<string> { "test_id", "score", "timestamp" }, DerivedFactsToStore = new List<DerivedFactMapping> { new() { SourceField = "score", TargetFactName = "iq_score" } } },
                new() { NodeId = 21, VisibilityCondition = new Condition { Type = ConditionTypes.ScoreRange, Field = "iq_score", Min = 60, Max = 75 }, PassCondition = new Condition { Type = ConditionTypes.ScoreThreshold, Field = "score", Threshold = 75 }, PayloadIdentifiers = new List<string> { "score", "timestamp" }, RequiresPreviousTaskFailedId = 20, DerivedFactsToStore = new List<DerivedFactMapping> { new() { SourceField = "score", TargetFactName = "iq_score" } } }
            } },
            new() { NodeId = 3, Tasks = new List<TaskConfiguration>
            {
                new() { NodeId = 30, PayloadIdentifiers = new List<string> { "interview_date" } },
                new() { NodeId = 31, PassCondition = new Condition { Type = ConditionTypes.DecisionEquals, Field = "decision", ExpectedValue = "passed_interview" }, PayloadIdentifiers = new List<string> { "interview_date", "interviewer_id", "decision" } }
            } },
            new() { NodeId = 4, Tasks = new List<TaskConfiguration>
            {
                new() { NodeId = 40, PayloadIdentifiers = new List<string> { "passport_number", "timestamp" } },
                new() { NodeId = 41, PayloadIdentifiers = new List<string> { "timestamp" } }
            } },
            new() { NodeId = 5, Tasks = new List<TaskConfiguration> { new() { NodeId = 50, PayloadIdentifiers = new List<string> { "payment_id", "timestamp" } } } },
            new() { NodeId = 6, Tasks = new List<TaskConfiguration> { new() { NodeId = 60, PayloadIdentifiers = new List<string> { "email", "timestamp" } } } }
        };
    }

    public Task<List<FlowNode>> GetAllNodesAsync()
    {
        return Task.FromResult(_nodesById.Values.ToList());
    }

    public Task<FlowNode?> GetNodeByIdAsync(int nodeId)
    {
        _nodesById.TryGetValue(nodeId, out var node);
        return Task.FromResult(node);
    }

    public Task<FlowNode?> GetNodeByNameAsync(string name)
    {
        _nodesByName.TryGetValue(name.ToLowerInvariant(), out var node);
        return Task.FromResult(node);
    }

    public Task<List<FlowNode>> GetChildNodesAsync(int parentId)
    {
        if (_childrenByParentId.TryGetValue(parentId, out var children))
            return Task.FromResult(children.OrderBy(c => c.Order).ToList());
        return Task.FromResult(new List<FlowNode>());
    }

    public Task<List<FlowNode>> GetRootStepsAsync()
    {
        lock (_rootStepsLock)
        {
            return Task.FromResult(_cachedRootSteps?.ToList() ?? new List<FlowNode>());
        }
    }
}
