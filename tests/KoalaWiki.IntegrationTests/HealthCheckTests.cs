namespace KoalaWiki.IntegrationTests;

[Collection("Server collection")]
public class HealthCheckTests : ApiTestBase
{
    public HealthCheckTests(ServerFixture server) : base(server) { }
    [Fact]
    public async Task Server_ShouldBeRunning()
    {
        // Arrange & Act
        var isRunning = await IsServerRunningAsync();

        // Assert
        Assert.True(isRunning, $"Server is not running at {BaseUrl}. Start the server with: dotnet run --project src/KoalaWiki/KoalaWiki.csproj");
    }

    [Fact]
    public async Task HealthCheck_ReturnsOk()
    {
        // Arrange
        Assert.True(await IsServerRunningAsync(), "Server not running");

        // Act
        var response = await Client.GetAsync("/health");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        Assert.NotEmpty(content);
    }

    [Fact]
    public async Task RootEndpoint_ReturnsSuccess()
    {
        // Arrange
        Assert.True(await IsServerRunningAsync(), "Server not running");

        // Act
        var response = await Client.GetAsync("/");

        // Assert
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task ApiScalar_ReturnsSuccess()
    {
        // Arrange
        Assert.True(await IsServerRunningAsync(), "Server not running");

        // Act
        var response = await Client.GetAsync("/scalar");

        // Assert
        response.EnsureSuccessStatusCode();
    }
}
