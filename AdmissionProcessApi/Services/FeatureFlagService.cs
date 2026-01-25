using AdmissionProcessApi.Configuration;
using Microsoft.Extensions.Options;

namespace AdmissionProcessApi.Services;

public class FeatureFlagService : IFeatureFlagService
{
    private readonly FeatureFlagsOptions _options;

    public FeatureFlagService(IOptions<FeatureFlagsOptions> options)
    {
        _options = options.Value;
    }

    public bool IsEnabled(string featureName)
    {
        return featureName switch
        {
            "ServiceBusUserRegistration" => _options.ServiceBusUserRegistration,
            _ => false
        };
    }

    public Task<bool> IsEnabledAsync(string featureName)
    {
        return Task.FromResult(IsEnabled(featureName));
    }
}
