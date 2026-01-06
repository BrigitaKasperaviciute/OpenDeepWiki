using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Text;

namespace KoalaWiki.IntegrationTests;

public sealed class ServerFixture : IAsyncLifetime, IDisposable
{
    private Process? _serverProcess;
    private readonly string _baseUrl;
    private readonly StringBuilder _stdout = new();
    private readonly StringBuilder _stderr = new();

    public string BaseUrl => _baseUrl;

    public ServerFixture()
    {
        var configuredUrl = Environment.GetEnvironmentVariable("KOALAWIKI_TEST_URL");
        _baseUrl = string.IsNullOrWhiteSpace(configuredUrl)
            ? $"http://127.0.0.1:{GetFreeTcpPort()}"
            : configuredUrl;
    }

    public async Task InitializeAsync()
    {
        if (_serverProcess != null && !_serverProcess.HasExited)
        {
            return;
        }

        // Start the API server
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = "run --project src/KoalaWiki/KoalaWiki.csproj --urls " + _baseUrl,
            WorkingDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..")),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        // Ensure we run in Development so the test data seeder executes
        startInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
        startInfo.Environment["DOTNET_ENVIRONMENT"] = "Development";
        startInfo.Environment["ASPNETCORE_URLS"] = _baseUrl;

        // Force tests to use an isolated SQLite database to avoid external dependencies
        var dbPath = Path.Combine(startInfo.WorkingDirectory, "koalawiki-tests.db");
        startInfo.Environment["DB_TYPE"] = "sqlite";
        startInfo.Environment["DB_CONNECTION_STRING"] = $"Data Source={dbPath}";

        // Set minimal AI configuration for tests (required by OpenAIOptions.InitConfig)
        startInfo.Environment["CHAT_MODEL"] = Environment.GetEnvironmentVariable("CHAT_MODEL") ?? "gpt-4o-mini";
        startInfo.Environment["CHAT_API_KEY"] = Environment.GetEnvironmentVariable("CHAT_API_KEY") ?? "test-key";
        startInfo.Environment["ENDPOINT"] = Environment.GetEnvironmentVariable("ENDPOINT") ?? "https://api.openai.com/v1";

        _stdout.Clear();
        _stderr.Clear();

        _serverProcess = Process.Start(startInfo);

        if (_serverProcess == null)
        {
            throw new InvalidOperationException("Failed to start KoalaWiki process for integration tests.");
        }

        _serverProcess.OutputDataReceived += (_, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                _stdout.AppendLine(e.Data);
            }
        };
        _serverProcess.ErrorDataReceived += (_, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                _stderr.AppendLine(e.Data);
            }
        };

        _serverProcess.BeginOutputReadLine();
        _serverProcess.BeginErrorReadLine();

        // Wait for readiness by polling the root endpoint
        using var client = new HttpClient { BaseAddress = new Uri(_baseUrl), Timeout = TimeSpan.FromSeconds(3) };
        var started = false;
        for (var i = 0; i < 20 && !started; i++)
        {
            try
            {
                var response = await client.GetAsync("/");
                started = response.IsSuccessStatusCode;
            }
            catch
            {
                // ignore and retry
            }

            if (_serverProcess.HasExited)
            {
                break;
            }
            if (!started)
            {
                await Task.Delay(500);
            }
        }

        if (!started)
        {
            try
            {
                if (_serverProcess is { HasExited: false })
                {
                    _serverProcess.Kill(true);
                    _serverProcess.WaitForExit(2000);
                }
            }
            catch
            {
                // ignore
            }

            var stdout = _stdout.ToString();
            var stderr = _stderr.ToString();
            var exitCode = _serverProcess?.HasExited == true ? _serverProcess.ExitCode : (int?)null;
            throw new InvalidOperationException($"KoalaWiki server failed to start for integration tests. ExitCode: {exitCode}\nSTDOUT:\n{stdout}\nSTDERR:\n{stderr}");
        }
    }

    public Task DisposeAsync()
    {
        Dispose();
        return Task.CompletedTask;
    }

    private static int GetFreeTcpPort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    public void Dispose()
    {
        try
        {
            if (_serverProcess is { HasExited: false })
            {
                _serverProcess.Kill(true);
                _serverProcess.WaitForExit(2000);
            }
        }
        catch
        {
            // swallow
        }

        _serverProcess?.Dispose();
    }
}
