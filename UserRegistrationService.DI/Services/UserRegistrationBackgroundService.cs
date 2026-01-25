using Azure.Messaging.ServiceBus;
using UserRegistrationService.DI.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace UserRegistrationService.DI.Services;

public class UserRegistrationBackgroundService : BackgroundService
{
    private readonly ServiceBusProcessor? _processor;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<UserRegistrationBackgroundService> _logger;
    private readonly ServiceBusOptions _options;

    public UserRegistrationBackgroundService(
        ServiceBusProcessor? processor,
        IServiceProvider serviceProvider,
        IOptions<ServiceBusOptions> options,
        ILogger<UserRegistrationBackgroundService> logger)
    {
        _processor = processor;
        _serviceProvider = serviceProvider;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_processor == null)
        {
            _logger.LogWarning("Service Bus processor is not configured. UserRegistrationBackgroundService will not start.");
            return;
        }

        _processor.ProcessMessageAsync += ProcessMessageAsync;
        _processor.ProcessErrorAsync += ProcessErrorAsync;

        _logger.LogInformation(
            "Starting User Registration Service Bus consumer. Topic: {TopicName}, Subscription: {SubscriptionName}",
            _options.TopicName, _options.SubscriptionName);

        try
        {
            await _processor.StartProcessingAsync(stoppingToken);
            _logger.LogInformation("Service Bus processor started successfully");

            // Keep running until cancellation
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Service Bus processor is stopping due to cancellation");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Service Bus processor");
            throw;
        }
    }

    private async Task ProcessMessageAsync(ProcessMessageEventArgs args)
    {
        try
        {
            var body = args.Message.Body.ToString();
            _logger.LogInformation(
                "Received message: MessageId={MessageId}, CorrelationId={CorrelationId}",
                args.Message.MessageId, args.Message.CorrelationId);

            // Parse the message
            var registrationEvent = JsonSerializer.Deserialize<UserRegistrationEvent>(body);
            
            if (registrationEvent == null)
            {
                _logger.LogWarning("Failed to deserialize message: MessageId={MessageId}", args.Message.MessageId);
                await args.DeadLetterMessageAsync(args.Message, "InvalidMessageFormat", "Message could not be deserialized");
                return;
            }

            // Process using scoped service
            var scopeFactory = _serviceProvider.GetRequiredService<IServiceScopeFactory>();
            using var scope = scopeFactory.CreateScope();
            var processor = scope.ServiceProvider.GetRequiredService<IUserRegistrationProcessor>();
            
            await processor.ProcessUserRegistrationAsync(registrationEvent, args.CancellationToken);

            // Complete the message
            await args.CompleteMessageAsync(args.Message);
            _logger.LogInformation(
                "Successfully processed user registration: UserId={UserId}",
                registrationEvent.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message: MessageId={MessageId}", args.Message.MessageId);
            await args.DeadLetterMessageAsync(args.Message, "ProcessingError", ex.Message);
        }
    }

    private Task ProcessErrorAsync(ProcessErrorEventArgs args)
    {
        _logger.LogError(
            args.Exception,
            "Error in Service Bus processor: ErrorSource={ErrorSource}, EntityPath={EntityPath}",
            args.ErrorSource, args.EntityPath);
        return Task.CompletedTask;
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping Service Bus processor...");
        
        if (_processor != null)
        {
            await _processor.StopProcessingAsync(cancellationToken);
            await _processor.DisposeAsync();
        }

        await base.StopAsync(cancellationToken);
        _logger.LogInformation("Service Bus processor stopped");
    }
}
