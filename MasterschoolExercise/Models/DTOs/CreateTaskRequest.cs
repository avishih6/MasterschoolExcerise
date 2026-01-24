namespace MasterschoolExercise.Models.DTOs;

public class CreateTaskRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string PassingConditionType { get; set; } = "always";
    public string PassingConditionConfig { get; set; } = "{}";
    public string ConditionalVisibilityType { get; set; } = string.Empty;
    public string ConditionalVisibilityConfig { get; set; } = string.Empty;
}
