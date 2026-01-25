namespace AdmissionProcessApi.Configuration;

public class FeatureFlagsOptions
{
    public const string SectionName = "FeatureFlags";

    public bool ServiceBusUserRegistration { get; set; } = false;
}
