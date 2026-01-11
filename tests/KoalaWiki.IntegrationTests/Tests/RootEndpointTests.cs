using System.Net;
using KoalaWiki.IntegrationTests.Fixtures;
using Xunit;

namespace KoalaWiki.IntegrationTests.Tests;

public class RootEndpointTests : IntegrationTestBase
{
    public RootEndpointTests(KoalaWikiWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task GetRootEndpoint_Returns200()
    {
        var response = await Client.GetAsync("/");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetRootEndpoint_Responds()
    {
        var response = await Client.GetAsync("/");
        Assert.NotNull(response);
    }
}
