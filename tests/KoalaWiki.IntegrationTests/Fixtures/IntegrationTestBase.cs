using System.Net.Http.Headers;
using System.Net.Http.Json;
using Xunit;

namespace KoalaWiki.IntegrationTests.Fixtures;

/// <summary>
/// Base class for integration tests providing common test utilities
/// </summary>
public abstract class IntegrationTestBase : IClassFixture<KoalaWikiWebApplicationFactory>
{
    protected readonly HttpClient Client;
    protected readonly KoalaWikiWebApplicationFactory Factory;

    protected IntegrationTestBase(KoalaWikiWebApplicationFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient();
    }

    /// <summary>
    /// Authenticates a user and returns the JWT token
    /// </summary>
    protected async Task<string> AuthenticateAsync(string username, string password)
    {
        var loginRequest = new
        {
            Username = username,
            Password = password
        };

        var response = await Client.PostAsJsonAsync("/api/Auth/Login", loginRequest);
        response.EnsureSuccessStatusCode();

        // Response is wrapped by ResultFilter: { code: 200, data: {...} }
        var wrappedResponse = await response.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();

        if (wrappedResponse?.Data?.Token == null)
        {
            throw new InvalidOperationException("Failed to retrieve token from login response");
        }

        return wrappedResponse.Data.Token;
    }

    /// <summary>
    /// Creates an authenticated HTTP client with JWT token
    /// </summary>
    protected async Task<HttpClient> GetAuthenticatedClientAsync(string username = "testuser", string password = "Test123!")
    {
        var client = Factory.CreateClient();
        var token = await AuthenticateAsync(username, password);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    /// <summary>
    /// API response wrapper used by ResultFilter
    /// </summary>
    protected class ApiResponse<T>
    {
        public int Code { get; set; }
        public T? Data { get; set; }
    }

    /// <summary>
    /// Helper class for deserializing login responses
    /// </summary>
    protected class LoginResponse
    {
        public bool Success { get; set; }
        public string? Token { get; set; }
        public string? RefreshToken { get; set; }
        public string? ErrorMessage { get; set; }
        public UserInfo? User { get; set; }
    }

    /// <summary>
    /// Helper class for user info in login response
    /// </summary>
    protected class UserInfo
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? Email { get; set; }
    }
}
