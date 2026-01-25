using AdmissionProcessDAL.Models;

namespace AdmissionProcessDAL.Services;

public class WorkflowSeeder
{
    private readonly IWorkflowDataService _workflowDataService;
    private bool _isSeeded = false;

    public WorkflowSeeder(IWorkflowDataService workflowDataService)
    {
        _workflowDataService = workflowDataService;
    }

    public async Task SeedInitialDataAsync()
    {
        if (_isSeeded) return;

        // Create Phases
        var admissionsPhase = await _workflowDataService.CreatePhaseAsync(new Phase
        {
            Name = "Admissions",
            DefaultEnableCondition = null,
            DefaultVisibilityCondition = null
        });

        var interviewPhase = await _workflowDataService.CreatePhaseAsync(new Phase
        {
            Name = "Interview",
            DefaultEnableCondition = null,
            DefaultVisibilityCondition = null
        });

        var localExamPhase = await _workflowDataService.CreatePhaseAsync(new Phase
        {
            Name = "Local exam",
            DefaultEnableCondition = null,
            DefaultVisibilityCondition = null
        });

        // ===== GLOBAL WORKFLOW DEFINITION =====
        var globalDef = await _workflowDataService.CreateWorkflowDefinitionAsync(new WorkflowDefinition
        {
            ScopeLevel = ScopeLevel.Global,
            ScopeEntityId = null,
            Name = "Global Admissions Workflow",
            DerivedFromDefinitionId = null
        });

        // Global: Step "Admissions" (root)
        var globalAdmissionsStep = await _workflowDataService.CreateWorkflowNodeAsync(new WorkflowNode
        {
            WorkflowDefinitionId = globalDef.Id,
            PhaseId = admissionsPhase.Id,
            Role = NodeRole.Step,
            ParentNodeId = null, // root
            Order = 1,
            Name = "Admissions",
            EnableConditionOverride = null,
            VisibilityConditionOverride = null
        });

        // Global: Task "Upload documents" (under Admissions)
        await _workflowDataService.CreateWorkflowNodeAsync(new WorkflowNode
        {
            WorkflowDefinitionId = globalDef.Id,
            PhaseId = admissionsPhase.Id,
            Role = NodeRole.Task,
            ParentNodeId = globalAdmissionsStep.Id,
            Order = 1,
            Name = "Upload documents",
            EnableConditionOverride = null,
            VisibilityConditionOverride = null
        });

        // Global: Task "Pay application fee" (under Admissions)
        await _workflowDataService.CreateWorkflowNodeAsync(new WorkflowNode
        {
            WorkflowDefinitionId = globalDef.Id,
            PhaseId = admissionsPhase.Id,
            Role = NodeRole.Task,
            ParentNodeId = globalAdmissionsStep.Id,
            Order = 2,
            Name = "Pay application fee",
            EnableConditionOverride = null,
            VisibilityConditionOverride = null
        });

        // Global: Step "Interview" (root)
        var globalInterviewStep = await _workflowDataService.CreateWorkflowNodeAsync(new WorkflowNode
        {
            WorkflowDefinitionId = globalDef.Id,
            PhaseId = interviewPhase.Id,
            Role = NodeRole.Step,
            ParentNodeId = null, // root
            Order = 2,
            Name = "Interview",
            EnableConditionOverride = null,
            VisibilityConditionOverride = null
        });

        // Global: Task "Book slot" (under Interview)
        await _workflowDataService.CreateWorkflowNodeAsync(new WorkflowNode
        {
            WorkflowDefinitionId = globalDef.Id,
            PhaseId = interviewPhase.Id,
            Role = NodeRole.Task,
            ParentNodeId = globalInterviewStep.Id,
            Order = 1,
            Name = "Book slot",
            EnableConditionOverride = null,
            VisibilityConditionOverride = null
        });

        // ===== COUNTRY WORKFLOW DEFINITION (CountryId=10) - FULL OVERRIDE =====
        var countryDef = await _workflowDataService.CreateWorkflowDefinitionAsync(new WorkflowDefinition
        {
            ScopeLevel = ScopeLevel.Country,
            ScopeEntityId = 10,
            Name = "Country 10 Admissions Workflow",
            DerivedFromDefinitionId = globalDef.Id
        });

        // Country: Step "Admissions" (root) - different order
        var countryAdmissionsStep = await _workflowDataService.CreateWorkflowNodeAsync(new WorkflowNode
        {
            WorkflowDefinitionId = countryDef.Id,
            PhaseId = admissionsPhase.Id,
            Role = NodeRole.Step,
            ParentNodeId = null,
            Order = 1,
            Name = "Admissions",
            EnableConditionOverride = null,
            VisibilityConditionOverride = null
        });

        // Country: Task "Pay application fee" (under Admissions) - different order (comes first)
        await _workflowDataService.CreateWorkflowNodeAsync(new WorkflowNode
        {
            WorkflowDefinitionId = countryDef.Id,
            PhaseId = admissionsPhase.Id,
            Role = NodeRole.Task,
            ParentNodeId = countryAdmissionsStep.Id,
            Order = 1,
            Name = "Pay application fee",
            EnableConditionOverride = null,
            VisibilityConditionOverride = null
        });

        // Country: Task "Upload documents" (under Admissions) - different order (comes second)
        await _workflowDataService.CreateWorkflowNodeAsync(new WorkflowNode
        {
            WorkflowDefinitionId = countryDef.Id,
            PhaseId = admissionsPhase.Id,
            Role = NodeRole.Task,
            ParentNodeId = countryAdmissionsStep.Id,
            Order = 2,
            Name = "Upload documents",
            EnableConditionOverride = null,
            VisibilityConditionOverride = null
        });

        // Country: Step "Local exam" (root) - NEW step not in global
        var countryLocalExamStep = await _workflowDataService.CreateWorkflowNodeAsync(new WorkflowNode
        {
            WorkflowDefinitionId = countryDef.Id,
            PhaseId = localExamPhase.Id,
            Role = NodeRole.Step,
            ParentNodeId = null,
            Order = 2,
            Name = "Local exam",
            EnableConditionOverride = null,
            VisibilityConditionOverride = null
        });

        // Country: Task "Schedule exam" (under Local exam)
        await _workflowDataService.CreateWorkflowNodeAsync(new WorkflowNode
        {
            WorkflowDefinitionId = countryDef.Id,
            PhaseId = localExamPhase.Id,
            Role = NodeRole.Task,
            ParentNodeId = countryLocalExamStep.Id,
            Order = 1,
            Name = "Schedule exam",
            EnableConditionOverride = null,
            VisibilityConditionOverride = null
        });

        // Note: Country definition does NOT include "Interview" step (full override)

        // ===== UNIVERSITY WORKFLOW DEFINITION (UniversityId=77) - FULL OVERRIDE =====
        var universityDef = await _workflowDataService.CreateWorkflowDefinitionAsync(new WorkflowDefinition
        {
            ScopeLevel = ScopeLevel.University,
            ScopeEntityId = 77,
            Name = "University 77 Admissions Workflow",
            DerivedFromDefinitionId = countryDef.Id
        });

        // University: Step "Admissions" (root)
        var universityAdmissionsStep = await _workflowDataService.CreateWorkflowNodeAsync(new WorkflowNode
        {
            WorkflowDefinitionId = universityDef.Id,
            PhaseId = admissionsPhase.Id,
            Role = NodeRole.Step,
            ParentNodeId = null,
            Order = 1,
            Name = "Admissions",
            EnableConditionOverride = null,
            VisibilityConditionOverride = null
        });

        // University: Task "Upload documents" (under Admissions)
        await _workflowDataService.CreateWorkflowNodeAsync(new WorkflowNode
        {
            WorkflowDefinitionId = universityDef.Id,
            PhaseId = admissionsPhase.Id,
            Role = NodeRole.Task,
            ParentNodeId = universityAdmissionsStep.Id,
            Order = 1,
            Name = "Upload documents",
            EnableConditionOverride = null,
            VisibilityConditionOverride = null
        });

        // University: Task "Pay application fee" (under Admissions)
        await _workflowDataService.CreateWorkflowNodeAsync(new WorkflowNode
        {
            WorkflowDefinitionId = universityDef.Id,
            PhaseId = admissionsPhase.Id,
            Role = NodeRole.Task,
            ParentNodeId = universityAdmissionsStep.Id,
            Order = 2,
            Name = "Pay application fee",
            EnableConditionOverride = null,
            VisibilityConditionOverride = null
        });

        // University: Task "Upload passport scan" (under Admissions) - NEW university-only task
        await _workflowDataService.CreateWorkflowNodeAsync(new WorkflowNode
        {
            WorkflowDefinitionId = universityDef.Id,
            PhaseId = admissionsPhase.Id,
            Role = NodeRole.Task,
            ParentNodeId = universityAdmissionsStep.Id,
            Order = 3,
            Name = "Upload passport scan",
            EnableConditionOverride = null,
            VisibilityConditionOverride = null
        });

        // ===== USER PROGRESS SEED =====
        // UserId=1001, for effective definition (countryId=10, universityId=77) -> should use universityDef
        var effectiveDef = await _workflowDataService.GetEffectiveDefinitionAsync(10, 77);
        if (effectiveDef != null)
        {
            var nodes = await _workflowDataService.GetNodesByWorkflowDefinitionIdAsync(effectiveDef.Id);
            
            // Mark some nodes as Passed/Failed/NotStarted
            int userId = 1001;
            int statusIndex = 0;
            foreach (var node in nodes.OrderBy(n => n.Order))
            {
                ProgressStatus status = (statusIndex % 3) switch
                {
                    0 => ProgressStatus.Passed,
                    1 => ProgressStatus.Failed,
                    _ => ProgressStatus.NotStarted
                };
                
                await _workflowDataService.CreateOrUpdateUserNodeStatusAsync(new UserNodeStatus
                {
                    UserId = userId,
                    WorkflowDefinitionId = effectiveDef.Id,
                    NodeId = node.Id,
                    Status = status,
                    UpdatedAtUtc = DateTime.UtcNow
                });
                
                statusIndex++;
            }
        }

        _isSeeded = true;
    }
}
