using System.Net;
using KoalaWiki.IntegrationTests.Fixtures;
using Xunit;

namespace KoalaWiki.IntegrationTests.Tests.Auth;

public class CurrentUserTests : IntegrationTestBase
{
    public CurrentUserTests(KoalaWikiWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task GetCurrentUser_WithToken_Responds()
    {
        var client = await GetAuthenticatedClientAsync();
        var response = await client.GetAsync("/api/Auth/CurrentUser");
        Assert.NotNull(response);
    }

    [Fact]
    public async Task GetCurrentUser_WithoutToken_Responds()
    {
        var response = await Client.GetAsync("/api/Auth/CurrentUser");
        Assert.NotNull(response);
    }
}
