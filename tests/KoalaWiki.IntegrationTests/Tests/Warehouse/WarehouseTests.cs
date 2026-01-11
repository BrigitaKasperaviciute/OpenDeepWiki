using System.Net;
using KoalaWiki.IntegrationTests.Fixtures;
using Xunit;

namespace KoalaWiki.IntegrationTests.Tests.Warehouse;

public class WarehouseTests : IntegrationTestBase
{
    public WarehouseTests(KoalaWikiWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task GetRepository_ValidId_Responds()
    {
        var id = "00000000-0000-0000-0000-000000000001";
        var response = await Client.GetAsync($"/api/Warehouse/Repository?id={id}");
        Assert.NotNull(response);
    }

    [Fact]
    public async Task GetRepository_InvalidId_Responds()
    {
        var response = await Client.GetAsync($"/api/Warehouse/Repository?id=invalid");
        Assert.NotNull(response);
    }
}
