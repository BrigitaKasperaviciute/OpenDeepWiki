using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using KoalaWiki.Core.DataAccess;
using KoalaWiki.Domains.Users;
using KoalaWiki.Domains.Warehouse;

namespace KoalaWiki.IntegrationTests.Fixtures;

/// <summary>
/// Test-specific database context for integration tests
/// </summary>
public class TestKoalaWikiContext(DbContextOptions<TestKoalaWikiContext> options)
    : KoalaWikiContext<TestKoalaWikiContext>(options)
{
}

/// <summary>
/// Custom WebApplicationFactory for integration testing with in-memory database
/// </summary>
public class KoalaWikiWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Set environment variables before the host is built
        // This ensures they're available when Program.cs runs its static initialization
        Environment.SetEnvironmentVariable("CHAT_MODEL", "gpt-4o-mini");
        Environment.SetEnvironmentVariable("CHAT_API_KEY", "test-api-key");
        Environment.SetEnvironmentVariable("ENDPOINT", "https://api.openai.com");
        Environment.SetEnvironmentVariable("EMBEDDING_MODEL", "text-embedding-3-small");
        Environment.SetEnvironmentVariable("EMBEDDING_API_KEY", "test-api-key");
        Environment.SetEnvironmentVariable("EMBEDDING_ENDPOINT", "https://api.openai.com");
        Environment.SetEnvironmentVariable("MODEL_PROVIDER", "OpenAI");
        Environment.SetEnvironmentVariable("DEEP_RESEARCH_MODEL", "gpt-4o-mini");
        Environment.SetEnvironmentVariable("MAX_FILE_LIMIT", "10");
        Environment.SetEnvironmentVariable("ENABLE_MEM0", "false");
        Environment.SetEnvironmentVariable("JWT_SECRET", "test-jwt-secret-key-for-integration-tests-12345678");
        Environment.SetEnvironmentVariable("JWT_ISSUER", "test-issuer");
        Environment.SetEnvironmentVariable("JWT_AUDIENCE", "test-audience");
        Environment.SetEnvironmentVariable("JWT_EXPIRES", "60");
        Environment.SetEnvironmentVariable("SKIP_MIGRATIONS", "true");
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
        Environment.SetEnvironmentVariable("SUPPRESS_LOGGING", "true");

        // Configure logging to reduce noise in test output
        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.SetMinimumLevel(LogLevel.None); // Suppress all logs in tests
        });

        builder.ConfigureServices(services =>
        {
            // Remove background services to prevent TaskCanceledException spam in tests
            var hostedServices = services.Where(d => d.ServiceType == typeof(IHostedService)).ToList();
            foreach (var descriptor in hostedServices)
            {
                services.Remove(descriptor);
            }

            // Remove the existing database context registrations
            var descriptors = services.Where(
                d => d.ServiceType.IsGenericType &&
                     d.ServiceType.GetGenericTypeDefinition() == typeof(DbContextOptions<>) ||
                     d.ServiceType == typeof(IKoalaWikiContext)).ToList();

            foreach (var descriptor in descriptors)
            {
                services.Remove(descriptor);
            }

            // Add in-memory database for testing
            services.AddDbContext<TestKoalaWikiContext>(options =>
            {
                options.UseInMemoryDatabase("InMemoryKoalaWikiTest");
                options.EnableSensitiveDataLogging(false);
                options.EnableDetailedErrors(false);
            });

            // Register the test context as IKoalaWikiContext
            services.AddScoped<IKoalaWikiContext>(provider =>
                provider.GetRequiredService<TestKoalaWikiContext>());

            // Build the service provider
            var sp = services.BuildServiceProvider();

            // Create a scope to obtain a reference to the database context
            using var scope = sp.CreateScope();
            var scopedServices = scope.ServiceProvider;
            var db = scopedServices.GetRequiredService<IKoalaWikiContext>();

            // Seed test data
            SeedTestData(db);
        });

        builder.UseEnvironment("Testing");
    }

    private static void SeedTestData(IKoalaWikiContext context)
    {
        // Clear existing data
        context.Users.RemoveRange(context.Users);
        context.Roles.RemoveRange(context.Roles);
        context.Warehouses.RemoveRange(context.Warehouses);
        ((DbContext)context).SaveChanges();

        // Seed test users
        // Note: The AuthService compares passwords directly without BCrypt verification (line 51 in AuthService.cs)
        // So we store plain text passwords for testing. In production, this should use BCrypt.Verify()
        var testUser = new User
        {
            Id = Guid.NewGuid().ToString(),
            Name = "testuser",
            Email = "test@example.com",
            Password = "Test123!", // Plain text for testing due to AuthService implementation
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var adminUser = new User
        {
            Id = Guid.NewGuid().ToString(),
            Name = "admin",
            Email = "admin@example.com",
            Password = "Admin123!", // Plain text for testing due to AuthService implementation
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Users.AddRange(testUser, adminUser);

        // Seed roles
        var userRole = new Role
        {
            Id = Guid.NewGuid().ToString(),
            Name = "user",
            Description = "Standard user role",
            CreatedAt = DateTime.UtcNow
        };

        var adminRole = new Role
        {
            Id = Guid.NewGuid().ToString(),
            Name = "admin",
            Description = "Administrator role",
            CreatedAt = DateTime.UtcNow
        };

        context.Roles.AddRange(userRole, adminRole);

        // Seed user-role mappings
        context.UserInRoles.AddRange(
            new UserInRole
            {
                UserId = testUser.Id,
                RoleId = userRole.Id
            },
            new UserInRole
            {
                UserId = adminUser.Id,
                RoleId = adminRole.Id
            }
        );

        // Seed test warehouse/repository
        var testWarehouse = new KoalaWiki.Domains.Warehouse.Warehouse
        {
            Id = Guid.NewGuid().ToString(),
            Name = "test-repo",
            OrganizationName = "test-org",
            Address = "https://github.com/test-org/test-repo",
            Type = "Git",
            Branch = "main",
            Status = WarehouseStatus.Completed,
            Description = "Test repository for integration tests",
            CreatedAt = DateTime.UtcNow
        };

        context.Warehouses.Add(testWarehouse);

        // Cast to DbContext to access SaveChanges
        ((DbContext)context).SaveChanges();
    }
}
