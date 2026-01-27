namespace AdmissionProcessModels.DTOs;

public class ErrorResponse
{
    public string Error { get; set; } = string.Empty;
    public string? ExistingUserId { get; set; }
}
