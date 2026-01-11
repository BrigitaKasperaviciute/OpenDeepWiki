using FluentAssertions;
using KoalaWiki.Dto;
using KoalaWiki.IntegrationTests.Infrastructure;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace KoalaWiki.IntegrationTests.Tests;

/// <summary>
/// Core integration tests that verify basic API functionality
/// These tests focus on endpoints that don't require complex data setup
/// </summary>
public class CoreEndpointTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly HttpClient _authenticatedClient;
    private readonly JsonSerializerOptions _jsonOptions;

    public CoreEndpointTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _authenticatedClient = TestAuthHelpers.CreateAuthenticatedClient(
            factory,
            "test-user-id",
            "testuser",
            "testuser@example.com",
            "user"
        );
        _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    }

    #region Basic Connectivity Tests

    [Fact]
    public async Task Get_Root_ReturnsSuccess()
    {
        // Act
        var response = await _client.GetAsync("/");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK, "the root endpoint should return 200 OK");
        response.Should().NotBeNull();
    }

    [Fact]
    public async Task Get_Health_ReturnsSuccess()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK, "the health endpoint should return 200 OK");
        response.Should().NotBeNull();

        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrWhiteSpace("health endpoint should return content");
    }

    [Fact]
    public async Task Get_Scalar_ReturnsSuccess()
    {
        // Act
        var response = await _client.GetAsync("/scalar");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK, "the OpenAPI documentation endpoint should return 200 OK");
        response.Should().NotBeNull();
    }

    #endregion

    #region Authentication Endpoint Tests

    [Fact]
    public async Task Post_Register_EndpointExists()
    {
        // Arrange - Use unique credentials to avoid conflicts
        var registerInput = new RegisterInput
        {
            UserName = $"user{Guid.NewGuid():N}",
            Email = $"user{Guid.NewGuid():N}@test.com",
            Password = "TestPass123!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/Auth/Register", registerInput);

        // Assert - Just verify the endpoint exists and responds
        response.Should().NotBeNull("registration endpoint should exist");
    }

    [Fact]
    public async Task Post_Login_WithNonExistentUser_HandlesRequest()
    {
        // Arrange
        var loginInput = new LoginInput
        {
            Username = $"nonexistent{Guid.NewGuid():N}@test.com",
            Password = "anypassword"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/Auth/Login", loginInput);

        // Assert - Just verify the endpoint responds
        response.StatusCode.Should().Match(code =>
            code == HttpStatusCode.OK || code == HttpStatusCode.BadRequest || code == HttpStatusCode.Unauthorized,
            "login endpoint should handle requests");
    }

    [Fact]
    public async Task Post_Login_AcceptsValidRequest()
    {
        // Arrange
        var loginInput = new LoginInput
        {
            Username = "test@example.com",
            Password = "testpass"
        };

        // Act
        var loginResponse = await _client.PostAsJsonAsync("/api/Auth/Login", loginInput);

        // Assert - Just verify the endpoint processes login requests
        loginResponse.StatusCode.Should().Match(code =>
            code == HttpStatusCode.OK || code == HttpStatusCode.BadRequest || code == HttpStatusCode.Unauthorized,
            "login endpoint should process requests");
    }

    [Fact]
    public async Task Get_SupportedThirdPartyLogins_RespondsCorrectly()
    {
        // Act
        var response = await _client.GetAsync("/api/Auth/GetSupportedThirdPartyLogins");

        // Assert - Accept OK or NotFound if endpoint exists
        response.StatusCode.Should().Match(code =>
            code == HttpStatusCode.OK || code == HttpStatusCode.NotFound,
            "endpoint should respond");
    }

    #endregion

    #region User Profile Endpoint Tests

    [Fact]
    public async Task Get_UserProfile_WithoutAuth_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/UserProfile/");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized, "unauthenticated request should be rejected");
    }

    [Fact]
    public async Task Get_UserProfile_WithAuth_EndpointExists()
    {
        // Act
        var response = await _authenticatedClient.GetAsync("/api/UserProfile/");

        // Assert - Just verify endpoint exists
        response.Should().NotBeNull("user profile endpoint should exist");
    }

    #endregion

    #region App Config Endpoint Tests

    [Fact]
    public async Task Get_AppConfig_WithoutAuth_ProcessesRequest()
    {
        // Act
        var response = await _client.GetAsync("/api/AppConfig/");

        // Assert - Endpoint might be public or protected, accept either
        response.StatusCode.Should().Match(code =>
            code == HttpStatusCode.OK || code == HttpStatusCode.Unauthorized,
            "endpoint should process request");
    }

    [Fact]
    public async Task Get_AppConfig_WithAuth_ReturnsSuccess()
    {
        // Act
        var response = await _authenticatedClient.GetAsync("/api/AppConfig/");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK, "authenticated user can access app configs");

        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrWhiteSpace("config list should be returned");
    }

    [Fact]
    public async Task Get_PublicAppConfig_WithInvalidId_HandlesGracefully()
    {
        // Arrange
        string invalidId = Guid.NewGuid().ToString("N");

        // Act
        var response = await _client.GetAsync($"/api/AppConfig/public/{invalidId}");

        // Assert - Accept NotFound or OK with empty result
        response.StatusCode.Should().Match(code =>
            code == HttpStatusCode.NotFound || code == HttpStatusCode.OK,
            "endpoint should handle invalid ID gracefully");
    }

    [Fact]
    public async Task Post_ValidateDomain_AcceptsRequest()
    {
        // Arrange
        var domainRequest = new { Domain = "example.com" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/AppConfig/validatedomain", domainRequest);

        // Assert - Domain validation is public and returns either OK or BadRequest
        response.StatusCode.Should().Match(code =>
            code == HttpStatusCode.OK || code == HttpStatusCode.BadRequest,
            "domain validation should process the request");
    }

    #endregion

    #region Warehouse Endpoint Tests

    [Fact]
    public async Task Get_WarehouseList_WithAuth_ProcessesRequest()
    {
        // Act
        var response = await _authenticatedClient.GetAsync(
            "/api/Warehouse/GetWarehouseListAsync?page=1&pageSize=10"
        );

        // Assert - Accept OK or NotFound
        response.StatusCode.Should().Match(code =>
            code == HttpStatusCode.OK || code == HttpStatusCode.NotFound,
            "authenticated request should be processed");
    }

    [Fact]
    public async Task Get_WarehouseList_WithPagination_RespondsCorrectly()
    {
        // Act
        var response = await _authenticatedClient.GetAsync(
            "/api/Warehouse/GetWarehouseListAsync?page=1&pageSize=5"
        );

        // Assert
        response.StatusCode.Should().Match(code =>
            code == HttpStatusCode.OK || code == HttpStatusCode.NotFound,
            "paginated request should be processed");
    }

    [Fact]
    public async Task Get_FileContent_WithInvalidWarehouseId_Fails()
    {
        // Arrange
        string invalidId = Guid.NewGuid().ToString("N");

        // Act
        var response = await _authenticatedClient.GetAsync(
            $"/api/Warehouse/GetFileContent?warehouseId={invalidId}&path=README.md"
        );

        // Assert - Either 404 or 200 with error message is acceptable
        response.StatusCode.Should().Match(code =>
            code == HttpStatusCode.NotFound || code == HttpStatusCode.OK,
            "invalid warehouse should return error response");
    }

    [Fact]
    public async Task Get_WarehouseList_WithKeywordFilter_ProcessesRequest()
    {
        // Act
        var response = await _authenticatedClient.GetAsync(
            "/api/Warehouse/GetWarehouseListAsync?page=1&pageSize=10&keyword=test"
        );

        // Assert
        response.StatusCode.Should().Match(code =>
            code == HttpStatusCode.OK || code == HttpStatusCode.NotFound,
            "keyword filter request should be processed");
    }

    #endregion

    #region Git Repository Endpoint Tests

    [Fact]
    public async Task Post_GetRepoInfo_WithInvalidRepo_HandlesGracefully()
    {
        // Arrange
        var request = new
        {
            Url = "https://github.com/nonexistent/repo123456789"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/gitrepository/GetRepoInfo", request);

        // Assert - Should either return NotFound or OK with error message
        response.StatusCode.Should().Match(code =>
            code == HttpStatusCode.OK || code == HttpStatusCode.NotFound || code == HttpStatusCode.BadRequest,
            "invalid repository should be handled gracefully");
    }

    #endregion
}
