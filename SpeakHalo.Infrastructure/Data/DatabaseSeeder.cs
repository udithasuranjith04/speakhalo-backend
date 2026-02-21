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
            await SeedDemoUsersAsync();
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
        var adminPassword = Environment.GetEnvironmentVariable("SPEAKHALO_ADMIN_SEED_PASSWORD") ?? "Admin@123456";
        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(adminPassword, workFactor: 12);

        var existingAdmin = await _context.Users.FirstOrDefaultAsync(u => u.Email == "admin@speakhalo.com");
        if (existingAdmin != null)
        {
            var existingAuth = await _context.Set<UserAuth>().FirstOrDefaultAsync(a => a.UserId == existingAdmin.Id);
            if (existingAuth != null)
            {
                existingAuth.HashedPassword = hashedPassword;
                existingAuth.PasswordMustChange = false;
                existingAuth.EmailVerified = true;
                _context.Set<UserAuth>().Update(existingAuth);
            }

            // Ensure subscription exists
            var existingSub = await _context.Set<UserSubscription>().FirstOrDefaultAsync(s => s.UserId == existingAdmin.Id);
            if (existingSub == null)
            {
                await _context.Set<UserSubscription>().AddAsync(new UserSubscription
                {
                    UserId = existingAdmin.Id,
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
                });
                _logger.LogInformation("Created missing subscription for admin");
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Admin user already exists, reset password");
            return;
        }

        var adminUser = new User
        {
            Name = "Admin User",
            Email = "admin@speakhalo.com",
            Role = UserRole.Admin,
            IsActive = true,
            BusinessName = "SpeakHalo Admin",
            Industry = ServiceType.HomeServices,
            OnboardingCompleted = true,
            OnboardingStep = 999,
            CreatedAt = DateTime.UtcNow
        };
        await _context.Users.AddAsync(adminUser);
        await _context.SaveChangesAsync();

        var adminAuth = new UserAuth
        {
            UserId = adminUser.Id,
            HashedPassword = hashedPassword,
            PasswordMustChange = false,
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

        _logger.LogInformation("Seeded admin user: admin@speakhalo.com");
    }

    private async Task SeedDemoUsersAsync()
    {
        await SeedOrResetUserAsync("owner@demo.com", "Demo@123456", "Sarah Thompson", UserRole.Owner,
            "Sparkle Clean Co.", ServiceType.Cleaning, "business");
        await SeedOrResetUserAsync("member@demo.com", "Demo@123456", "David Chen", UserRole.Member,
            "Premier Clean Services", ServiceType.Cleaning, "starter");
    }

    private async Task SeedOrResetUserAsync(string email, string password, string name,
        UserRole role, string businessName, ServiceType industry, string plan)
    {
        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);

        var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (existingUser != null)
        {
            var existingAuth = await _context.Set<UserAuth>().FirstOrDefaultAsync(a => a.UserId == existingUser.Id);
            if (existingAuth != null)
            {
                existingAuth.HashedPassword = hashedPassword;
                existingAuth.PasswordMustChange = false;
                existingAuth.EmailVerified = true;
                _context.Set<UserAuth>().Update(existingAuth);
            }

            // Ensure subscription exists (may have been missing from earlier seeds)
            var existingSub = await _context.Set<UserSubscription>().FirstOrDefaultAsync(s => s.UserId == existingUser.Id);
            if (existingSub == null)
            {
                await _context.Set<UserSubscription>().AddAsync(new UserSubscription
                {
                    UserId = existingUser.Id,
                    Plan = plan,
                    HasActivePayment = true,
                    RequiresPayment = false,
                    EnforceHardLimits = plan == "starter",
                    AIMinutesIncluded = plan == "business" ? 500 : 100,
                    WhatsAppMessagesIncluded = plan == "business" ? 1000 : 200,
                    SMSMessagesIncluded = plan == "business" ? 200 : 50,
                    ImagesIncluded = plan == "business" ? 100 : 20,
                    BillingCycleStartDate = DateTime.UtcNow.AddDays(-15),
                    NextBillingDate = DateTime.UtcNow.AddDays(15)
                });
                _logger.LogInformation("Created missing subscription for {Email}", email);
            }
            else if (!existingSub.HasActivePayment)
            {
                existingSub.HasActivePayment = true;
                existingSub.IsTemporarilySuspended = false;
                _context.Set<UserSubscription>().Update(existingSub);
                _logger.LogInformation("Reactivated subscription for {Email}", email);
            }

            // Ensure profile exists
            var existingProfile = await _context.Set<UserProfile>().FirstOrDefaultAsync(p => p.UserId == existingUser.Id);
            if (existingProfile == null)
            {
                await _context.Set<UserProfile>().AddAsync(new UserProfile
                {
                    UserId = existingUser.Id,
                    BusinessName = businessName,
                    Industry = industry
                });
                _logger.LogInformation("Created missing profile for {Email}", email);
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("User {Email} already exists, reset password", email);
            return;
        }

        var user = new User
        {
            Name = name,
            Email = email,
            Role = role,
            IsActive = true,
            BusinessName = businessName,
            Industry = industry,
            OnboardingCompleted = true,
            OnboardingStep = 999,
            CreatedAt = DateTime.UtcNow.AddDays(-14)
        };
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        await _context.Set<UserAuth>().AddAsync(new UserAuth
        {
            UserId = user.Id,
            HashedPassword = hashedPassword,
            PasswordMustChange = false,
            EmailVerified = true
        });

        await _context.Set<UserSubscription>().AddAsync(new UserSubscription
        {
            UserId = user.Id,
            Plan = plan,
            HasActivePayment = true,
            RequiresPayment = false,
            EnforceHardLimits = plan == "starter",
            AIMinutesIncluded = plan == "business" ? 500 : 100,
            WhatsAppMessagesIncluded = plan == "business" ? 1000 : 200,
            SMSMessagesIncluded = plan == "business" ? 200 : 50,
            ImagesIncluded = plan == "business" ? 100 : 20,
            BillingCycleStartDate = DateTime.UtcNow.AddDays(-15),
            NextBillingDate = DateTime.UtcNow.AddDays(15)
        });

        await _context.Set<UserProfile>().AddAsync(new UserProfile
        {
            UserId = user.Id,
            BusinessName = businessName,
            Industry = industry
        });
        await _context.SaveChangesAsync();

        _logger.LogInformation("Seeded user: {Email}", email);
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
