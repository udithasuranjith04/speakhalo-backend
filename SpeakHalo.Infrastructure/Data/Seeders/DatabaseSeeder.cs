using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace SpeakHalo.Infrastructure.Data.Seeders;

/// <summary>
/// Main database seeder that runs all seeders
/// </summary>
public static class DatabaseSeeder
{
    /// <summary>
    /// Run all database seeders
    /// </summary>
    public static async Task SeedAllAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();

        try
        {
            logger.LogInformation("Starting database seeding...");

            // Ensure database is created and migrations are applied
            await context.Database.MigrateAsync();

            await CookieManagementSeeder.SeedAsync(context);

            logger.LogInformation("Database seeding completed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding the database");
            throw;
        }
    }

    /// <summary>
    /// Run specific seeder
    /// </summary>
    public static async Task SeedCookieManagementAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await CookieManagementSeeder.SeedAsync(context);
    }
}
