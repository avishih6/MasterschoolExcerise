using AdmissionProcessModels.Enums;

namespace AdmissionProcessModels.DTOs;

public class StatusResponse
{
    public UserStatus Status { get; set; }
    
    public string StatusString => Status switch
    {
        UserStatus.Accepted => StatusStrings.Accepted,
        UserStatus.Rejected => StatusStrings.Rejected,
        _ => StatusStrings.InProgress
    };
}

public static class StatusStrings
{
    public const string Accepted = "accepted";
    public const string Rejected = "rejected";
    public const string InProgress = "in_progress";
}
