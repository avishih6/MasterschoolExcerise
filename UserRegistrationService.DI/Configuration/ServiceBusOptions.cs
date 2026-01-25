namespace UserRegistrationService.DI.Configuration;

public class ServiceBusOptions
{
    public const string SectionName = "ServiceBus";

    public string ConnectionString { get; set; } = string.Empty;
    public string TopicName { get; set; } = "user-registrations";
    public string SubscriptionName { get; set; } = "user-registration-processor";
    public int MaxConcurrentCalls { get; set; } = 5;
    public bool AutoCompleteMessages { get; set; } = false;
    public int MaxAutoLockRenewalDuration { get; set; } = 300; // seconds
}
