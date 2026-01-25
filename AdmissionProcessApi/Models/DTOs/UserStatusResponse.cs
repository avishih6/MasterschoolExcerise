namespace AdmissionProcessApi.Models.DTOs;

public class UserStatusResponse
{
    public string Status { get; set; } = string.Empty; // "accepted", "rejected", "in_progress"
}
