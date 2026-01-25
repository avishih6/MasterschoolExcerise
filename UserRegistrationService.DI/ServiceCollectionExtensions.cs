using Azure.Messaging.ServiceBus;
using AdmissionProcessDAL.Services;
using UserRegistrationService.DI.Configuration;
using UserRegistrationService.DI.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace UserRegistrationService.DI;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMasterschoolServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register configuration options
        services.Configure<ServiceBusOptions>(configuration.GetSection(ServiceBusOptions.SectionName));

        // Register DAL data services (all mock storage and insertion logic is here)
        services.AddSingleton<IStepDataService, StepDataService>();
        services.AddSingleton<ITaskDataService, TaskDataService>();
        services.AddSingleton<IStepTaskDataService, StepTaskDataService>();
        services.AddSingleton<IUserProgressDataService, UserProgressDataService>();
        services.AddSingleton<IUserTaskAssignmentDataService, UserTaskAssignmentDataService>();
        services.AddScoped<IUserDataService, UserDataService>();
        services.AddSingleton<IEmailService, MockEmailService>();

        // Register Service Bus client
        services.AddSingleton(serviceProvider =>
        {
            // Try to get connection string from ServiceBus section first, then fallback to ConnectionStrings
            var connectionString = configuration.GetSection(ServiceBusOptions.SectionName)
                .GetValue<string>("ConnectionString");
            
            if (string.IsNullOrEmpty(connectionString))
            {
                connectionString = configuration.GetConnectionString("ServiceBus");
            }
            
            if (string.IsNullOrEmpty(connectionString))
            {
                var logger = serviceProvider.GetRequiredService<ILogger<ServiceBusClient>>();
                logger.LogWarning("Service Bus connection string not configured");
                return null as ServiceBusClient;
            }

            return new ServiceBusClient(connectionString);
        });

        // Register Service Bus sender
        services.AddSingleton(serviceProvider =>
        {
            var client = serviceProvider.GetService<ServiceBusClient>();
            if (client == null)
                return null as ServiceBusSender;

            var optionsMonitor = serviceProvider.GetRequiredService<IOptions<ServiceBusOptions>>();
            var options = optionsMonitor.Value;
            
            return client.CreateSender(options.TopicName);
        });

        // Register Service Bus processor
        services.AddSingleton(serviceProvider =>
        {
            var client = serviceProvider.GetService<ServiceBusClient>();
            if (client == null)
                return null as ServiceBusProcessor;

            var optionsMonitor = serviceProvider.GetRequiredService<IOptions<ServiceBusOptions>>();
            var options = optionsMonitor.Value;
            
            var processorOptions = new ServiceBusProcessorOptions
            {
                AutoCompleteMessages = options.AutoCompleteMessages,
                MaxConcurrentCalls = options.MaxConcurrentCalls,
                MaxAutoLockRenewalDuration = TimeSpan.FromSeconds(options.MaxAutoLockRenewalDuration)
            };

            return client.CreateProcessor(options.TopicName, options.SubscriptionName, processorOptions);
        });

        return services;
    }

    public static IServiceCollection AddUserRegistrationService(
        this IServiceCollection services)
    {
        services.AddHostedService<UserRegistrationBackgroundService>();
        services.AddScoped<IUserRegistrationProcessor, UserRegistrationProcessor>();
        
        return services;
    }
}
