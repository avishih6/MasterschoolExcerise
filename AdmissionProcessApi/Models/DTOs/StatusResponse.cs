namespace AdmissionProcessApi.Models.DTOs;

public class StatusResponse
{
    public string Status { get; set; } = string.Empty; // "accepted", "rejected", "in_progress"
}
