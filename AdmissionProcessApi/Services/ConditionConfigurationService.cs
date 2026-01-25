using System.Text.Json;

namespace AdmissionProcessApi.Services;

public class ConditionConfigurationService : IConditionConfigurationService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ConditionConfigurationService> _logger;
    private Dictionary<string, object>? _conditionsConfig;
    private readonly string _configFilePath;

    public ConditionConfigurationService(
        IConfiguration configuration,
        IHostEnvironment environment,
        ILogger<ConditionConfigurationService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _configFilePath = Path.Combine(environment.ContentRootPath, "Configuration", "conditions.json");
        LoadConfiguration();
    }

    private void LoadConfiguration()
    {
        try
        {
            if (File.Exists(_configFilePath))
            {
                var json = File.ReadAllText(_configFilePath);
                _conditionsConfig = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
                _logger.LogInformation("Condition configuration loaded from {FilePath}", _configFilePath);
            }
            else
            {
                _logger.LogWarning("Condition configuration file not found at {FilePath}. Using defaults.", _configFilePath);
                _conditionsConfig = new Dictionary<string, object>();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load condition configuration from {FilePath}", _configFilePath);
            _conditionsConfig = new Dictionary<string, object>();
        }
    }

    public async Task<string> GetPassingConditionConfigAsync(string conditionName)
    {
        if (_conditionsConfig == null)
        {
            await ReloadConfigurationAsync();
        }

        try
        {
            if (_conditionsConfig != null &&
                _conditionsConfig.TryGetValue("passingConditions", out var passingConditionsObj) &&
                passingConditionsObj is JsonElement passingConditions)
            {
                if (passingConditions.TryGetProperty(conditionName, out var condition))
                {
                    if (condition.TryGetProperty("config", out var config))
                    {
                        return config.GetRawText();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading passing condition config for {ConditionName}", conditionName);
        }

        return "{}";
    }

    public async Task<string> GetVisibilityConditionConfigAsync(string conditionName)
    {
        if (_conditionsConfig == null)
        {
            await ReloadConfigurationAsync();
        }

        try
        {
            if (_conditionsConfig != null &&
                _conditionsConfig.TryGetValue("visibilityConditions", out var visibilityConditionsObj) &&
                visibilityConditionsObj is JsonElement visibilityConditions)
            {
                if (visibilityConditions.TryGetProperty(conditionName, out var condition))
                {
                    if (condition.TryGetProperty("config", out var config))
                    {
                        return config.GetRawText();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading visibility condition config for {ConditionName}", conditionName);
        }

        return "{}";
    }

    public async Task<Dictionary<string, string>> GetTaskConditionsAsync(string taskName)
    {
        if (_conditionsConfig == null)
        {
            await ReloadConfigurationAsync();
        }

        var result = new Dictionary<string, string>();

        try
        {
            if (_conditionsConfig != null &&
                _conditionsConfig.TryGetValue("taskConditions", out var taskConditionsObj) &&
                taskConditionsObj is JsonElement taskConditions)
            {
                if (taskConditions.TryGetProperty(taskName, out var taskCondition))
                {
                    if (taskCondition.TryGetProperty("passingCondition", out var passingCondition))
                    {
                        result["passingCondition"] = passingCondition.GetString() ?? "always";
                    }
                    if (taskCondition.TryGetProperty("visibilityCondition", out var visibilityCondition))
                    {
                        result["visibilityCondition"] = visibilityCondition.GetString() ?? "";
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading task conditions for {TaskName}", taskName);
        }

        return result;
    }

    public Task ReloadConfigurationAsync()
    {
        LoadConfiguration();
        return Task.CompletedTask;
    }
}
