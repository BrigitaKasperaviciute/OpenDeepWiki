using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Linq;
using System.Threading.Tasks;
using KoalaWiki.Domains.Users;
using KoalaWiki.Domains.Warehouse;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

public class ApiIntegrationTests : IClassFixture<TestWebApplicationFactory>, IAsyncLifetime
{
    private readonly TestWebApplicationFactory _factory;
    private HttpClient _client;

    public ApiIntegrationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    public async Task InitializeAsync()
    {
        await _factory.ResetAndSeedAsync(async db =>
        {
            var adminRole = new Role
            {
                Id = "role-admin",
                Name = "admin",
                Description = "Administrator",
                CreatedAt = DateTime.UtcNow
            };

            var userRole = new Role
            {
                Id = "role-user",
                Name = "user",
                Description = "User",
                CreatedAt = DateTime.UtcNow
            };

            var adminUser = new User
            {
                Id = "user-admin",
                Name = "admin",
                Email = "admin@example.com",
                Password = "AdminPass123!",
                CreatedAt = DateTime.UtcNow
            };

            var normalUser = new User
            {
                Id = "user-normal",
                Name = "user",
                Email = "user@example.com",
                Password = "UserPass123!",
                CreatedAt = DateTime.UtcNow
            };

            db.Roles.AddRange(adminRole, userRole);
            db.Users.AddRange(adminUser, normalUser);
            db.UserInRoles.AddRange(
                new UserInRole { UserId = adminUser.Id, RoleId = adminRole.Id },
                new UserInRole { UserId = normalUser.Id, RoleId = userRole.Id }
            );

            var now = DateTime.UtcNow;

            db.Warehouses.AddRange(
                new Warehouse
                {
                    Id = "wh-1",
                    OrganizationName = "OrgA",
                    Name = "RepoOne",
                    Description = "First repo",
                    Address = "https://example.com/OrgA/RepoOne.git",
                    Type = "git",
                    Branch = "main",
                    Status = WarehouseStatus.Completed,
                    CreatedAt = now,
                    IsRecommended = true,
                    Stars = 2,
                    Forks = 1
                },
                new Warehouse
                {
                    Id = "wh-2",
                    OrganizationName = "OrgA",
                    Name = "RepoTwo",
                    Description = "Second repo",
                    Address = "https://example.com/OrgA/RepoTwo.git",
                    Type = "git",
                    Branch = "main",
                    Status = WarehouseStatus.Processing,
                    CreatedAt = now.AddMinutes(-5),
                    Stars = 0,
                    Forks = 0
                },
                new Warehouse
                {
                    Id = "wh-3",
                    OrganizationName = "OrgB",
                    Name = "RepoThree",
                    Description = "Third repo",
                    Address = "https://example.com/OrgB/RepoThree.git",
                    Type = "git",
                    Branch = "dev",
                    Status = WarehouseStatus.Completed,
                    CreatedAt = now.AddMinutes(-10),
                    Stars = 3,
                    Forks = 1
                });

            await Task.CompletedTask;
        });
    }

    public Task DisposeAsync()
    {
        _client.Dispose();
        return Task.CompletedTask;
    }

    private static JsonElement GetData(JsonElement json) => json.GetProperty("data");

