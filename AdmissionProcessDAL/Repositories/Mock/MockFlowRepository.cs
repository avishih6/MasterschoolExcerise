using System.Text.Json;
using AdmissionProcessDAL.Models;
using AdmissionProcessDAL.Repositories.Interfaces;
using Microsoft.Extensions.Logging;

namespace AdmissionProcessDAL.Repositories.Mock;

public class MockFlowRepository : IFlowRepository
{
    private readonly Dictionary<int, FlowNode> _nodes = new();
    private readonly Dictionary<int, List<FlowNode>> _childrenByParent = new();
    private readonly ILogger<MockFlowRepository> _logger;

    public MockFlowRepository(ILogger<MockFlowRepository> logger)
    {
        _logger = logger;
        LoadFlowFromConfigurationAsync().GetAwaiter().GetResult();
    }

    private async Task LoadFlowFromConfigurationAsync()
    {
        try
        {
            var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Configuration", "flow-config.json");
            
            if (!File.Exists(configPath))
            {
                _logger.LogWarning("Flow configuration file not found at {Path}, using fallback configuration", configPath);
                SeedFallbackFlow();
                return;
            }

            var jsonContent = await File.ReadAllTextAsync(configPath).ConfigureAwait(false);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            
            var flowConfig = JsonSerializer.Deserialize<FlowConfiguration>(jsonContent, options);
            
            if (flowConfig?.Steps == null || flowConfig.Steps.Count == 0)
            {
                _logger.LogWarning("Flow configuration is empty, using fallback configuration");
                SeedFallbackFlow();
                return;
            }

            foreach (var stepConfig in flowConfig.Steps)
            {
                var step = new FlowNode
                {
                    Id = stepConfig.Id,
                    Name = stepConfig.Name,
                    Role = NodeRole.Step,
                    ParentId = null,
                    Order = stepConfig.Order
                };
                _nodes[step.Id] = step;

                if (stepConfig.Tasks != null)
                {
                    foreach (var taskConfig in stepConfig.Tasks)
                    {
                        var task = new FlowNode
                        {
                            Id = taskConfig.Id,
                            Name = taskConfig.Name,
                            Role = NodeRole.Task,
                            ParentId = stepConfig.Id,
                            Order = taskConfig.Order,
                            VisibilityCondition = taskConfig.VisibilityCondition,
                            PassCondition = taskConfig.PassCondition,
                            PayloadIdentifiers = taskConfig.PayloadIdentifiers ?? new List<string>(),
                            RequiresPreviousTaskFailedId = taskConfig.RequiresPreviousTaskFailedId,
                            DerivedFactsToStore = taskConfig.DerivedFactsToStore
                        };
                        _nodes[task.Id] = task;
                        AddChild(stepConfig.Id, task);
                    }
                }
            }

            _logger.LogInformation("Successfully loaded {StepCount} steps from flow configuration", flowConfig.Steps.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load flow configuration, using fallback");
            SeedFallbackFlow();
        }
    }

    private void SeedFallbackFlow()
    {
        var step1 = new FlowNode { Id = 1, Name = "Personal Details Form", Role = NodeRole.Step, ParentId = null, Order = 1 };
        _nodes[1] = step1;
        var task1 = new FlowNode { Id = 10, Name = "Complete personal details", Role = NodeRole.Task, ParentId = 1, Order = 1, PayloadIdentifiers = new List<string> { "first_name", "last_name", "email" } };
        _nodes[10] = task1;
        AddChild(1, task1);

        var step2 = new FlowNode { Id = 2, Name = "IQ Test", Role = NodeRole.Step, ParentId = null, Order = 2 };
        _nodes[2] = step2;
        var task2 = new FlowNode 
        { 
            Id = 20, 
            Name = "Take IQ test", 
            Role = NodeRole.Task, 
            ParentId = 2, 
            Order = 1, 
            PassCondition = new PassCondition { Type = "score_threshold", Field = "score", Threshold = 75 },
            PayloadIdentifiers = new List<string> { "score", "test_id" },
            DerivedFactsToStore = new List<DerivedFactMapping> { new() { SourceField = "score", TargetFactName = "iq_score" } }
        };
        _nodes[20] = task2;
        AddChild(2, task2);
        
        var task2b = new FlowNode 
        { 
            Id = 21, 
            Name = "Take second chance IQ test", 
            Role = NodeRole.Task, 
            ParentId = 2, 
            Order = 2, 
            VisibilityCondition = new VisibilityCondition { Type = "score_range", Field = "iq_score", Min = 60, Max = 75 },
            PassCondition = new PassCondition { Type = "score_threshold", Field = "score", Threshold = 75 },
            PayloadIdentifiers = new List<string> { "score" },
            RequiresPreviousTaskFailedId = 20,
            DerivedFactsToStore = new List<DerivedFactMapping> { new() { SourceField = "score", TargetFactName = "iq_score" } }
        };
        _nodes[21] = task2b;
        AddChild(2, task2b);

        var step3 = new FlowNode { Id = 3, Name = "Interview", Role = NodeRole.Step, ParentId = null, Order = 3 };
        _nodes[3] = step3;
        var task3a = new FlowNode { Id = 30, Name = "Schedule interview", Role = NodeRole.Task, ParentId = 3, Order = 1, PayloadIdentifiers = new List<string> { "interview_date" } };
        _nodes[30] = task3a;
        AddChild(3, task3a);
        var task3b = new FlowNode 
        { 
            Id = 31, 
            Name = "Perform interview", 
            Role = NodeRole.Task, 
            ParentId = 3, 
            Order = 2, 
            PassCondition = new PassCondition { Type = "decision_equals", Field = "decision", ExpectedValue = "passed_interview" },
            PayloadIdentifiers = new List<string> { "decision", "interviewer_id" }
        };
        _nodes[31] = task3b;
        AddChild(3, task3b);

        var step4 = new FlowNode { Id = 4, Name = "Sign Contract", Role = NodeRole.Step, ParentId = null, Order = 4 };
        _nodes[4] = step4;
        var task4a = new FlowNode { Id = 40, Name = "Upload identification document", Role = NodeRole.Task, ParentId = 4, Order = 1, PayloadIdentifiers = new List<string> { "passport_number" } };
        _nodes[40] = task4a;
        AddChild(4, task4a);
        var task4b = new FlowNode { Id = 41, Name = "Sign contract", Role = NodeRole.Task, ParentId = 4, Order = 2, PayloadIdentifiers = new List<string> { "signature", "contract_signed" } };
        _nodes[41] = task4b;
        AddChild(4, task4b);

        var step5 = new FlowNode { Id = 5, Name = "Payment", Role = NodeRole.Step, ParentId = null, Order = 5 };
        _nodes[5] = step5;
        var task5 = new FlowNode { Id = 50, Name = "Complete payment", Role = NodeRole.Task, ParentId = 5, Order = 1, PayloadIdentifiers = new List<string> { "payment_id" } };
        _nodes[50] = task5;
        AddChild(5, task5);

        var step6 = new FlowNode { Id = 6, Name = "Join Slack", Role = NodeRole.Step, ParentId = null, Order = 6 };
        _nodes[6] = step6;
        var task6 = new FlowNode { Id = 60, Name = "Join Slack workspace", Role = NodeRole.Task, ParentId = 6, Order = 1, PayloadIdentifiers = new List<string> { "slack_email", "slack_joined" } };
        _nodes[60] = task6;
        AddChild(6, task6);
    }

    private void AddChild(int parentId, FlowNode child)
    {
        if (!_childrenByParent.ContainsKey(parentId))
            _childrenByParent[parentId] = new List<FlowNode>();
        _childrenByParent[parentId].Add(child);
    }

    public Task<List<FlowNode>> GetAllNodesAsync()
    {
        return Task.FromResult(_nodes.Values.ToList());
    }

    public Task<FlowNode?> GetNodeByIdAsync(int nodeId)
    {
        _nodes.TryGetValue(nodeId, out var node);
        return Task.FromResult(node);
    }

    public Task<List<FlowNode>> GetChildNodesAsync(int parentId)
    {
        if (_childrenByParent.TryGetValue(parentId, out var children))
            return Task.FromResult(children.OrderBy(c => c.Order).ToList());
        return Task.FromResult(new List<FlowNode>());
    }

    public Task<List<FlowNode>> GetRootStepsAsync()
    {
        return Task.FromResult(_nodes.Values
            .Where(n => n.Role == NodeRole.Step && n.ParentId == null)
            .OrderBy(n => n.Order)
            .ToList());
    }
}

internal class FlowConfiguration
{
    public List<StepConfiguration> Steps { get; set; } = new();
}

internal class StepConfiguration
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Order { get; set; }
    public List<TaskConfiguration>? Tasks { get; set; }
}

internal class TaskConfiguration
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Order { get; set; }
    public VisibilityCondition? VisibilityCondition { get; set; }
    public PassCondition? PassCondition { get; set; }
    public List<string>? PayloadIdentifiers { get; set; }
    public int? RequiresPreviousTaskFailedId { get; set; }
    public List<DerivedFactMapping>? DerivedFactsToStore { get; set; }
}
