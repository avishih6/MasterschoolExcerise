namespace UserRegistrationService.DI.Services;

public interface IUserRegistrationProcessor
{
    Task ProcessUserRegistrationAsync(UserRegistrationEvent registrationEvent, CancellationToken cancellationToken = default);
}

public class UserRegistrationEvent
{
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string EventType { get; set; } = string.Empty;
}
