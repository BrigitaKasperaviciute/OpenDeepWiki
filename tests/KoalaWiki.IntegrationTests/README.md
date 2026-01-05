# KoalaWiki Integration Tests

## Overview

These integration tests validate the KoalaWiki API by connecting to a running instance of the application. The tests use HttpClient to make real HTTP requests to the API endpoints.

## Prerequisites

- .NET 9 SDK
- A running instance of KoalaWiki application

## Running the Tests

### Step 1: Start the KoalaWiki Application

First, start the application in a separate terminal:

```powershell
# From the project root
dotnet run --project src/KoalaWiki/KoalaWiki.csproj
```

The application should start on `http://localhost:5085` by default.

### Step 2: Run the Tests

In another terminal, run the tests:

```powershell
# Run all tests
dotnet test tests/KoalaWiki.IntegrationTests/KoalaWiki.IntegrationTests.csproj

# Run with detailed output
dotnet test tests/KoalaWiki.IntegrationTests/KoalaWiki.IntegrationTests.csproj --verbosity normal

# Run specific test class
dotnet test tests/KoalaWiki.IntegrationTests/KoalaWiki.IntegrationTests.csproj --filter "FullyQualifiedName~HealthCheckTests"
```

## Configuration

### Custom Base URL

If your application runs on a different URL, set the `KOALAWIKI_TEST_URL` environment variable:

```powershell
# PowerShell
$env:KOALAWIKI_TEST_URL="http://localhost:8080"
dotnet test tests/KoalaWiki.IntegrationTests/KoalaWiki.IntegrationTests.csproj

# Command Prompt
set KOALAWIKI_TEST_URL=http://localhost:8080
dotnet test tests\KoalaWiki.IntegrationTests\KoalaWiki.IntegrationTests.csproj
```

## Test Structure

- **ApiTestBase.cs**: Base class for all API tests, provides HttpClient and server connectivity checks
- **HealthCheckTests.cs**: Tests for health check and basic endpoints
- **WarehouseApiTests.cs**: Tests for warehouse/repository API endpoints
- **AuthenticationTests.cs**: Tests for authentication and user management
- **DocumentApiTests.cs**: Tests for document API endpoints

## Test Behavior

- Tests will automatically skip if the server is not running
- The first test (`Server_ShouldBeRunning`) will fail if the server is not accessible
- All other tests check server availability before executing

## Troubleshooting

### "Server is not running" Error

Make sure the KoalaWiki application is running before executing tests:

```powershell
dotnet run --project src/KoalaWiki/KoalaWiki.csproj
```

### Connection Refused

Verify the application is listening on the correct port:
- Check the console output when starting the application
- Verify firewall settings are not blocking the connection
- Try accessing `http://localhost:5085/health` in a browser

### Tests Fail with Authentication Errors

Some endpoints may require authentication. The tests are designed to handle this gracefully:
- Authentication tests verify that protected endpoints return 401 Unauthorized
- Public endpoints should return 200 OK

