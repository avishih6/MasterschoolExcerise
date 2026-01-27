using AdmissionProcessModels.Enums;

namespace AdmissionProcessDAL.Models;

public class NodeStatus
{
    public ProgressStatus Status { get; set; }
    public DateTime UpdatedAt { get; set; }
}
