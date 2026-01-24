namespace MasterschoolExercise.Models.DTOs;

public class UpdateStepRequest
{
    public string? Name { get; set; }
    public int? Order { get; set; }
    public bool? IsActive { get; set; }
}
