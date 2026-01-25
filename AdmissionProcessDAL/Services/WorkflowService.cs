using AdmissionProcessDAL.Models;

namespace AdmissionProcessDAL.Services;

public class WorkflowService : IWorkflowService
{
    private readonly IWorkflowDataService _workflowDataService;

    public WorkflowService(IWorkflowDataService workflowDataService)
    {
        _workflowDataService = workflowDataService;
    }

    public async Task<WorkflowDefinition?> GetEffectiveDefinitionAsync(int? countryId, int? universityId)
    {
        return await _workflowDataService.GetEffectiveDefinitionAsync(countryId, universityId);
    }

    public async Task<WorkflowTreeDto> BuildWorkflowWithProgressAsync(int userId, int? countryId, int? universityId)
    {
        // Get effective definition
        var definition = await GetEffectiveDefinitionAsync(countryId, universityId);
        if (definition == null)
        {
            throw new InvalidOperationException("No workflow definition found for the given scope");
        }

        // Load all nodes for this definition
        var allNodes = await _workflowDataService.GetNodesByWorkflowDefinitionIdAsync(definition.Id);
        
        // Get user progress map
        var progressMap = await _workflowDataService.GetUserProgressMapAsync(userId, definition.Id);
        
        // Get all phases for name lookup
        var phases = await _workflowDataService.GetAllPhasesAsync();
        var phaseLookup = phases.ToDictionary(p => p.Id, p => p.Name);

        // Build tree structure
        var rootSteps = allNodes
            .Where(n => n.Role == NodeRole.Step && n.ParentNodeId == null)
            .OrderBy(n => n.Order)
            .ToList();

        var steps = new List<StepDto>();

        foreach (var stepNode in rootSteps)
        {
            var stepDto = new StepDto
            {
                NodeId = stepNode.Id,
                PhaseId = stepNode.PhaseId,
                Name = stepNode.Name, // Use node name, not phase name
                Order = stepNode.Order,
                Status = progressMap.GetValueOrDefault(stepNode.Id, ProgressStatus.NotStarted),
                Tasks = new List<TaskDto>()
            };

            // Get child tasks
            var taskNodes = allNodes
                .Where(n => n.Role == NodeRole.Task && n.ParentNodeId == stepNode.Id)
                .OrderBy(n => n.Order)
                .ToList();

            foreach (var taskNode in taskNodes)
            {
                var taskDto = new TaskDto
                {
                    NodeId = taskNode.Id,
                    PhaseId = taskNode.PhaseId,
                    Name = taskNode.Name, // Use node name, not phase name
                    Order = taskNode.Order,
                    Status = progressMap.GetValueOrDefault(taskNode.Id, ProgressStatus.NotStarted)
                };
                stepDto.Tasks.Add(taskDto);
            }

            steps.Add(stepDto);
        }

        return new WorkflowTreeDto
        {
            Definition = new WorkflowDefinitionDto
            {
                Id = definition.Id,
                ScopeLevel = definition.ScopeLevel,
                ScopeEntityId = definition.ScopeEntityId,
                Name = definition.Name
            },
            Steps = steps
        };
    }
}
