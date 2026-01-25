using Azure.Messaging.ServiceBus;
using AdmissionProcessDAL.Models;
using AdmissionProcessApi.Configuration;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace AdmissionProcessApi.Services;

public class ServiceBusService : IServiceBusService
{
    private readonly ServiceBusSender? _sender;
    private readonly ILogger<ServiceBusService> _logger;
    private readonly ServiceBusOptions _options;

    public ServiceBusService(
        ServiceBusSender? sender,
        IOptions<ServiceBusOptions> options,
        ILogger<ServiceBusService> logger)
    {
        _sender = sender;
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendUserRegistrationMessageAsync(User user)
    {
        if (_sender == null)
        {
            _logger.LogWarning("Service Bus sender not initialized. Message not sent.");
            return;
        }

        try
        {
            var messageBody = JsonSerializer.Serialize(new
            {
                UserId = user.Id,
                Email = user.Email,
                CreatedAt = user.CreatedAt,
                EventType = "UserRegistered"
            });

            var message = new ServiceBusMessage(Encoding.UTF8.GetBytes(messageBody))
            {
                MessageId = user.Id,
                Subject = "UserRegistration",
                CorrelationId = user.Id,
                ApplicationProperties =
                {
                    { "EventType", "UserRegistered" },
                    { "UserId", user.Id },
                    { "Email", user.Email }
                }
            };

            await _sender.SendMessageAsync(message);
            _logger.LogInformation("User registration message sent to Service Bus for user {UserId}", user.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send message to Service Bus for user {UserId}", user.Id);
            throw;
        }
    }
}
