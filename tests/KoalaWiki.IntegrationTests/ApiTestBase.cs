namespace KoalaWiki.IntegrationTests;

/// <summary>
/// Base class for API integration tests
/// Tests connect to a running instance of the application
/// Start the application before running tests: dotnet run --project src/KoalaWiki/KoalaWiki.csproj
/// </summary>
public abstract class ApiTestBase : IDisposable
{
    protected readonly HttpClient Client;
    protected readonly string BaseUrl;

    protected ApiTestBase(ServerFixture server)
    {
        BaseUrl = server.BaseUrl;
        Client = new HttpClient
        {
            BaseAddress = new Uri(BaseUrl),
            Timeout = TimeSpan.FromSeconds(30)
        };
    }

    protected async Task<bool> IsServerRunningAsync()
    {
        try
        {
            using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(5));
            var response = await Client.GetAsync("/", cts.Token);
            return response.IsSuccessStatusCode;
        }
        catch (System.Threading.Tasks.TaskCanceledException)
        {
            // Timeout or cancellation means server isn't responding fast enough
            return false;
        }
        catch
        {
            // Any other exception means server isn't running
            return false;
        }
    }

    public void Dispose()
    {
        Client?.Dispose();
        GC.SuppressFinalize(this);
    }
}
