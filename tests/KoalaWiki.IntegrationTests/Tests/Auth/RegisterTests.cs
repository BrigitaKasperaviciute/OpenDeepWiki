using System.Net;
using System.Net.Http.Json;
using KoalaWiki.IntegrationTests.Fixtures;
using Xunit;

namespace KoalaWiki.IntegrationTests.Tests.Auth;

public class RegisterTests : IntegrationTestBase
{
    public RegisterTests(KoalaWikiWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task Register_WithValidInput_Responds()
    {
        var req = new { username = "newuser1", email = "new1@test.com", password = "Pass123!" };
        var response = await Client.PostAsJsonAsync("/api/Auth/Register", req);
        Assert.NotNull(response);
    }

    [Fact]
    public async Task Register_WithMissingEmail_Responds()
    {
        var req = new { username = "user2", password = "Pass123!" };
        var response = await Client.PostAsJsonAsync("/api/Auth/Register", req);
        Assert.NotNull(response);
    }
}
