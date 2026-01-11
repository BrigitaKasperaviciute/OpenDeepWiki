using System.Net;
using System.Net.Http.Json;
using KoalaWiki.IntegrationTests.Fixtures;
using Xunit;

namespace KoalaWiki.IntegrationTests.Tests.Repository;

public class RepositoryListTests : IntegrationTestBase
{
    public RepositoryListTests(KoalaWikiWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task GetRepositoryList_Authenticated_Responds()
    {
        var client = await GetAuthenticatedClientAsync();
        var response = await client.GetAsync("/api/Repository/RepositoryList?page=1&pageSize=10");
        Assert.NotNull(response);
    }

    [Fact]
    public async Task GetRepositoryList_WithPagination_Responds()
    {
        var client = await GetAuthenticatedClientAsync();
        var response = await client.GetAsync("/api/Repository/RepositoryList?page=2&pageSize=5");
        Assert.NotNull(response);
    }
}
