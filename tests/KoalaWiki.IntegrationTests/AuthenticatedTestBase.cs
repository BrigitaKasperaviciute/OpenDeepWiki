using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace KoalaWiki.IntegrationTests;

/// <summary>
/// Base class for tests that require authentication
/// </summary>
public abstract class AuthenticatedTestBase : ApiTestBase
{
    protected string? AuthToken { get; private set; }
    protected bool IsAuthenticated { get; private set; }
    private static bool _testUserSeeded = false;
    private static readonly SemaphoreSlim _seedLock = new(1, 1);

    protected AuthenticatedTestBase(ServerFixture server) : base(server)
    {
    }

    /// <summary>
    /// Ensures test user exists in database before running tests
    /// </summary>
    protected async Task<bool> EnsureTestUserExistsAsync(string username = "admin", string password = "admin")
    {
        if (_testUserSeeded)
        {
            return true;
        }

        await _seedLock.WaitAsync();
        try
        {
            if (_testUserSeeded)
            {
                return true;
            }

            var loginCheck = await PerformLoginAsync(username, password);
            if (loginCheck.Success)
            {
                _testUserSeeded = true;
                return true;
            }

            // If login failed, try register (best effort; 400/409 likely means user already exists)
            var registerRequest = new { UserName = username, Password = password, Email = $"{username}@test.com" };
            var registerResponse = await Client.PostAsJsonAsync("/api/Auth/Register", registerRequest);
            var registerBody = await registerResponse.Content.ReadAsStringAsync();
            Console.WriteLine($"Register attempt status: {registerResponse.StatusCode}\nBody: {registerBody}");

            if (registerResponse.IsSuccessStatusCode ||
                registerResponse.StatusCode == HttpStatusCode.Conflict ||
                registerResponse.StatusCode == HttpStatusCode.BadRequest)
            {
                _testUserSeeded = true;
                return true;
            }

            return false;
        }
        finally
        {
            _seedLock.Release();
        }
    }

    /// <summary>
    /// Login with test credentials and store authentication token
    /// </summary>
    protected async Task<bool> LoginAsTestUserAsync(string username = "admin", string password = "admin")
    {
        try
        {
            var ensured = await EnsureTestUserExistsAsync(username, password);
            if (!ensured)
            {
                return false;
            }

            var login = await PerformLoginAsync(username, password);
            if (!login.Success || string.IsNullOrWhiteSpace(login.Response?.Token))
            {
                return false;
            }

            AuthToken = login.Response.Token;
            IsAuthenticated = true;

            // Set Authorization header for subsequent requests
            Client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", AuthToken);

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Login exception: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Clear authentication state
    /// </summary>
    protected void Logout()
    {
        AuthToken = null;
        IsAuthenticated = false;
        Client.DefaultRequestHeaders.Authorization = null;
    }

    private async Task<(bool Success, LoginResponse? Response)> PerformLoginAsync(string username, string password)
    {
        try
        {
            var loginRequest = new { Username = username, Password = password };
            var response = await Client.PostAsJsonAsync("/api/Auth/Login", loginRequest);
            var responseBody = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Login request failed with status: {response.StatusCode}\nBody: {responseBody}");
                return (false, null);
            }

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            LoginResponse? loginResponse = null;
            try
            {
                using var doc = JsonDocument.Parse(responseBody);
                var payload = doc.RootElement;
                if (payload.TryGetProperty("data", out var dataElement))
                {
                    payload = dataElement;
                }

                loginResponse = JsonSerializer.Deserialize<LoginResponse>(payload.GetRawText(), options);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to parse login response: {ex.Message}\nBody: {responseBody}");
            }

            var success = loginResponse?.Success == true && !string.IsNullOrWhiteSpace(loginResponse.Token);

            if (!success)
            {
                Console.WriteLine($"Login response did not indicate success. Body: {responseBody}");
            }

            return (success, loginResponse);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Login request exception: {ex.Message}");
            return (false, null);
        }
    }

    /// <summary>
    /// Login response DTO matching the API response
    /// </summary>
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
