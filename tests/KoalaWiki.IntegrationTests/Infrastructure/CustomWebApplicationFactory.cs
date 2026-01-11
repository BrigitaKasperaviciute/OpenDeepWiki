using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using KoalaWiki.Core.DataAccess;
using KoalaWiki.Provider.Sqlite;
using System;
using System.Collections.Generic;
using System.Linq;

namespace KoalaWiki.IntegrationTests.Infrastructure;

/// <summary>
/// Custom WebApplicationFactory for integration testing
/// </summary>
/// <typeparam name="TProgram">The entry point type of the application</typeparam>
public class CustomWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Set testing environment BEFORE configuration runs
        builder.UseEnvironment("Testing");

        // Add test configuration BEFORE the application initializes
        builder.ConfigureAppConfiguration((context, config) =>
        {
            // Add in-memory configuration with high priority to override all other sources
            var testConfig = new Dictionary<string, string?>
            {
                // OpenAI Options - PascalCase versions (Program.cs Testing environment uses these)
                ["ChatModel"] = "test-model",
                ["AnalysisModel"] = "test-analysis",
                ["ChatApiKey"] = "test-api-key",
                ["Endpoint"] = "https://test-endpoint.com",

                // JWT Settings
                ["JwtSettings:Secret"] = "this-is-a-test-secret-key-for-jwt-token-generation-minimum-32-chars",
                ["JwtSettings:Issuer"] = "test-issuer",
                ["JwtSettings:Audience"] = "test-audience",
                ["JwtSettings:ExpireMinutes"] = "60"
            };

            // The Program.cs checks for Testing environment and skips OpenAIOptions.InitConfig validation
            // Instead it directly reads from configuration, so we provide the minimal required settings
            config.AddInMemoryCollection(testConfig);
        });

        // Configure logging to suppress informational messages
        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.SetMinimumLevel(LogLevel.Error);
        });

        builder.ConfigureTestServices(services =>
        {
            // Remove the existing DbContext and all related registrations
            services.RemoveAll<DbContextOptions<SqliteContext>>();
            services.RemoveAll<SqliteContext>();
            services.RemoveAll<IKoalaWikiContext>();

            // Add a fresh database context using an in-memory database for testing
            // Using a unique database name per test run to ensure isolation
            var dbName = $"InMemoryTestDb_{Guid.NewGuid()}";
            services.AddDbContext<SqliteContext>((serviceProvider, options) =>
            {
                options.UseInMemoryDatabase(dbName);
                // Suppress all EF Core logging and warnings
                options.ConfigureWarnings(warnings => warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.AmbientTransactionWarning));
                options.EnableSensitiveDataLogging(false);
                options.EnableDetailedErrors(false);
                // Don't use internal service provider to avoid conflicts
            }, ServiceLifetime.Scoped);

            services.AddScoped<IKoalaWikiContext>(provider =>
                provider.GetRequiredService<SqliteContext>());

            // Build the service provider
            var sp = services.BuildServiceProvider();

            // Create a scope to obtain a reference to the database context
            using (var scope = sp.CreateScope())
            {
                var scopedServices = scope.ServiceProvider;
                var db = scopedServices.GetRequiredService<SqliteContext>();

                // Ensure the database is created
                db.Database.EnsureCreated();

                // Seed the database with test data
                SeedDatabase(db);
            }
        });

        builder.UseEnvironment("Testing");
    }

    /// <summary>
    /// Seeds the database with initial test data
    /// </summary>
    /// <param name="context">The database context</param>
    private static void SeedDatabase(IKoalaWikiContext context)
    {
        // Seed test roles
        var adminRole = new KoalaWiki.Domains.Users.Role
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = "admin",
            Description = "Administrator role",
            CreatedAt = DateTime.UtcNow
        };

        var userRole = new KoalaWiki.Domains.Users.Role
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = "user",
            Description = "Regular user role",
            CreatedAt = DateTime.UtcNow
        };

        context.Roles.AddRange(adminRole, userRole);

        // Seed test users
        var testUser = new KoalaWiki.Domains.Users.User
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = "testuser",
            Email = "testuser@example.com",
            Password = "password123", // Note: In production, this should be hashed
            CreatedAt = DateTime.UtcNow,
            Avatar = "https://example.com/avatar.jpg"
        };

        var adminUser = new KoalaWiki.Domains.Users.User
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = "admin",
            Email = "admin@example.com",
            Password = "admin123",
            CreatedAt = DateTime.UtcNow
        };

        context.Users.AddRange(testUser, adminUser);

        // Assign roles to users
        context.UserInRoles.AddRange(
            new KoalaWiki.Domains.Users.UserInRole
            {
                UserId = testUser.Id,
                RoleId = userRole.Id
            },
            new KoalaWiki.Domains.Users.UserInRole
            {
                UserId = adminUser.Id,
                RoleId = adminRole.Id
            }
        );

        // Seed test warehouse
        var testWarehouse = new KoalaWiki.Domains.Warehouse.Warehouse
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = "test-repo",
            OrganizationName = "testorg",
            Address = "https://github.com/testorg/test-repo.git",
            Description = "Test repository",
            Branch = "main",
            Type = "git",
            Status = KoalaWiki.Domains.Warehouse.WarehouseStatus.Completed,
            CreatedAt = DateTime.UtcNow,
            UserId = testUser.Id,
            Stars = 100,
            Forks = 50
        };

        context.Warehouses.Add(testWarehouse);

        // Seed test document catalog
        var testCatalog = new KoalaWiki.Domains.DocumentFile.DocumentCatalog
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = "Getting Started",
            Url = "getting-started",
            WarehouseId = testWarehouse.Id,
            IsCompleted = true,
            IsDeleted = false,
            Order = 1,
            CreatedAt = DateTime.UtcNow,
            Description = "Getting started guide"
        };

        context.DocumentCatalogs.Add(testCatalog);

        ((DbContext)context).SaveChanges();
    }
}
