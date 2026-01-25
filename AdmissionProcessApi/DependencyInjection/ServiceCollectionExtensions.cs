using AdmissionProcessDAL.Repositories;
using AdmissionProcessDAL.Repositories.Interfaces;
using AdmissionProcessDAL.Services;
using AdmissionProcessApi.Services;
using AdmissionProcessApi.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AdmissionProcessApi.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAdmissionProcessServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register configuration options
        services.Configure<ServiceBusOptions>(configuration.GetSection(ServiceBusOptions.SectionName));
        services.Configure<FeatureFlagsOptions>(configuration.GetSection(FeatureFlagsOptions.SectionName));

        // Register DAL data services (all mock storage and insertion logic is here)
        services.AddSingleton<IStepDataService, StepDataService>();
        services.AddSingleton<ITaskDataService, TaskDataService>();
        services.AddSingleton<IStepTaskDataService, StepTaskDataService>();
        services.AddSingleton<IUserProgressDataService, UserProgressDataService>();
        services.AddSingleton<IUserTaskAssignmentDataService, UserTaskAssignmentDataService>();
        services.AddScoped<IUserDataService, UserDataService>();

        // Register Service Bus (only sender for web app)
        services.AddSingleton(serviceProvider =>
        {
            var connectionString = configuration.GetSection(ServiceBusOptions.SectionName)
                .GetValue<string>("ConnectionString");
            
            if (string.IsNullOrEmpty(connectionString))
            {
                connectionString = configuration.GetConnectionString("ServiceBus");
            }
            
            if (string.IsNullOrEmpty(connectionString))
            {
                var logger = serviceProvider.GetRequiredService<ILogger<Azure.Messaging.ServiceBus.ServiceBusClient>>();
                logger.LogWarning("Service Bus connection string not configured");
                return null as Azure.Messaging.ServiceBus.ServiceBusClient;
            }

            return new Azure.Messaging.ServiceBus.ServiceBusClient(connectionString);
        });

        services.AddSingleton(serviceProvider =>
        {
            var client = serviceProvider.GetService<Azure.Messaging.ServiceBus.ServiceBusClient>();
            if (client == null)
                return null as Azure.Messaging.ServiceBus.ServiceBusSender;

            var options = configuration.GetSection(ServiceBusOptions.SectionName)
                .Get<ServiceBusOptions>() ?? new ServiceBusOptions();
            
            return client.CreateSender(options.TopicName);
        });

        // Register additional DAL services
        services.AddSingleton<IEmailService, MockEmailService>();

        // Register application services
        services.AddSingleton<IConditionConfigurationService, ConditionConfigurationService>();
        services.AddSingleton<IConditionEvaluator, ConditionEvaluator>();
        services.AddSingleton<IFeatureFlagService, FeatureFlagService>();
        services.AddSingleton<IServiceBusService, ServiceBusService>();
        services.AddSingleton<IFlowDataSeeder, FlowDataSeeder>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IFlowService, FlowService>();
        services.AddScoped<IProgressService, ProgressService>();
        services.AddScoped<IStepManagementService, StepManagementService>();
        services.AddScoped<ITaskManagementService, TaskManagementService>();

        return services;
    }
}
