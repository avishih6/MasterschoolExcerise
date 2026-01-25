namespace AdmissionProcessApi.Services;

public interface IFeatureFlagService
{
    bool IsEnabled(string featureName);
    Task<bool> IsEnabledAsync(string featureName);
}
