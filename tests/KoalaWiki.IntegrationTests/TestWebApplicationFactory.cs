using System.Collections.Generic;
using System.Threading.Tasks;
using KoalaWiki.Core.DataAccess;
using KoalaWiki.Provider.Sqlite;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private SqliteConnection? _connection;

    public TestWebApplicationFactory()
    {
        // Ensure mandatory AI settings exist before Program runs its static init
        Environment.SetEnvironmentVariable("CHAT_MODEL", "test-model");
        Environment.SetEnvironmentVariable("CHAT_API_KEY", "test-api-key");
        Environment.SetEnvironmentVariable("ENDPOINT", "https://example.test");
        Environment.SetEnvironmentVariable("MODEL_PROVIDER", "OpenAI");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = "integration-tests-secret-key-12345678901234567890",
                ["Jwt:Issuer"] = "KoalaWiki.Tests",
                ["Jwt:Audience"] = "KoalaWiki.Tests",
                ["ConnectionStrings:Default"] = "DataSource=:memory:",
                ["CHAT_MODEL"] = "test-model",
                ["CHAT_API_KEY"] = "test-api-key",
                ["ENDPOINT"] = "https://example.test",
                ["MODEL_PROVIDER"] = "OpenAI"
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll(typeof(IKoalaWikiContext));
            services.RemoveAll(typeof(DbContextOptions<SqliteContext>));
            services.RemoveAll(typeof(IHostedService));

            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            services.AddDbContext<IKoalaWikiContext, SqliteContext>((_, options) =>
            {
                options.UseSqlite(_connection);
            });

            services.AddDbContext<SqliteContext>((_, options) =>
            {
                options.UseSqlite(_connection);
            });
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SqliteContext>();
        db.Database.EnsureCreated();

        return host;
    }

    public async Task ResetAndSeedAsync(Func<SqliteContext, Task> seed)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SqliteContext>();

        // Fast clear: just delete data, not schema
        await ClearDatabaseAsync(db);

        if (seed != null)
        {
            await seed(db);
            await db.SaveChangesAsync();
        }
    }

    private async Task ClearDatabaseAsync(SqliteContext db)
    {
        // Clear data without dropping/recreating schema
        await db.Database.ExecuteSqlRawAsync("DELETE FROM DocumentCommitRecords");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM DocumentCatalogs");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM DocumentFileItems");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM Documents");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM WarehouseInRoles");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM Warehouses");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM UserInRoles");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM Users");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM Roles");
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _connection?.Dispose();
    }
}
