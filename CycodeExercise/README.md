# Cycode Vulnerability Scanner

A WebAPI application that scans project definition files for vulnerable packages using the GitHub Security Vulnerabilities API.

## Prerequisites

- .NET 10.0 SDK
- GitHub Personal Access Token (no scope required)
  - Generate at: https://github.com/settings/tokens

## Project Structure

```
CycodeExercise/
├── CycodeExerciseApi/           # Main API project
│   ├── Controllers/
│   ├── Interfaces/
│   ├── Models/
│   └── Services/
├── CycodeExerciseApi.Tests/     # Unit & Integration tests
│   ├── Unit/
│   └── Integration/
├── TestData/                    # Manual test data & curl commands
└── README.md
```

## Setup

### 1. Set the GitHub Access Token

The application requires a GitHub personal access token to query the Security Vulnerabilities API.

**On macOS/Linux:**
```bash
export GITHUB_ACCESS_TOKEN="your_token_here"
```

**On Windows (PowerShell):**
```powershell
$env:GITHUB_ACCESS_TOKEN="your_token_here"
```

**On Windows (Command Prompt):**
```cmd
set GITHUB_ACCESS_TOKEN=your_token_here
```

> Note: The app also supports `GITHUB-ACCESS-TOKEN` if your shell allows it.

### 2. Run the Application

```bash
cd CycodeExerciseApi
dotnet run
```

The API will start on `https://localhost:5001` (or similar port shown in console).

### 3. Access Swagger UI

Open your browser and navigate to:
```
https://localhost:5001/swagger
```

## API Usage

### Endpoint

```
POST /api/v1/scan
```

### Request Body

```json
{
  "ecosystem": "npm",
  "fileContent": "<base64-encoded-package.json>"
}
```

| Parameter | Required | Description |
|-----------|----------|-------------|
| ecosystem | Yes | Package ecosystem (currently supports: `npm`) |
| fileContent | Yes | Base64-encoded project definition file content |

### Response

```json
{
  "vulnerablePackages": [
    {
      "name": "package-name",
      "version": "1.0.0",
      "vulnerabilities": [
        {
          "summary": "Description of the vulnerability",
          "severity": "CRITICAL",
          "firstPatchedVersion": "1.0.1"
        }
      ]
    }
  ]
}
```

## Example

### Encoding a package.json file

**On macOS/Linux:**
```bash
base64 -i package.json
```

**On Windows (PowerShell):**
```powershell
[Convert]::ToBase64String([IO.File]::ReadAllBytes("package.json"))
```

### Sample Request

```bash
curl -X POST https://localhost:5001/api/v1/scan \
  -H "Content-Type: application/json" \
  -d '{
    "ecosystem": "npm",
    "fileContent": "ewogICJuYW1lIjogIk15IEFwcGxpY2F0aW9uIiwKICAidmVyc2lvbiI6ICIxLjAuMCIsCiAgImRlcGVuZGVuY2llcyI6IHsKICAgICJkZWVwLW92ZXJyaWRlIjogIjEuMC4xIiwKICAgICJtdXN0YWNoZSI6ICIyLjEuMSIKICB9Cn0K"
  }'
```

### Expected Response

```json
{
  "vulnerablePackages": [
    {
      "name": "deep-override",
      "version": "1.0.1",
      "vulnerabilities": [
        {
          "summary": "Prototype Pollution in deep-override",
          "severity": "CRITICAL",
          "firstPatchedVersion": "1.0.2"
        }
      ]
    },
    {
      "name": "mustache",
      "version": "2.1.1",
      "vulnerabilities": [
        {
          "summary": "Cross-Site Scripting in mustache",
          "severity": "HIGH",
          "firstPatchedVersion": "2.2.1"
        }
      ]
    }
  ]
}
```

## Architecture

The application is designed with extensibility in mind:

### Interfaces

- **IFileParser**: Implement to support new ecosystems (pip, nuget, etc.)
- **IVulnerabilityProvider**: Implement to switch vulnerability data sources (Snyk, etc.)
- **IScanService**: Main orchestration service

### Current Implementations

- `NpmFileParser`: Parses npm package.json files
- `GitHubVulnerabilityProvider`: Queries GitHub Security Vulnerabilities GraphQL API

### Adding New Ecosystems

1. Create a new parser implementing `IFileParser`:
```csharp
public class PipFileParser : IFileParser
{
    public string Ecosystem => "pip";
    
    public Task<IEnumerable<PackageDependency>> ParseAsync(string fileContent)
    {
        // Parse requirements.txt
    }
}
```

2. Register in `Program.cs`:
```csharp
builder.Services.AddSingleton<IFileParser, PipFileParser>();
```

### Switching Vulnerability Providers

1. Implement `IVulnerabilityProvider` for the new source
2. Replace the registration in `Program.cs`

## Error Handling

| Status Code | Description |
|-------------|-------------|
| 200 | Successful scan (may return empty vulnerablePackages) |
| 400 | Invalid request (unsupported ecosystem, invalid base64, invalid file format) |
| 503 | Vulnerability service unavailable |
| 500 | Internal server error |

## Assumptions

1. Package versions in package.json are specific versions without modifiers (^, ~, etc.)
   - However, the parser gracefully handles and strips these modifiers if present
2. Only the `dependencies` and `devDependencies` sections are scanned
3. The GitHub API rate limits apply (5000 requests/hour with authentication)

## Testing

### Run Unit Tests
```bash
cd CycodeExerciseApi.Tests
dotnet test --filter "FullyQualifiedName!~Integration"
```

### Run Integration Tests (requires GitHub token)
```bash
export GITHUB_ACCESS_TOKEN="your_token_here"
cd CycodeExerciseApi.Tests
dotnet test
```

### Manual Testing
See `TestData/README.md` for curl commands and test data.

## Detailed Project Structure

```
CycodeExercise/
├── CycodeExerciseApi/
│   ├── Controllers/
│   │   └── ScanController.cs           # API endpoint
│   ├── Interfaces/
│   │   ├── IFileParser.cs              # File parsing contract
│   │   ├── IVulnerabilityProvider.cs   # Vulnerability source contract
│   │   └── IScanService.cs             # Scan orchestration contract
│   ├── Models/
│   │   ├── Requests/ScanRequest.cs
│   │   └── Responses/
│   │       ├── ScanResponse.cs
│   │       └── ErrorResponse.cs
│   ├── Services/
│   │   ├── FileParsers/
│   │   │   └── NpmFileParser.cs        # npm package.json parser
│   │   ├── VulnerabilityProviders/
│   │   │   └── GitHubVulnerabilityProvider.cs
│   │   └── ScanService.cs              # Main scan orchestration
│   └── Program.cs                       # Application entry point
├── CycodeExerciseApi.Tests/
│   ├── Unit/
│   │   ├── NpmFileParserTests.cs       # Parser unit tests
│   │   └── ScanServiceTests.cs         # Service unit tests
│   └── Integration/
│       └── ScanControllerIntegrationTests.cs
├── TestData/                            # Manual test data
└── README.md
```
