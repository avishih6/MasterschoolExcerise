namespace MasterschoolExercise.Models;

public class Step
{
    public string Name { get; set; } = string.Empty;
    public int Order { get; set; }
    public List<Task> Tasks { get; set; } = new();
}

public class Task
{
    public string Name { get; set; } = string.Empty;
    public string StepName { get; set; } = string.Empty;
    public Func<Dictionary<string, object>, bool>? PassingCondition { get; set; }
    public Func<string, Dictionary<string, object>, bool>? ConditionalVisibility { get; set; }
}
