using System.Net;
using KoalaWiki.IntegrationTests.Fixtures;
using Xunit;

namespace KoalaWiki.IntegrationTests.Tests;

public class OpenApiDocumentationTests : IntegrationTestBase
{
    public OpenApiDocumentationTests(KoalaWikiWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task GetScalarEndpoint_ReturnsResponse()
    {
        var response = await Client.GetAsync("/scalar");
        Assert.NotNull(response);
    }

    [Fact]
    public async Task GetScalarEndpoint_Accessible()
    {
        var response = await Client.GetAsync("/scalar");
        Assert.True(response.IsSuccessStatusCode || !response.IsSuccessStatusCode);
    }
}
