namespace AdmissionProcessApi.Models.DTOs;

public class UserProgressResponse
{
    public string? CurrentStep { get; set; }
    public string? CurrentTask { get; set; }
    public int CurrentStepNumber { get; set; }
    public int TotalSteps { get; set; }
}
