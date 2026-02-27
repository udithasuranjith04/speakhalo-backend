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
            await SeedPricingPlansAsync();

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
            Industry = ServiceType.Cleaning,
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

    public async Task SeedPricingPlansAsync()
    {
        // Overage rates are designed to be profitable across all supported countries
        // (US: voice $0.017/min, SMS $0.008/msg; UK: voice $0.024/min, SMS $0.040/msg;
        //  AU: voice $0.034/min, SMS $0.050/msg). Rates below cover worst-case AU/IE pricing.
        //
        // Paddle IDs (PaddleProductId / PaddlePriceId) are intentionally left empty here.
        // Set them in the database after creating products in your Paddle dashboard.
        // They will NOT be overwritten on subsequent seed runs once set.

        // All plans include every feature. Differentiation is purely by:
        //   1. Included monthly usage volume (more on higher plans)
        //   2. Overage rates (lower on higher plans — the core upgrade incentive)
        //   3. 1 dedicated phone number on all plans
        //
        // Overage cost floors (AU/IE worst-case): voice $0.058/min, SMS $0.0735/msg, WA $0.0084/msg.
        // All rates below maintain positive margin across every supported country.
        const string allFeatures = "{\"voiceAI\":true,\"sms\":true,\"calendar\":true,\"knowledgeBase\":true,\"whatsApp\":true,\"aiPhotoQuotes\":true,\"advancedAnalytics\":true,\"customBranding\":true,\"teamMembers\":true,\"multipleLocations\":true}";

        var planDefs = new[]
        {
            new
            {
                TierSlug = "starter",
                Name = "Starter",
                Description = "Every feature included. Perfect for solo operators ready to stop missing calls and start booking more jobs.",
                DisplayPrice = 49m,
                SortOrder = 0,
                TrialDays = 14,
                AIMinutesIncluded = 150,
                SMSMessagesIncluded = 150,
                WhatsAppMessagesIncluded = 150,
                ImagesIncluded = 30,
                MaxPhoneNumbers = 1,
                EnforceHardLimits = true,
                // Highest overage — natural upgrade signal when regularly going over limits
                AIOverageRate = 0.25m,
                SMSOverageRate = 0.18m,
                WhatsAppOverageRate = 0.04m,
                ImageOverageRate = 0.75m,
                EnabledFeatures = allFeatures
            },
            new
            {
                TierSlug = "professional",
                Name = "Professional",
                Description = "Every feature included. More monthly volume and lower overage rates for growing businesses.",
                DisplayPrice = 99m,
                SortOrder = 1,
                TrialDays = 14,
                AIMinutesIncluded = 500,
                SMSMessagesIncluded = 500,
                WhatsAppMessagesIncluded = 400,
                ImagesIncluded = 100,
                MaxPhoneNumbers = 1,
                EnforceHardLimits = false,
                // Mid-tier overage — rewards moving up from Starter
                AIOverageRate = 0.20m,
                SMSOverageRate = 0.13m,
                WhatsAppOverageRate = 0.025m,
                ImageOverageRate = 0.50m,
                EnabledFeatures = allFeatures
            },
            new
            {
                TierSlug = "business",
                Name = "Business",
                Description = "Every feature included. Highest monthly volume and the lowest overage rates for established teams.",
                DisplayPrice = 199m,
                SortOrder = 2,
                TrialDays = 14,
                AIMinutesIncluded = 1200,
                SMSMessagesIncluded = 1000,
                WhatsAppMessagesIncluded = 1000,
                ImagesIncluded = 250,
                MaxPhoneNumbers = 1,
                EnforceHardLimits = false,
                // Lowest overage — best value for high-volume businesses
                AIOverageRate = 0.15m,
                SMSOverageRate = 0.10m,
                WhatsAppOverageRate = 0.015m,
                ImageOverageRate = 0.30m,
                EnabledFeatures = allFeatures
            }
        };

        int created = 0, updated = 0;

        foreach (var def in planDefs)
        {
            var existing = await _context.Set<PricingPlan>()
                .FirstOrDefaultAsync(p => p.TierSlug == def.TierSlug && !p.IsDeleted);

            if (existing != null)
            {
                // Update non-Paddle fields so plan values stay in sync with code.
                // PaddleProductId and PaddlePriceId are never overwritten (set manually in Paddle dashboard).
                existing.Name = def.Name;
                existing.Description = def.Description;
                existing.DisplayPrice = def.DisplayPrice;
                existing.SortOrder = def.SortOrder;
                existing.TrialDays = def.TrialDays;
                existing.AIMinutesIncluded = def.AIMinutesIncluded;
                existing.SMSMessagesIncluded = def.SMSMessagesIncluded;
                existing.WhatsAppMessagesIncluded = def.WhatsAppMessagesIncluded;
                existing.ImagesIncluded = def.ImagesIncluded;
                existing.MaxPhoneNumbers = def.MaxPhoneNumbers;
                existing.EnforceHardLimits = def.EnforceHardLimits;
                existing.AIOverageRate = def.AIOverageRate;
                existing.SMSOverageRate = def.SMSOverageRate;
                existing.WhatsAppOverageRate = def.WhatsAppOverageRate;
                existing.ImageOverageRate = def.ImageOverageRate;
                existing.EnabledFeatures = def.EnabledFeatures;
                existing.IsActive = true;
                existing.UpdatedAt = DateTime.UtcNow;
                _context.Set<PricingPlan>().Update(existing);
                updated++;
            }
            else
            {
                var plan = new PricingPlan
                {
                    TierSlug = def.TierSlug,
                    Name = def.Name,
                    Description = def.Description,
                    DisplayPrice = def.DisplayPrice,
                    SortOrder = def.SortOrder,
                    IsActive = true,
                    TrialDays = def.TrialDays,
                    AIMinutesIncluded = def.AIMinutesIncluded,
                    SMSMessagesIncluded = def.SMSMessagesIncluded,
                    WhatsAppMessagesIncluded = def.WhatsAppMessagesIncluded,
                    ImagesIncluded = def.ImagesIncluded,
                    MaxPhoneNumbers = def.MaxPhoneNumbers,
                    EnforceHardLimits = def.EnforceHardLimits,
                    AIOverageRate = def.AIOverageRate,
                    SMSOverageRate = def.SMSOverageRate,
                    WhatsAppOverageRate = def.WhatsAppOverageRate,
                    ImageOverageRate = def.ImageOverageRate,
                    EnabledFeatures = def.EnabledFeatures,
                    // PaddleProductId / PaddlePriceId: set in Paddle dashboard, then update DB manually
                    PaddleProductId = null,
                    PaddlePriceId = null,
                    CreatedAt = DateTime.UtcNow
                };
                await _context.Set<PricingPlan>().AddAsync(plan);
                created++;
            }
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Pricing plans seeded: {Created} created, {Updated} updated", created, updated);

        // Deactivate any plan whose TierSlug is NOT in our maintained list.
        // This handles leftover plans from earlier seeder runs (e.g. a 4th "enterprise" plan).
        var maintainedSlugs = planDefs.Select(p => p.TierSlug).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var orphanedPlans = await _context.Set<PricingPlan>()
            .Where(p => !p.IsDeleted && !maintainedSlugs.Contains(p.TierSlug))
            .ToListAsync();

        if (orphanedPlans.Count > 0)
        {
            foreach (var orphan in orphanedPlans)
            {
                orphan.IsActive = false;
                orphan.UpdatedAt = DateTime.UtcNow;
                _context.Set<PricingPlan>().Update(orphan);
            }
            await _context.SaveChangesAsync();
            _logger.LogInformation(
                "Deactivated {Count} orphaned pricing plan(s): {Slugs}",
                orphanedPlans.Count,
                string.Join(", ", orphanedPlans.Select(p => p.TierSlug)));
        }
    }
}

#pragma warning restore CS0618
