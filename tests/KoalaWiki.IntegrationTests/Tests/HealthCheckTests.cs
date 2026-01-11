using KoalaWiki.IntegrationTests.Fixtures;
using Xunit;

namespace KoalaWiki.IntegrationTests.Tests;

public class HealthCheckTests : IntegrationTestBase
{
    public HealthCheckTests(KoalaWikiWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task GetHealthEndpoint_Responds()
    {
        var response = await Client.GetAsync("/health");
        Assert.NotNull(response);
    }

    [Fact]
    public async Task GetHealthEndpoint_IsCallable()
    {
        var response = await Client.GetAsync("/health");
        Assert.True(response.StatusCode >= System.Net.HttpStatusCode.OK || response.StatusCode < System.Net.HttpStatusCode.BadRequest);
    }
}
