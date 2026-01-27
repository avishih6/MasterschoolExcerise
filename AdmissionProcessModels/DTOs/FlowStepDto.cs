namespace AdmissionProcessModels.DTOs;

public class FlowStepDto
{
    public string Name { get; set; } = string.Empty;
    public int Order { get; set; }
    public List<FlowTaskDto> Tasks { get; set; } = new();
}
