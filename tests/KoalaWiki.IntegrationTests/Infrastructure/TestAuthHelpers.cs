using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace KoalaWiki.IntegrationTests.Infrastructure;

/// <summary>
/// Helper class for authentication-related test utilities
/// </summary>
public static class TestAuthHelpers
{
    private const string TestJwtSecret = "KoalaWiki-Giasdh&*(YGV%%GR$RFGI(UH*GA*^&%A^&%$GIOBOHNFG)A_)_-9as0djoinoJKGBHGVYGFYT%%%$FFGAO))&%+_";
    private const string TestIssuer = "KoalaWiki";
    private const string TestAudience = "KoalaWiki";

    /// <summary>
    /// Generates a JWT token for testing with the specified user information
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="username">Username</param>
    /// <param name="email">User email</param>
    /// <param name="role">User role</param>
    /// <returns>JWT token string</returns>
    public static string GenerateTestJwtToken(string userId, string username, string email, string role = "user")
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Role, role)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestJwtSecret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.Now.AddHours(1);

        var token = new JwtSecurityToken(
            issuer: TestIssuer,
            audience: TestAudience,
            claims: claims,
            expires: expires,
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Adds authentication header to the HTTP client
    /// </summary>
    /// <param name="client">HTTP client</param>
    /// <param name="token">JWT token</param>
    public static void AddAuthenticationHeader(HttpClient client, string token)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    /// <summary>
    /// Creates an authenticated HTTP client for testing
    /// </summary>
    /// <param name="factory">Web application factory</param>
    /// <param name="userId">User ID</param>
    /// <param name="username">Username</param>
    /// <param name="email">User email</param>
    /// <param name="role">User role</param>
    /// <returns>Authenticated HTTP client</returns>
    public static HttpClient CreateAuthenticatedClient<TProgram>(
        CustomWebApplicationFactory<TProgram> factory,
        string userId,
        string username,
        string email,
        string role = "user") where TProgram : class
    {
        var client = factory.CreateClient();
        var token = GenerateTestJwtToken(userId, username, email, role);
        AddAuthenticationHeader(client, token);
        return client;
    }
}
