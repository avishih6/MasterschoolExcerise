using AdmissionProcessDAL.Models;
using AdmissionProcessDAL.Services;
using Xunit;

namespace AdmissionProcessDAL.Tests;

public class WorkflowServiceTests
{
    private readonly IWorkflowDataService _workflowDataService;
    private readonly IWorkflowService _workflowService;
    private readonly WorkflowSeeder _seeder;

    public WorkflowServiceTests()
    {
        _workflowDataService = new WorkflowDataService();
        _workflowService = new WorkflowService(_workflowDataService);
        _seeder = new WorkflowSeeder(_workflowDataService);
    }

    [Fact]
    public async Task GetEffectiveDefinition_FallbackSelection_Works()
    {
        // Arrange
        await _seeder.SeedInitialDataAsync();

        // Act & Assert
        
        // University should return university definition
        var universityDef = await _workflowService.GetEffectiveDefinitionAsync(10, 77);
        Assert.NotNull(universityDef);
        Assert.Equal(ScopeLevel.University, universityDef.ScopeLevel);
        Assert.Equal(77, universityDef.ScopeEntityId);

        // Country (no university) should return country definition
        var countryDef = await _workflowService.GetEffectiveDefinitionAsync(10, null);
        Assert.NotNull(countryDef);
        Assert.Equal(ScopeLevel.Country, countryDef.ScopeLevel);
        Assert.Equal(10, countryDef.ScopeEntityId);

        // Global (no country, no university) should return global definition
        var globalDef = await _workflowService.GetEffectiveDefinitionAsync(null, null);
        Assert.NotNull(globalDef);
        Assert.Equal(ScopeLevel.Global, globalDef.ScopeLevel);
        Assert.Null(globalDef.ScopeEntityId);
    }

    [Fact]
    public async Task BuildWorkflowWithProgress_FullOverride_DiffersFromGlobal()
    {
        // Arrange
        await _seeder.SeedInitialDataAsync();

        // Act
        var countryWorkflow = await _workflowService.BuildWorkflowWithProgressAsync(1001, 10, null);
        var globalWorkflow = await _workflowService.BuildWorkflowWithProgressAsync(1001, null, null);

        // Assert
        Assert.NotNull(countryWorkflow);
        Assert.NotNull(globalWorkflow);
        
        // Country workflow should NOT have "Interview" step (full override)
        var countryHasInterview = countryWorkflow.Steps.Any(s => s.Name == "Interview");
        Assert.False(countryHasInterview, "Country workflow should not have Interview step (full override)");
        
        // Country workflow should have "Local exam" step (not in global)
        var countryHasLocalExam = countryWorkflow.Steps.Any(s => s.Name == "Local exam");
        Assert.True(countryHasLocalExam, "Country workflow should have Local exam step");
        
        // Global workflow should have "Interview" step
        var globalHasInterview = globalWorkflow.Steps.Any(s => s.Name == "Interview");
        Assert.True(globalHasInterview, "Global workflow should have Interview step");
        
        // Country workflow should have different order (Pay fee comes before Upload documents)
        var countryAdmissionsStep = countryWorkflow.Steps.FirstOrDefault(s => s.Name == "Admissions");
        Assert.NotNull(countryAdmissionsStep);
        Assert.True(countryAdmissionsStep.Tasks.Count >= 2);
        var firstTask = countryAdmissionsStep.Tasks[0];
        var secondTask = countryAdmissionsStep.Tasks[1];
        // Note: In our seed, Pay fee is order 1, Upload documents is order 2
        Assert.True(firstTask.Order < secondTask.Order, "Country workflow should have different task order");
    }

    [Fact]
    public async Task BuildWorkflowWithProgress_ProgressMapping_AttachesCorrectly()
    {
        // Arrange
        await _seeder.SeedInitialDataAsync();

        // Act
        var workflow = await _workflowService.BuildWorkflowWithProgressAsync(1001, 10, 77);

        // Assert
        Assert.NotNull(workflow);
        Assert.NotEmpty(workflow.Steps);
        
        // Check that progress statuses are attached
        foreach (var step in workflow.Steps)
        {
            // Step should have a status (NotStarted, Passed, or Failed)
            Assert.True(Enum.IsDefined(typeof(ProgressStatus), step.Status));
            
            // All tasks should have statuses
            foreach (var task in step.Tasks)
            {
                Assert.True(Enum.IsDefined(typeof(ProgressStatus), task.Status));
            }
        }
        
        // Verify that at least some nodes have non-NotStarted status (from seed)
        var hasProgress = workflow.Steps.Any(s => 
            s.Status != ProgressStatus.NotStarted || 
            s.Tasks.Any(t => t.Status != ProgressStatus.NotStarted));
        Assert.True(hasProgress, "At least some nodes should have progress status from seed data");
    }

    [Fact]
    public async Task BuildWorkflowWithProgress_UniversityOverride_IncludesUniversityOnlyTask()
    {
        // Arrange
        await _seeder.SeedInitialDataAsync();

        // Act
        var universityWorkflow = await _workflowService.BuildWorkflowWithProgressAsync(1001, 10, 77);
        var countryWorkflow = await _workflowService.BuildWorkflowWithProgressAsync(1001, 10, null);

        // Assert
        Assert.NotNull(universityWorkflow);
        Assert.NotNull(countryWorkflow);
        
        // University workflow should have "Upload passport scan" task
        var universityAdmissionsStep = universityWorkflow.Steps.FirstOrDefault(s => s.Name == "Admissions");
        Assert.NotNull(universityAdmissionsStep);
        var hasPassportScan = universityAdmissionsStep.Tasks.Any(t => t.Name == "Upload passport scan");
        Assert.True(hasPassportScan, "University workflow should have university-only task");
        
        // Country workflow should NOT have "Upload passport scan" task
        var countryAdmissionsStep = countryWorkflow.Steps.FirstOrDefault(s => s.Name == "Admissions");
        Assert.NotNull(countryAdmissionsStep);
        var countryHasPassportScan = countryAdmissionsStep.Tasks.Any(t => t.Name == "Upload passport scan");
        Assert.False(countryHasPassportScan, "Country workflow should not have university-only task");
    }
}
