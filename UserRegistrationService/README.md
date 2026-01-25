# Service Bus Consumer

This console application consumes user registration messages from Azure Service Bus.

## Configuration

Update `appsettings.json` with your Azure Service Bus connection string:

```json
{
  "ConnectionStrings": {
    "ServiceBus": "Endpoint=sb://your-namespace.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=your-key"
  },
  "ServiceBus": {
    "TopicName": "user-registrations",
    "SubscriptionName": "user-registration-processor"
  }
}
```

## Running

```bash
dotnet run
```

## Features

- Consumes messages from Azure Service Bus topic
- Processes user registration events
- Handles errors and dead-letter messages
- Graceful shutdown on Ctrl+C

## Message Format

The consumer expects messages in the following format:

```json
{
  "UserId": "123",
  "Email": "user@example.com",
  "CreatedAt": "2026-01-24T12:00:00Z",
  "EventType": "UserRegistered"
}
```
