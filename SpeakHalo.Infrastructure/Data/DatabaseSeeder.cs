using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SpeakHalo.Domain.Entities;
using SpeakHalo.Domain.Enums;

#pragma warning disable CS0618

namespace SpeakHalo.Infrastructure.Data;

public class DatabaseSeeder
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(ApplicationDbContext context, ILogger<DatabaseSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        try
        {
            _logger.LogInformation("Starting database seeding...");

            await SeedAdminUserAsync();
            await SeedFeatureFlagsAsync();

            await _context.SaveChangesAsync();

            _logger.LogInformation("Database seeding completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during database seeding");
            throw;
        }
    }

    private async Task SeedAdminUserAsync()
    {
        if (await _context.Users.AnyAsync())
        {
            _logger.LogInformation("Users already exist, skipping seed");
            return;
        }

        var adminPassword = Environment.GetEnvironmentVariable("SPEAKHALO_ADMIN_SEED_PASSWORD");
        if (string.IsNullOrEmpty(adminPassword))
        {
            _logger.LogWarning("SPEAKHALO_ADMIN_SEED_PASSWORD not set, skipping admin user seed. Set this environment variable to seed the initial admin user.");
            return;
        }

        var adminUser = new User
        {
            Name = "Admin User",
            Email = "admin@speakhalo.com",
            Role = UserRole.Admin,
            IsActive = true,
            BusinessName = "SpeakHalo Admin",
            CreatedAt = DateTime.UtcNow
        };
        await _context.Users.AddAsync(adminUser);
        await _context.SaveChangesAsync();

        var adminAuth = new UserAuth
        {
            UserId = adminUser.Id,
            HashedPassword = BCrypt.Net.BCrypt.HashPassword(adminPassword, workFactor: 12),
            PasswordMustChange = true,
            EmailVerified = true
        };
        await _context.Set<UserAuth>().AddAsync(adminAuth);

        var adminSubscription = new UserSubscription
        {
            UserId = adminUser.Id,
            Plan = "business",
            HasActivePayment = true,
            RequiresPayment = false,
            EnforceHardLimits = false,
            AIMinutesIncluded = 99999,
            WhatsAppMessagesIncluded = 99999,
            SMSMessagesIncluded = 99999,
            ImagesIncluded = 99999,
            BillingCycleStartDate = DateTime.UtcNow,
            NextBillingDate = DateTime.UtcNow.AddYears(10)
        };
        await _context.Set<UserSubscription>().AddAsync(adminSubscription);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Seeded admin user: admin@speakhalo.com (password must be changed on first login)");
    }

    private async Task SeedFeatureFlagsAsync()
    {
        if (await _context.FeatureFlags.AnyAsync())
        {
            _logger.LogInformation("Feature flags already exist, skipping seed");
            return;
        }

        var featureFlags = new[]
        {
            new FeatureFlag
            {
                Name = "ai_voice_assistant",
                Description = "Enable AI-powered voice assistant for calls",
                IsEnabled = true,
                RolloutPercentage = 100,
                CreatedAt = DateTime.UtcNow
            },
            new FeatureFlag
            {
                Name = "whatsapp_integration",
                Description = "Enable WhatsApp messaging integration",
                IsEnabled = true,
                RolloutPercentage = 100,
                CreatedAt = DateTime.UtcNow
            },
            new FeatureFlag
            {
                Name = "advanced_analytics",
                Description = "Enable advanced analytics dashboard",
                IsEnabled = true,
                RolloutPercentage = 100,
                CreatedAt = DateTime.UtcNow
            }
        };

        await _context.FeatureFlags.AddRangeAsync(featureFlags);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Seeded {Count} feature flags", featureFlags.Length);
    }
}

#pragma warning restore CS0618
