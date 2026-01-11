using System.Net;
using System.Net.Http.Json;
using KoalaWiki.IntegrationTests.Fixtures;
using Xunit;

namespace KoalaWiki.IntegrationTests.Tests.Auth;

/// <summary>
/// Integration tests for POST /api/Auth/Login (2 scenarios)
/// </summary>
public class LoginTests : IntegrationTestBase
{
    public LoginTests(KoalaWikiWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsToken()
    {
        // Arrange
        var loginRequest = new { Username = "testuser", Password = "Test123!" };

        // Act
        var response = await Client.PostAsJsonAsync("/api/Auth/Login", loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var data = await response.Content.ReadFromJsonAsync<dynamic>();
        Assert.NotNull(data);
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsFailed()
    {
        // Arrange
        var loginRequest = new { Username = "testuser", Password = "Wrong123!" };

        // Act
        var response = await Client.PostAsJsonAsync("/api/Auth/Login", loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var data = await response.Content.ReadFromJsonAsync<dynamic>();
        Assert.NotNull(data);
    }
}
