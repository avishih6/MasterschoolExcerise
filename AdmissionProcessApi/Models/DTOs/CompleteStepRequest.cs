namespace AdmissionProcessApi.Models.DTOs;

public class CompleteStepRequest
{
    public string StepName { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public Dictionary<string, object> StepPayload { get; set; } = new();
}
