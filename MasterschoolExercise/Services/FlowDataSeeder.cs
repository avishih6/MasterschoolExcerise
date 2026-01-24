using MasterschoolExercise.Models;
using MasterschoolExercise.Repositories;
using System.Text.Json;

namespace MasterschoolExercise.Services;

public class FlowDataSeeder : IFlowDataSeeder
{
    private readonly IStepRepository _stepRepository;
    private readonly IFlowTaskRepository _taskRepository;
    private readonly IStepTaskRepository _stepTaskRepository;
    private bool _isSeeded = false;

    public FlowDataSeeder(
        IStepRepository stepRepository,
        IFlowTaskRepository taskRepository,
        IStepTaskRepository stepTaskRepository)
    {
        _stepRepository = stepRepository;
        _taskRepository = taskRepository;
        _stepTaskRepository = stepTaskRepository;
    }

    public async Task SeedInitialDataAsync()
    {
        if (_isSeeded) return;

        // Create Steps
        var step1 = await _stepRepository.CreateStepAsync(new Step
        {
            Name = "Personal Details Form",
            Order = 1
        });

        var step2 = await _stepRepository.CreateStepAsync(new Step
        {
            Name = "IQ Test",
            Order = 2
        });

        var step3 = await _stepRepository.CreateStepAsync(new Step
        {
            Name = "Interview",
            Order = 3
        });

        var step4 = await _stepRepository.CreateStepAsync(new Step
        {
            Name = "Sign Contract",
            Order = 4
        });

        var step5 = await _stepRepository.CreateStepAsync(new Step
        {
            Name = "Payment",
            Order = 5
        });

        var step6 = await _stepRepository.CreateStepAsync(new Step
        {
            Name = "Join Slack",
            Order = 6
        });

        // Create Tasks
        var task1 = await _taskRepository.CreateTaskAsync(new FlowTask
        {
            Name = "complete_personal_details",
            Description = "Complete personal details form",
            PassingConditionType = "always",
            PassingConditionConfig = "{}"
        });

        var task2 = await _taskRepository.CreateTaskAsync(new FlowTask
        {
            Name = "take_iq_test",
            Description = "Take IQ test",
            PassingConditionType = "score_threshold",
            PassingConditionConfig = JsonSerializer.Serialize(new { threshold = 75 })
        });

        var task3 = await _taskRepository.CreateTaskAsync(new FlowTask
        {
            Name = "take_second_chance_iq_test",
            Description = "Take second chance IQ test",
            PassingConditionType = "score_threshold",
            PassingConditionConfig = JsonSerializer.Serialize(new { threshold = 75 }),
            ConditionalVisibilityType = "score_range",
            ConditionalVisibilityConfig = JsonSerializer.Serialize(new { min = 60, max = 75 })
        });

        var task4 = await _taskRepository.CreateTaskAsync(new FlowTask
        {
            Name = "schedule_interview",
            Description = "Schedule interview",
            PassingConditionType = "always",
            PassingConditionConfig = "{}"
        });

        var task5 = await _taskRepository.CreateTaskAsync(new FlowTask
        {
            Name = "perform_interview",
            Description = "Perform interview",
            PassingConditionType = "decision_match",
            PassingConditionConfig = JsonSerializer.Serialize(new { expectedValue = "passed_interview" })
        });

        var task6 = await _taskRepository.CreateTaskAsync(new FlowTask
        {
            Name = "upload_identification_document",
            Description = "Upload identification document",
            PassingConditionType = "always",
            PassingConditionConfig = "{}"
        });

        var task7 = await _taskRepository.CreateTaskAsync(new FlowTask
        {
            Name = "sign_contract",
            Description = "Sign the contract",
            PassingConditionType = "always",
            PassingConditionConfig = "{}"
        });

        var task8 = await _taskRepository.CreateTaskAsync(new FlowTask
        {
            Name = "complete_payment",
            Description = "Complete payment",
            PassingConditionType = "always",
            PassingConditionConfig = "{}"
        });

        var task9 = await _taskRepository.CreateTaskAsync(new FlowTask
        {
            Name = "join_slack",
            Description = "Join Slack workspace",
            PassingConditionType = "always",
            PassingConditionConfig = "{}"
        });

        // Assign Tasks to Steps
        await _stepTaskRepository.AssignTaskToStepAsync(step1.Id, task1.Id, 1);
        await _stepTaskRepository.AssignTaskToStepAsync(step2.Id, task2.Id, 1);
        await _stepTaskRepository.AssignTaskToStepAsync(step2.Id, task3.Id, 2);
        await _stepTaskRepository.AssignTaskToStepAsync(step3.Id, task4.Id, 1);
        await _stepTaskRepository.AssignTaskToStepAsync(step3.Id, task5.Id, 2);
        await _stepTaskRepository.AssignTaskToStepAsync(step4.Id, task6.Id, 1);
        await _stepTaskRepository.AssignTaskToStepAsync(step4.Id, task7.Id, 2);
        await _stepTaskRepository.AssignTaskToStepAsync(step5.Id, task8.Id, 1);
        await _stepTaskRepository.AssignTaskToStepAsync(step6.Id, task9.Id, 1);

        _isSeeded = true;
    }
}
