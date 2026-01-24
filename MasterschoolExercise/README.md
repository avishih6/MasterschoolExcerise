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

- The flow configuration is easily modifiable in `FlowConfiguration.cs`
- Mock repositories use in-memory storage (data is lost on restart)
- Conditional tasks are supported (e.g., second chance IQ test)
- All endpoints include proper error handling and HTTP status codes
