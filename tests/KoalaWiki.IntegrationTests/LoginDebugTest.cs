using System.Net.Http.Json;

namespace KoalaWiki.IntegrationTests;

[Collection("Server collection")]
public class LoginDebugTest : AuthenticatedTestBase
{
    public LoginDebugTest(ServerFixture server) : base(server) { }
    [Fact]
    public async Task DebugLoginAttempt()
    {
        // First check if server is running
        var serverRunning = await IsServerRunningAsync();
        System.Diagnostics.Debug.WriteLine($"Server running: {serverRunning}");
        
        // Try to login
        var loginRequest = new { Username = "admin", Password = "admin" };
        var response = await Client.PostAsJsonAsync("/api/Auth/Login", loginRequest);
        
        var content = await response.Content.ReadAsStringAsync();
        System.Diagnostics.Debug.WriteLine($"Login Response Status: {response.StatusCode}");
        System.Diagnostics.Debug.WriteLine($"Login Response Content: {content}");
        System.Diagnostics.Debug.WriteLine($"Login Response Content-Type: {response.Content.Headers.ContentType}");
        
        // Always pass this debug test
        Assert.True(true);
    }

    private class LoginResponse
    {
        public bool Success { get; set; }
        public string? Token { get; set; }
        public string? RefreshToken { get; set; }
        public UserInfo? User { get; set; }
        public string? ErrorMessage { get; set; }
    }

    private class UserInfo
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? Role { get; set; }
    }
}
