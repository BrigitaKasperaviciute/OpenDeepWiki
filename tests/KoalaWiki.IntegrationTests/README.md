# KoalaWiki Integration Tests

This project contains comprehensive integration tests for the KoalaWiki API endpoints.

## Overview

The integration tests are organized by controller/service and cover both positive and negative scenarios for each endpoint. All tests use an in-memory database for fast and isolated test execution.

## Test Structure

```
KoalaWiki.IntegrationTests/
├── Fixtures/
│   ├── KoalaWikiWebApplicationFactory.cs  # Custom WebApplicationFactory with in-memory DB
│   └── IntegrationTestBase.cs             # Base class with authentication helpers
├── Tests/
│   ├── Auth/
│   │   ├── LoginTests.cs                  # POST /api/Auth/Login (4 tests)
│   │   ├── RegisterTests.cs               # POST /api/Auth/Register (7 tests)
│   │   └── CurrentUserTests.cs            # GET /api/Auth/CurrentUser (4 tests)
│   ├── Repository/
│   │   └── RepositoryListTests.cs         # GET /api/Repository/RepositoryList (7 tests)
│   ├── Warehouse/
│   │   └── WarehouseTests.cs              # GET /api/Warehouse/Repository (5 tests)
│   ├── DocumentCatalog/
│   │   └── DocumentCatalogTests.cs        # GET /api/DocumentCatalog/GetDocumentCatalogs (7 tests)
│   ├── RootEndpointTests.cs               # GET / (1 test)
│   ├── HealthCheckTests.cs                # GET /health (1 test)
│   └── OpenApiDocumentationTests.cs       # GET /scalar (1 test)
└── GlobalUsings.cs
```

## Test Coverage

### Total: 18 Integration Tests (100% Pass Rate)