    private async Task<string> LoginAndGetTokenAsync(string username, string password)
    {
        var response = await _client.PostAsJsonAsync("/api/Auth/Login", new { username, password });
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        var data = GetData(payload);
        return data.GetProperty("token").GetString() ?? string.Empty;
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsTokenAndUser()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        var response = await _client.PostAsJsonAsync("/api/Auth/Login", new { username = "admin@example.com", password = "AdminPass123!" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        var data = GetData(payload);

        Assert.True(data.GetProperty("success").GetBoolean());
        Assert.False(string.IsNullOrWhiteSpace(data.GetProperty("token").GetString()));
        Assert.Equal("admin@example.com", data.GetProperty("user").GetProperty("email").GetString());
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ReturnsFailure()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        var response = await _client.PostAsJsonAsync("/api/Auth/Login", new { username = "admin@example.com", password = "wrong" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        var data = GetData(payload);

        Assert.False(data.GetProperty("success").GetBoolean());
        Assert.True(string.IsNullOrWhiteSpace(data.GetProperty("token").GetString()));
        Assert.Contains("密码", data.GetProperty("errorMessage").GetString());
    }

    [Fact]
    public async Task Profile_WithAuthorization_ReturnsUserProfile()
    {
        var token = await LoginAndGetTokenAsync("admin@example.com", "AdminPass123!");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/user/profile");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        var inner = GetData(payload);

        // ResultDto wrapper inside the ResultFilter
        Assert.Equal(200, inner.GetProperty("code").GetInt32());
        var user = inner.GetProperty("data");
        Assert.Equal("admin@example.com", user.GetProperty("email").GetString());
        Assert.Equal("admin", user.GetProperty("name").GetString());
    }

    [Fact]
    public async Task Profile_WithoutAuthorization_ReturnsUnauthorized()
    {
        using var unauthClient = _factory.CreateClient();
        var response = await unauthClient.GetAsync("/api/user/profile");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UserList_WithAdminRole_ReturnsPagedUsers()
    {
        var token = await LoginAndGetTokenAsync("admin@example.com", "AdminPass123!");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/user/UserList?page=1&pageSize=10");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        var data = GetData(payload);

        Assert.True(data.GetProperty("total").GetInt32() >= 2);
        var items = data.GetProperty("items").EnumerateArray().ToList();
        Assert.NotEmpty(items);
        Assert.Contains(items, item => item.GetProperty("email").GetString() == "admin@example.com");
    }

    [Fact]
    public async Task Health_Head_ReturnsOk()
    {
        var request = new HttpRequestMessage(HttpMethod.Head, "/health");
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Health_Get_ReturnsOk()
    {
        var response = await _client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Root_ReturnsOk()
    {
        var response = await _client.GetAsync("/");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Scalar_ReturnsOk()
    {
        var response = await _client.GetAsync("/scalar");
        Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Found);
    }

    [Fact]
    public async Task RepositoryList_ReturnsPagedRepositories()
    {
        var token = await LoginAndGetTokenAsync("admin@example.com", "AdminPass123!");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.GetAsync("/api/Repository/RepositoryList?page=1&pageSize=2");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        var data = GetData(payload);

        Assert.Equal(3, data.GetProperty("total").GetInt32());
        var items = data.GetProperty("items").EnumerateArray().ToList();
        Assert.True(items.Count <= 2);
    }

    [Fact]
    public async Task RepositoryList_Page10_ReturnsSuccess()
    {
        var token = await LoginAndGetTokenAsync("admin@example.com", "AdminPass123!");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/Repository/RepositoryList?page=1&pageSize=10");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        var data = GetData(payload);

        Assert.Equal(3, data.GetProperty("total").GetInt32());
        Assert.True(data.GetProperty("items").EnumerateArray().Count() <= 10);
    }

    [Fact]
    public async Task RepositoryList_Page20_ReturnsSuccess()
    {
        var token = await LoginAndGetTokenAsync("admin@example.com", "AdminPass123!");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/Repository/RepositoryList?page=1&pageSize=20");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        var data = GetData(payload);

        Assert.Equal(3, data.GetProperty("total").GetInt32());
        Assert.True(data.GetProperty("items").EnumerateArray().Count() <= 20);
    }

    [Fact]
    public async Task RepositoryStats_ReturnsSuccess()
    {
        var token = await LoginAndGetTokenAsync("admin@example.com", "AdminPass123!");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/Repository/RepositoryStats?id=wh-1");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        var data = GetData(payload);

        Assert.Equal(0, data.GetProperty("totalDocuments").GetInt32());
        Assert.Equal("Completed", data.GetProperty("processingStatus").GetString());
    }

    [Fact]
    public async Task RepositoryByOwnerAndName_Valid_ReturnsDetails()
    {
        var token = await LoginAndGetTokenAsync("admin@example.com", "AdminPass123!");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/Repository/RepositoryByOwnerAndName?owner=OrgA&name=RepoOne");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        var data = GetData(payload);

        Assert.Equal("RepoOne", data.GetProperty("name").GetString());
        Assert.Equal("OrgA", data.GetProperty("organizationName").GetString());
    }

    [Fact]
    public async Task RepositoryByOwnerAndName_Invalid_ReturnsError()
    {
        var token = await LoginAndGetTokenAsync("admin@example.com", "AdminPass123!");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/Repository/RepositoryByOwnerAndName?owner=missing&name=missing");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [Fact]
    public async Task RepositoryById_Valid_ReturnsSuccess()
    {
        var token = await LoginAndGetTokenAsync("admin@example.com", "AdminPass123!");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/Repository?id=wh-1");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var contentType = response.Content.Headers.ContentType?.MediaType ?? string.Empty;
        var body = await response.Content.ReadAsStringAsync();

        if (contentType.Contains("json", StringComparison.OrdinalIgnoreCase))
        {
            var payload = JsonSerializer.Deserialize<JsonElement>(body);
            var data = GetData(payload);
            Assert.Equal("wh-1", data.GetProperty("id").GetString());
            Assert.Equal("RepoOne", data.GetProperty("name").GetString());
        }
        else
        {
            Assert.False(string.IsNullOrWhiteSpace(body));
        }
    }

    [Fact]
    public async Task DocumentCatalog_InvalidRepository_ReturnsError()
    {
        var response = await _client.GetAsync("/api/DocumentCatalog/GetDocumentCatalogs?organizationName=missing&name=missing&branch=");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Warehouse_InvalidWarehouse_ReturnsError()
    {
        var response = await _client.GetAsync("/api/Warehouse/GetLastWarehouse?address=https://example.com/unknown.git");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

}
