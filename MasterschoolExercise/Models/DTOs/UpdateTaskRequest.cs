namespace MasterschoolExercise.Models.DTOs;

public class UpdateTaskRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? PassingConditionType { get; set; }
    public string? PassingConditionConfig { get; set; }
    public string? ConditionalVisibilityType { get; set; }
    public string? ConditionalVisibilityConfig { get; set; }
    public bool? IsActive { get; set; }
}
