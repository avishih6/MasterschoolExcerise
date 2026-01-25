namespace AdmissionProcessApi.Services;

public interface IConditionConfigurationService
{
    Task<string> GetPassingConditionConfigAsync(string conditionName);
    Task<string> GetVisibilityConditionConfigAsync(string conditionName);
    Task<Dictionary<string, string>> GetTaskConditionsAsync(string taskName);
    Task ReloadConfigurationAsync();
}
