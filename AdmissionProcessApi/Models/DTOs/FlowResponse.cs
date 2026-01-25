namespace AdmissionProcessApi.Models.DTOs;

public class FlowResponse
{
    public List<FlowStepDto> Steps { get; set; } = new();
}

public class FlowStepDto
{
    public string Name { get; set; } = string.Empty;
    public int Order { get; set; }
    public List<FlowTaskDto> Tasks { get; set; } = new();
}

public class FlowTaskDto
{
    public string Name { get; set; } = string.Empty;
    public string StepName { get; set; } = string.Empty;
}
