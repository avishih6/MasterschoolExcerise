using AdmissionProcessDAL.Models;
using AdmissionProcessDAL.Services;
using System.Text.Json;

namespace AdmissionProcessApi.Services;

public class FlowDataSeeder : IFlowDataSeeder
{
    private readonly IStepDataService _stepDataService;
    private readonly ITaskDataService _taskDataService;
    private readonly IStepTaskDataService _stepTaskDataService;
    private bool _isSeeded = false;

    public FlowDataSeeder(
        IStepDataService stepDataService,
        ITaskDataService taskDataService,
        IStepTaskDataService stepTaskDataService)
    {
        _stepDataService = stepDataService;
        _taskDataService = taskDataService;
        _stepTaskDataService = stepTaskDataService;
    }

    public async Task SeedInitialDataAsync()
    {
        if (_isSeeded) return;

        // Create Steps
        var step1 = await _stepDataService.CreateStepAsync(new Step
        {
            Name = "Personal Details Form",
            Order = 1
        });

        var step2 = await _stepDataService.CreateStepAsync(new Step
        {
            Name = "IQ Test",
            Order = 2
        });

        var step3 = await _stepDataService.CreateStepAsync(new Step
        {
            Name = "Interview",
            Order = 3
        });

        var step4 = await _stepDataService.CreateStepAsync(new Step
        {
            Name = "Sign Contract",
            Order = 4
        });

        var step5 = await _stepDataService.CreateStepAsync(new Step
        {
            Name = "Payment",
            Order = 5
        });

        var step6 = await _stepDataService.CreateStepAsync(new Step
        {
            Name = "Join Slack",
            Order = 6
        });

        // Create Tasks
        var task1 = await _taskDataService.CreateTaskAsync(new FlowTask
        {
            Name = "complete_personal_details",
            Description = "Complete personal details form",
            PassingConditionType = "always",
            PassingConditionConfig = "always"
        });

        var task2 = await _taskDataService.CreateTaskAsync(new FlowTask
        {
            Name = "take_iq_test",
            Description = "Take IQ test",
            PassingConditionType = "score_threshold",
            PassingConditionConfig = "score_threshold_75" // Reference to external config
        });

        var task3 = await _taskDataService.CreateTaskAsync(new FlowTask
        {
            Name = "take_second_chance_iq_test",
            Description = "Take second chance IQ test",
            PassingConditionType = "score_threshold",
            PassingConditionConfig = "score_threshold_75", // Reference to external config
            ConditionalVisibilityType = "score_range",
            ConditionalVisibilityConfig = "score_range_60_75" // Reference to external config
        });

        var task4 = await _taskDataService.CreateTaskAsync(new FlowTask
        {
            Name = "schedule_interview",
            Description = "Schedule interview",
            PassingConditionType = "always",
            PassingConditionConfig = "always"
        });

        var task5 = await _taskDataService.CreateTaskAsync(new FlowTask
        {
            Name = "perform_interview",
            Description = "Perform interview",
            PassingConditionType = "decision_match",
            PassingConditionConfig = "decision_passed_interview" // Reference to external config
        });

        var task6 = await _taskDataService.CreateTaskAsync(new FlowTask
        {
            Name = "upload_identification_document",
            Description = "Upload identification document",
            PassingConditionType = "always",
            PassingConditionConfig = "always"
        });

        var task7 = await _taskDataService.CreateTaskAsync(new FlowTask
        {
            Name = "sign_contract",
            Description = "Sign the contract",
            PassingConditionType = "always",
            PassingConditionConfig = "always"
        });

        var task8 = await _taskDataService.CreateTaskAsync(new FlowTask
        {
            Name = "complete_payment",
            Description = "Complete payment",
            PassingConditionType = "always",
            PassingConditionConfig = "always"
        });

        var task9 = await _taskDataService.CreateTaskAsync(new FlowTask
        {
            Name = "join_slack",
            Description = "Join Slack workspace",
            PassingConditionType = "always",
            PassingConditionConfig = "always"
        });

        // Assign Tasks to Steps
        await _stepTaskDataService.AssignTaskToStepAsync(step1.Id, task1.Id, 1);
        await _stepTaskDataService.AssignTaskToStepAsync(step2.Id, task2.Id, 1);
        await _stepTaskDataService.AssignTaskToStepAsync(step2.Id, task3.Id, 2);
        await _stepTaskDataService.AssignTaskToStepAsync(step3.Id, task4.Id, 1);
        await _stepTaskDataService.AssignTaskToStepAsync(step3.Id, task5.Id, 2);
        await _stepTaskDataService.AssignTaskToStepAsync(step4.Id, task6.Id, 1);
        await _stepTaskDataService.AssignTaskToStepAsync(step4.Id, task7.Id, 2);
        await _stepTaskDataService.AssignTaskToStepAsync(step5.Id, task8.Id, 1);
        await _stepTaskDataService.AssignTaskToStepAsync(step6.Id, task9.Id, 1);

        _isSeeded = true;
    }
}
