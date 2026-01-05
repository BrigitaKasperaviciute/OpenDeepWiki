using System.Net;

namespace KoalaWiki.IntegrationTests;

[Collection("Server collection")]
public class DocumentApiTests : AuthenticatedTestBase
{
    public DocumentApiTests(ServerFixture server) : base(server) { }
    [Fact]
    public async Task GetRepositoryList_ReturnsSuccessStatusCode()
    {
        // Arrange
        Assert.True(await IsServerRunningAsync(), "Server not running");
        
        // Login to access repository endpoints
        var loggedIn = await LoginAsTestUserAsync("admin", "admin");
        Assert.True(loggedIn, "Failed to login - test user may not exist");

        // Act - Use RepositoryList endpoint for repository/document listing
        var response = await Client.GetAsync("/api/Repository/RepositoryList?page=1&pageSize=10");

        // Assert - Authenticated request should succeed
        Assert.True(response.IsSuccessStatusCode,
            $"Authenticated request should succeed. Status: {response.StatusCode}");
    }

    [Fact]
    public async Task GetDocumentCatalogs_WithInvalidRepo_ReturnsNotFound()
    {
        // Arrange
        Assert.True(await IsServerRunningAsync(), "Server not running");
        
        var loggedIn = await LoginAsTestUserAsync("admin", "admin");
        Assert.True(loggedIn, "Failed to authenticate");

        // Act - Use DocumentCatalog endpoint with invalid repo
        var response = await Client.GetAsync("/api/DocumentCatalog/GetDocumentCatalogs?organizationName=invalid&name=invalid");

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

        // Act - Use RepositoryList endpoint
        var response = await Client.GetAsync("/api/Repository/RepositoryList?page=1&pageSize=10");

        // Assert - Authenticated request should succeed
        Assert.True(response.IsSuccessStatusCode,
            $"Authenticated request should succeed. Status: {response.StatusCode}");
    }

    [Theory]
    [InlineData(1, 10)]
    [InlineData(1, 20)]
    public async Task GetRepositoryList_WithPagination_ReturnsSuccess(int page, int pageSize)
    {
        // Arrange
        Assert.True(await IsServerRunningAsync(), "Server not running");
        
        var loggedIn = await LoginAsTestUserAsync("admin", "admin");
        Assert.True(loggedIn, "Failed to authenticate");

        // Act - Use RepositoryList with pagination
        var response = await Client.GetAsync($"/api/Repository/RepositoryList?page={page}&pageSize={pageSize}");

        // Assert - Authenticated request should succeed
        Assert.True(response.IsSuccessStatusCode,
            $"Authenticated request with page={page}, pageSize={pageSize} should succeed. Status: {response.StatusCode}");
    }
}
