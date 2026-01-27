namespace AdmissionProcessModels.DTOs;

public class FlowResponse
{
    public List<FlowStepDto> Steps { get; set; } = new();
    public int TotalSteps { get; set; }
    public int CurrentStepNumber { get; set; }
    public string? CurrentStepName { get; set; }
    public string? CurrentTaskName { get; set; }
}
