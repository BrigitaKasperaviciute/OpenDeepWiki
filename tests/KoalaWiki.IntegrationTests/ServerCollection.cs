using Xunit;

namespace KoalaWiki.IntegrationTests;

[CollectionDefinition("Server collection")]
public class ServerCollection : ICollectionFixture<ServerFixture>
{
    // Shared server fixture for integration tests
}
