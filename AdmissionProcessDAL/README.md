# AdmissionProcessDAL

Data Access Layer for the Admission Process system. This package contains all data access logic and mock implementations.

## Building as NuGet Package

To build this as a NuGet package:

```bash
dotnet pack -c Release -o ./packages
```

## Using as NuGet Package

1. Build and pack the package:
   ```bash
   cd AdmissionProcessDAL
   dotnet pack -c Release -o ../packages
   ```

2. Add local NuGet source:
   ```bash
   dotnet nuget add source ./packages --name LocalPackages
   ```

3. In your consuming project's `.csproj`, replace the ProjectReference with:
   ```xml
   <PackageReference Include="AdmissionProcessDAL" Version="1.0.0" />
   ```

## Structure

- **Models**: Domain models (User, Step, FlowTask, etc.)
- **Repositories/Interfaces**: Repository interfaces
- **Repositories**: Mock repository implementations
- **Services**: Data services (IUserDataService, IEmailService) - all data writing goes through these

## Important

All data writing operations should go through the services in this package (e.g., `IUserDataService`), not directly through repositories. This ensures a single source of truth for data operations.
