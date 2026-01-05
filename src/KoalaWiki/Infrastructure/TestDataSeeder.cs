using KoalaWiki.Core.DataAccess;
using KoalaWiki.Domains.Users;

namespace KoalaWiki.Infrastructure;

/// <summary>
/// Seeds test data into the database for integration testing
/// </summary>
public static class TestDataSeeder
{
    /// <summary>
    /// Seed test user data if running in development or testing environment
    /// </summary>
    public static async Task SeedTestDataAsync(WebApplication app)
    {
        var environment = app.Services.GetRequiredService<IWebHostEnvironment>();
        
        // Only seed in development environment
        if (!environment.IsDevelopment())
        {
            return;
        }

        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        
        try
        {
            using (var scope = app.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<IKoalaWikiContext>();

                // Ensure database is created - cast to DbContext to access Database property
                if (dbContext is DbContext context)
                {
                    await context.Database.EnsureCreatedAsync();
                }

                logger.LogInformation("Starting test user seeding...");

                // Delete existing test users if they exist (for fresh testing)
                var existingUsers = dbContext.Users.Where(u => u.Name == "admin" || u.Name == "testuser").ToList();
                if (existingUsers.Any())
                {
                    logger.LogInformation($"Removing {existingUsers.Count} existing test users");
                    
                    // Remove associated user roles
                    var existingUserIds = existingUsers.Select(u => u.Id).ToList();
                    var existingUserRoles = dbContext.UserInRoles.Where(ur => existingUserIds.Contains(ur.UserId)).ToList();
                    if (existingUserRoles.Any())
                    {
                        dbContext.UserInRoles.RemoveRange(existingUserRoles);
                    }
                    
                    dbContext.Users.RemoveRange(existingUsers);
                    await dbContext.SaveChangesAsync();
                }

                // Ensure admin role exists
                var adminRole = await dbContext.Roles.FirstOrDefaultAsync(r => r.Name == "Admin");
                if (adminRole == null)
                {
                    adminRole = new Role
                    {
                        Id = Guid.NewGuid().ToString("N"),
                        Name = "Admin",
                        Description = "Administrator role",
                        CreatedAt = DateTime.UtcNow
                    };
                    dbContext.Roles.Add(adminRole);
                    await dbContext.SaveChangesAsync();
                    logger.LogInformation("Created Admin role");
                }

                // Ensure user role exists
                var userRole = await dbContext.Roles.FirstOrDefaultAsync(r => r.Name == "User");
                if (userRole == null)
                {
                    userRole = new Role
                    {
                        Id = Guid.NewGuid().ToString("N"),
                        Name = "User",
                        Description = "Regular user role",
                        CreatedAt = DateTime.UtcNow
                    };
                    dbContext.Roles.Add(userRole);
                    await dbContext.SaveChangesAsync();
                    logger.LogInformation("Created User role");
                }

                // Create test admin user
                var adminUser = new User
                {
                    Id = Guid.NewGuid().ToString("N"),
                    Name = "admin",
                    Email = "admin@test.com",
                    Password = "admin", // Stored as plain text for testing
                    Avatar = string.Empty,
                    CreatedAt = DateTime.UtcNow,
                    Bio = "Test admin user"
                };

                dbContext.Users.Add(adminUser);
                logger.LogInformation("Added admin user");

                // Assign admin role to admin user
                var adminUserRole = new UserInRole
                {
                    UserId = adminUser.Id,
                    RoleId = adminRole.Id
                };
                dbContext.UserInRoles.Add(adminUserRole);

                // Create test regular user
                var testUser = new User
                {
                    Id = Guid.NewGuid().ToString("N"),
                    Name = "testuser",
                    Email = "test@test.com",
                    Password = "testpass",
                    Avatar = string.Empty,
                    CreatedAt = DateTime.UtcNow,
                    Bio = "Test regular user"
                };

                dbContext.Users.Add(testUser);
                logger.LogInformation("Added testuser");

                // Assign user role to test user
                var testUserRole = new UserInRole
                {
                    UserId = testUser.Id,
                    RoleId = userRole.Id
                };
                dbContext.UserInRoles.Add(testUserRole);

                await dbContext.SaveChangesAsync();
                
                var userCount = dbContext.Users.Count();
                var roleCount = dbContext.Roles.Count();
                logger.LogInformation($"Test users seeded successfully. Total users: {userCount}, Total roles: {roleCount}");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to seed test data");
        }
    }
}
