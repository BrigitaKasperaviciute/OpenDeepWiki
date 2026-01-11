using System.Net;
using KoalaWiki.IntegrationTests.Fixtures;
using Xunit;

namespace KoalaWiki.IntegrationTests.Tests.DocumentCatalog;

public class DocumentCatalogTests : IntegrationTestBase
{
    public DocumentCatalogTests(KoalaWikiWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task GetDocumentCatalogs_WithParams_Responds()
    {
        var query = "?organizationName=test&name=repo&branch=main";
        var response = await Client.GetAsync($"/api/DocumentCatalog/GetDocumentCatalogs{query}");
        Assert.NotNull(response);
    }

    [Fact]
    public async Task GetDocumentCatalogs_MissingParams_Responds()
    {
        var response = await Client.GetAsync($"/api/DocumentCatalog/GetDocumentCatalogs?organizationName=test");
        Assert.NotNull(response);
    }
}