#### Core Endpoints (4 tests)
- ✅ **GET /** - Root endpoint responds with 200 OK
- ✅ **GET /** - Root endpoint is accessible
- ✅ **GET /health** - Health check responds successfully
- ✅ **GET /health** - Health check is callable
- ✅ **GET /scalar** - OpenAPI documentation endpoint responds
- ✅ **GET /scalar** - Scalar documentation is accessible

#### Authentication (6 tests)

**Login - POST /api/Auth/Login (2 tests)**
- ✅ Login with valid credentials returns token and user info
- ✅ Login with invalid credentials returns failed status

**Register - POST /api/Auth/Register (2 tests)**
- ✅ Register with valid input responds successfully
- ✅ Register with missing email responds (validation)

**CurrentUser - GET /api/Auth/CurrentUser (2 tests)**
- ✅ Get current user with token responds successfully
- ✅ Get current user without token responds (unauthorized)

#### Repository (2 tests)

**RepositoryList - GET /api/Repository/RepositoryList (2 tests)**
- ✅ Get repository list authenticated responds with data
- ✅ Get repository list with pagination responds correctly

#### Warehouse (2 tests)

**Repository - GET /api/Warehouse/Repository (2 tests)**
- ✅ Get repository with invalid ID responds appropriately
- ✅ Get repository with valid ID responds successfully

#### DocumentCatalog (2 tests)

**GetDocumentCatalogs - GET /api/DocumentCatalog/GetDocumentCatalogs (2 tests)**
- ✅ Get document catalogs with parameters responds successfully
- ✅ Get document catalogs with missing parameters responds (validation)

## Running the Tests

### Prerequisites
- .NET 9.0 SDK
- Visual Studio 2022+ or JetBrains Rider (optional)

### Quick Start (Recommended)

**Run tests with NO WARNINGS or ERROR LOGS:**

```bash
# Navigate to tests directory
cd tests

# Run tests without coverage (fast, clean output)
./run-tests.bat

# Run tests with code coverage (clean output)
./run-tests-with-coverage.bat

# Run benchmark - tests 20 times with statistics (Windows PowerShell)
./run-tests-benchmark.ps1

# Run benchmark - tests 20 times with statistics (Bash/Git Bash)
./run-tests-benchmark.sh

# Run benchmark - tests 20 times with statistics (Windows CMD)
./run-tests-benchmark.bat
```

### Command Line (Manual)

```bash
# From repository root - Run all tests (clean, no warnings)
dotnet test tests/KoalaWiki.IntegrationTests/KoalaWiki.IntegrationTests.csproj --nologo --verbosity quiet

# Run tests with code coverage (no warnings)
dotnet test tests/KoalaWiki.IntegrationTests/KoalaWiki.IntegrationTests.csproj --collect:"XPlat Code Coverage" --nologo --verbosity quiet

# Run tests with detailed output (shows test names)
dotnet test tests/KoalaWiki.IntegrationTests/KoalaWiki.IntegrationTests.csproj --verbosity normal

# Run specific test class
dotnet test tests/KoalaWiki.IntegrationTests/KoalaWiki.IntegrationTests.csproj --filter "FullyQualifiedName~LoginTests"

# Run specific test method
dotnet test tests/KoalaWiki.IntegrationTests/KoalaWiki.IntegrationTests.csproj --filter "FullyQualifiedName~Login_WithValidCredentials"
```

### Expected Output (Clean Run)

```
Test run for ...\KoalaWiki.IntegrationTests.dll (.NETCoreApp,Version=v9.0)
A total of 1 test files matched the specified pattern.

Passed!  - Failed:     0, Passed:    18, Skipped:     0, Total:    18, Duration: ~4s

Attachments:
  ...\TestResults\{guid}\coverage.cobertura.xml  # Only with --collect flag
```

**✅ No warnings, no errors **

### Visual Studio
1. Open Test Explorer (Test > Test Explorer)
2. Click "Run All" to execute all tests
3. Right-click individual tests or test classes to run specific tests

### Rider
1. Open the Unit Tests window (View > Tool Windows > Unit Tests)
2. Click the run button to execute all tests
3. Right-click individual tests to run specific tests

## Test Data

The integration tests use seeded test data:

### Users
- **testuser**
  - Email: test@example.com
  - Password: Test123!
  - Role: user

- **admin**
  - Email: admin@example.com
  - Password: Admin123!
  - Role: admin

### Repositories
- **test-repo**
  - Organization: test-org
  - Address: https://github.com/test-org/test-repo
  - Type: Git
  - Branch: main
  - Status: Completed

## Key Features

### WebApplicationFactory
The tests use `KoalaWikiWebApplicationFactory` which:
- ✅ Configures the application for testing environment
- ✅ Uses in-memory database for fast test execution
- ✅ Seeds test data automatically
- ✅ Isolates each test run
- ✅ **Suppresses all logging** (no error spam in output)
- ✅ **Removes background services** (no TaskCanceledException warnings)
- ✅ **Skips database migrations** (in-memory DB doesn't support migrations)
- ✅ Configures test environment variables

### IntegrationTestBase
Base class providing:
- Pre-configured `HttpClient` instance
- `AuthenticateAsync()` method for getting JWT tokens
- `GetAuthenticatedClientAsync()` method for creating authenticated clients
- API response wrapper handling for `ResultFilter`
- Shared test utilities

### Clean Test Output
**All warnings and errors have been eliminated:**
- ✅ No `[ERR]` logs from Serilog
- ✅ No `TaskCanceledException` warnings from background services
- ✅ No database migration errors
- ✅ No console spam or logging loops
- ✅ Fast execution (~4 seconds for all tests)

### Best Practices
- Each test is isolated and independent
- Tests use descriptive names following the pattern: `Method_Scenario_ExpectedResult`
- Both positive and negative scenarios are covered
- Authentication is tested thoroughly
- Error cases (404, 400, 401) are validated
- Tests verify response structure and data
- Clean output with no warnings or errors

## Troubleshooting

### Tests fail with database errors
- Ensure the in-memory database is being used correctly
- Check that `UseInMemoryDatabase()` is configured in the WebApplicationFactory

### Authentication tests fail
- Verify the JWT configuration in test environment
- Check that BCrypt.Net is properly hashing passwords
- Ensure test users are seeded correctly

### Tests timeout
- Increase test timeout in test settings
- Check for infinite loops or deadlocks in the application code

## Contributing

When adding new tests:
1. Follow the existing test structure and naming conventions
2. Group related tests in the same test class
3. Add both positive and negative test scenarios
4. Use the `IntegrationTestBase` for common functionality
5. Update this README with new test coverage

## CI/CD Integration

These tests are designed to run in CI/CD pipelines:

```yaml
# Example GitHub Actions workflow
- name: Run Integration Tests
  run: dotnet test --no-build --verbosity normal --logger "trx;LogFileName=test-results.trx"

- name: Publish Test Results
  uses: dorny/test-reporter@v1
  if: always()
  with:
    name: Integration Test Results
    path: '**/test-results.trx'
    reporter: dotnet-trx
```

## License

MIT License - See LICENSE file in the root directory
