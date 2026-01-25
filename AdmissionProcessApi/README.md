# Masterschool Software Engineer Exercise

A .NET 10 Web API application implementing the Masterschool admissions process flow.

## Features

- **User Management**: Create users with unique IDs
- **Flow Management**: Configurable admission flow with steps and tasks
- **Progress Tracking**: Track user progress through the admission process
- **Status Checking**: Determine if a user is accepted, rejected, or in progress
- **Conditional Tasks**: Support for conditional tasks (e.g., second chance IQ test)

## Architecture

The solution follows a clean architecture pattern:

- **Controllers**: API endpoints
- **Services**: Business logic
- **Repositories**: Data access (mock in-memory storage)
- **Models**: Data models and DTOs
- **Configuration**: Flow definition

## API Endpoints

### 1. Create User
```
POST /api/users
Body: { "email": "user@example.com" }
Response: { "userId": "1" }
```

### 2. Get Flow
```
GET /api/flow?userId={userId}
Response: { "steps": [...] }
```

### 3. Get User Progress
```
GET /api/users/{userId}/progress
Response: { "currentStep": "...", "currentTask": "...", "currentStepNumber": 3, "totalSteps": 6 }
```

### 4. Complete Step
```
PUT /api/users/{userId}/steps/{stepName}
Body: { "key": "value", ... } (step payload)
```

### 5. Get User Status
```
GET /api/users/{userId}/status
Response: { "status": "accepted" | "rejected" | "in_progress" }
```

## Running the Application

1. Ensure .NET 10 SDK is installed
2. Navigate to the project directory
3. Run `dotnet restore`
4. Run `dotnet run`
5. Open `http://localhost:5000/swagger` for API documentation

## Flow Steps

1. **Personal Details Form** - Basic user information
2. **IQ Test** - Score > 75 to pass (with optional second chance for scores 60-75)
3. **Interview** - Schedule and perform interview (decision must be "passed_interview")
4. **Sign Contract** - Upload ID document and sign contract
5. **Payment** - Complete payment
6. **Join Slack** - Final step

## Notes

- **Database-like Structure**: Steps and Tasks are stored in separate repositories (mock DB tables) with IDs
- **External Condition Configuration**: All passing and visibility conditions are configured in `Configuration/conditions.json` - no code changes needed
- **Feature Flags**: Service Bus integration can be enabled/disabled via `FeatureFlags:ServiceBusUserRegistration` in appsettings.json
- **Azure Service Bus**: User registration can optionally send messages to Service Bus topic when feature flag is enabled
- **Mock repositories**: Use in-memory storage (data is lost on restart)
- **Conditional tasks**: Supported (e.g., second chance IQ test) - configured externally
- All endpoints include proper error handling and HTTP status codes

## Service Bus Integration

### Enabling Service Bus

1. Set the connection string in `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "ServiceBus": "Endpoint=sb://your-namespace.servicebus.windows.net/;..."
  }
}
```

2. Enable the feature flag:
```json
{
  "FeatureFlags": {
    "ServiceBusUserRegistration": true
  }
}
```

3. Run the Service Bus Consumer console app (see `../ServiceBusConsumer/README.md`)

## External Condition Configuration

All task conditions are configured in `Configuration/conditions.json`. You can:
- Modify passing conditions (score thresholds, decision matching, etc.)
- Modify visibility conditions (score ranges, user-specific, etc.)
- Add new condition types
- All without code changes - just update the JSON file and restart the app
