using System.Net;

namespace KoalaWiki.IntegrationTests;

[Collection("Server collection")]
public class WarehouseApiTests : AuthenticatedTestBase
{
    public WarehouseApiTests(ServerFixture server) : base(server) { }
    [Fact]
    public async Task GetWarehouses_ReturnsSuccessStatusCode()
    {
        // Arrange
        Assert.True(await IsServerRunningAsync(), "Server not running");
        
        // Login as admin to access repository endpoints
        var loggedIn = await LoginAsTestUserAsync("admin", "admin");
        Assert.True(loggedIn, "Failed to login - test user may not exist");

        // Act
        var response = await Client.GetAsync("/api/Repository/RepositoryList?page=1&pageSize=10");

        // Assert - Authenticated admin should successfully access repository list
        Assert.True(response.IsSuccessStatusCode,
            $"Authenticated user should access repository list. Status: {response.StatusCode}");
    }

    [Fact]
    public async Task GetWarehouseById_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        Assert.True(await IsServerRunningAsync(), "Server not running");
        
        var loggedIn = await LoginAsTestUserAsync("admin", "admin");
        Assert.True(loggedIn, "Failed to authenticate");

        // Act
        var response = await Client.GetAsync("/api/Warehouse/Repository?organizationName=invalid&name=invalid");

        // Assert - Invalid repository should return 404
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetRepositoryStats_ReturnsSuccess()
    {
        // Arrange
        Assert.True(await IsServerRunningAsync(), "Server not running");
        
        var loggedIn = await LoginAsTestUserAsync("admin", "admin");
        Assert.True(loggedIn, "Failed to authenticate");

        // Act - Testing repository list endpoint
        var response = await Client.GetAsync("/api/Repository/RepositoryList?page=1&pageSize=1");

        // Assert - Authenticated request should succeed
        Assert.True(response.IsSuccessStatusCode,
            $"Authenticated request should succeed. Status: {response.StatusCode}");
    }
}
