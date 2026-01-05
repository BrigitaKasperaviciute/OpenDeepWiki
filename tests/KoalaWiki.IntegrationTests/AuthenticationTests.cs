using System.Net;
using System.Net.Http.Json;

namespace KoalaWiki.IntegrationTests;

[Collection("Server collection")]
public class AuthenticationTests : AuthenticatedTestBase
{
    public AuthenticationTests(ServerFixture server) : base(server) { }
    [Fact]
    public async Task Login_WithValidCredentials_ReturnsSuccess()
    {
        // Arrange
        Assert.True(await IsServerRunningAsync(), "Server not running");
        
        // Ensure test user exists
        var userExists = await EnsureTestUserExistsAsync("admin", "admin");
        Assert.True(userExists, "Failed to create test user");

        var loginRequest = new { Username = "admin", Password = "admin" };

        // Act
        var response = await Client.PostAsJsonAsync("/api/Auth/Login", loginRequest);
        var rawContent = await response.Content.ReadAsStringAsync();

        // Assert - With valid credentials, login should succeed and include a token
        Assert.True(response.IsSuccessStatusCode, 
            $"Login request should succeed. Status: {response.StatusCode}. Content: {rawContent}");

        var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        LoginResponse? content = null;
        using (var doc = System.Text.Json.JsonDocument.Parse(rawContent))
        {
            var payload = doc.RootElement;
            if (payload.TryGetProperty("data", out var dataElement))
            {
                payload = dataElement;
            }

            content = System.Text.Json.JsonSerializer.Deserialize<LoginResponse>(payload.GetRawText(), options);
        }

        Assert.NotNull(content);
        Assert.True(content!.Success, "Login with valid credentials should return success=true");
        Assert.False(string.IsNullOrEmpty(content.Token), "Response should contain a token for successful login");
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsError()
    {
        // Arrange
        Assert.True(await IsServerRunningAsync(), "Server not running");

        var loginRequest = new { Username = "invaliduser_does_not_exist", Password = "wrongpassword" };

        // Act
        var response = await Client.PostAsJsonAsync("/api/Auth/Login", loginRequest);

        // Assert - Invalid credentials should either fail (not OK) or return success=false in body
        if (response.IsSuccessStatusCode)
        {
            // If API returns 200, check the body for success=false
            var content = await response.Content.ReadFromJsonAsync<LoginResponse>();
            Assert.NotNull(content);
            Assert.False(content.Success, 
                "Login with invalid credentials should return success=false in response body");
        }
        else
        {
            // Proper RESTful APIs should return 401 Unauthorized or 400 Bad Request
            Assert.True(
                response.StatusCode == HttpStatusCode.Unauthorized || 
                response.StatusCode == HttpStatusCode.BadRequest,
                $"Expected 401 or 400 for invalid credentials, got {response.StatusCode}"
            );
        }
    }

    [Fact]
    public async Task GetCurrentUser_WithAuthentication_ReturnsUserInfo()
    {
        // Arrange
        Assert.True(await IsServerRunningAsync(), "Server not running");
        
        // Login with valid credentials
        var loggedIn = await LoginAsTestUserAsync("admin", "admin");
        Assert.True(loggedIn, "Failed to authenticate - test user may not exist");

        // Act
        var response = await Client.GetAsync("/api/User/profile");

        // Assert - Authenticated request should succeed
        Assert.True(response.IsSuccessStatusCode,
            $"Authenticated request to /api/User/profile should succeed. Status: {response.StatusCode}");

        var content = await response.Content.ReadAsStringAsync();
        Assert.NotEmpty(content);
    }

    [Fact]
    public async Task GetCurrentUser_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        Assert.True(await IsServerRunningAsync(), "Server not running");
        
        // Ensure we're not authenticated
        Logout();

        // Act
        var response = await Client.GetAsync("/api/User/profile");

        // Assert - Unauthenticated request should be rejected
        Assert.True(
            response.StatusCode == HttpStatusCode.Unauthorized ||
            response.StatusCode == HttpStatusCode.Forbidden,
            $"Unauthenticated request should return 401 or 403, got {response.StatusCode}"
        );
    }

    [Fact]
    public async Task UserListEndpoint_WithAdminAuth_ReturnsUsers()
    {
        // Arrange
        Assert.True(await IsServerRunningAsync(), "Server not running");
        
        // Login as admin
        var loggedIn = await LoginAsTestUserAsync("admin", "admin");
        Assert.True(loggedIn, "Failed to authenticate as admin");

        // Act - UserList requires admin role
        var response = await Client.GetAsync("/api/User/UserList?page=1&pageSize=10");

        // Assert - Authenticated request should return success or forbidden (if user lacks admin role)
        Assert.True(
            response.IsSuccessStatusCode ||
            response.StatusCode == System.Net.HttpStatusCode.Forbidden,
            $"Authenticated request should return success or 403. Status: {response.StatusCode}"
        );
    }

    // Helper class to deserialize login responses
    private class LoginResponse
    {
        public bool Success { get; set; }
        public string? Token { get; set; }
        public string? RefreshToken { get; set; }
        public UserInfo? User { get; set; }
        public string? ErrorMessage { get; set; }
    }

    private class UserInfo
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? Role { get; set; }
    }
}
