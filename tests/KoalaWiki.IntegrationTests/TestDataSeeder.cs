using System.Net.Http.Json;

namespace KoalaWiki.IntegrationTests;

/// <summary>
/// Seeds test data into the database for integration testing
/// </summary>
public class TestDataSeeder
{
    private readonly HttpClient _client;

    public TestDataSeeder(HttpClient client)
    {
        _client = client;
    }

    /// <summary>
    /// Ensures test user exists in the database
    /// Creates admin user if it doesn't exist
    /// </summary>
    public async Task<bool> EnsureTestUserExistsAsync(
        string username = "admin",
        string password = "admin",
        string email = "admin@test.com")
    {
        try
        {
            // First, try to login to see if user exists
            var loginRequest = new { Username = username, Password = password };
            var loginResponse = await _client.PostAsJsonAsync("/api/Auth/Login", loginRequest);

            if (loginResponse.IsSuccessStatusCode)
            {
                // User exists and credentials are correct
                var responseContent = await loginResponse.Content.ReadAsStringAsync();
                var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var loginResult = System.Text.Json.JsonSerializer.Deserialize<LoginResponse>(responseContent, options);
                if (loginResult?.Success == true)
                {
                    return true;
                }
            }

            // User doesn't exist or credentials are wrong, try to register
            var registerRequest = new
            {
                UserName = username,  // Note: RegisterInput uses "UserName" not "Username"
                Password = password,
                Email = email
            };

            var registerResponse = await _client.PostAsJsonAsync("/api/Auth/Register", registerRequest);

            if (registerResponse.IsSuccessStatusCode)
            {
                // Successfully registered - check if it returns success in body
                var registerContent = await registerResponse.Content.ReadAsStringAsync();
                var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var registerResult = System.Text.Json.JsonSerializer.Deserialize<LoginResponse>(registerContent, options);
                return registerResult?.Success == true;
            }

            // Registration failed - user might already exist with different password
            // Try login one more time
            loginResponse = await _client.PostAsJsonAsync("/api/Auth/Login", loginRequest);
            if (loginResponse.IsSuccessStatusCode)
            {
                var loginContent = await loginResponse.Content.ReadAsStringAsync();
                var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var loginResult = System.Text.Json.JsonSerializer.Deserialize<LoginResponse>(loginContent, options);
                return loginResult?.Success == true;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Creates multiple test users for testing different scenarios
    /// </summary>
    public async Task<bool> SeedTestUsersAsync()
    {
        var users = new[]
        {
            new { Username = "admin", Password = "admin", Email = "admin@test.com" },
            new { Username = "testuser", Password = "testpass", Email = "test@test.com" },
            new { Username = "viewer", Password = "viewpass", Email = "viewer@test.com" }
        };

        var allSuccess = true;
        foreach (var user in users)
        {
            var success = await EnsureTestUserExistsAsync(user.Username, user.Password, user.Email);
            allSuccess = allSuccess && success;
        }

        return allSuccess;
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
