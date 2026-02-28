using Hangfire;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using SpeakHalo.Application.Interfaces;
using SpeakHalo.Domain.Entities;
using SpeakHalo.Domain.Enums;
using SpeakHalo.Infrastructure.Data;
using SpeakHalo.Shared.Models;
using System.Text.Json;

namespace SpeakHalo.IntegrationTests;

public class SpeakHaloWebApplicationFactory : WebApplicationFactory<Program>
{
    public const string TestPassword = "TestPassword123!";
    public const string AdminPassword = "AdminPassword123!";

    // Seeded user IDs
    public const string OwnerUserId = "test-owner-id";
    public const string AdminUserId = "test-admin-id";
    public const string MemberUserId = "test-member-id";
    public const string SecondOwnerUserId = "test-second-owner-id";

    // Seeded emails
    public const string OwnerEmail = "owner@test.com";
    public const string AdminEmail = "admin@test.com";
    public const string MemberEmail = "member@test.com";
    public const string SecondOwnerEmail = "secondowner@test.com";

    // JWT config (must match what's in ConfigureWebHost)
    public const string JwtSecretKey = "test_secret_key_minimum_32_characters_long_for_security";
    public const string JwtIssuer = "test_issuer";
    public const string JwtAudience = "test_audience";

    // Pricing plan
    public const string PricingPlanId = "test-pricing-plan-id";

    private static readonly string _hashedTestPassword = BCrypt.Net.BCrypt.HashPassword(TestPassword, workFactor: 4);
    private static readonly string _hashedAdminPassword = BCrypt.Net.BCrypt.HashPassword(AdminPassword, workFactor: 4);

    // All features enabled JSON
    public static readonly string AllFeaturesEnabled = JsonSerializer.Serialize(new Dictionary<string, bool>
    {
        ["voiceAI"] = true,
        ["whatsApp"] = true,
        ["sms"] = true,
        ["aiPhotoQuotes"] = true,
        ["aiFollowUps"] = true,
        ["advancedAnalytics"] = true,
        ["sentimentAnalysis"] = true,
        ["multiplePhoneNumbers"] = true,
        ["prioritySupport"] = true,
        ["multipleLocations"] = true,
        ["basicCRM"] = true,
        ["calendar"] = true,
        ["callTranscription"] = true,
        ["knowledgeBase"] = true,
        ["customBranding"] = true,
        ["apiAccess"] = true,
        ["teamMembers"] = true
    });

    // Limited features (no CustomBranding, no AdvancedAnalytics, no TeamMembers)
    public static readonly string LimitedFeaturesEnabled = JsonSerializer.Serialize(new Dictionary<string, bool>
    {
        ["voiceAI"] = true,
        ["whatsApp"] = true,
        ["sms"] = true,
        ["aiPhotoQuotes"] = true,
        ["aiFollowUps"] = true,
        ["advancedAnalytics"] = false,
        ["sentimentAnalysis"] = false,
        ["multiplePhoneNumbers"] = false,
        ["prioritySupport"] = false,
        ["multipleLocations"] = false,
        ["basicCRM"] = true,
        ["calendar"] = true,
        ["callTranscription"] = true,
        ["knowledgeBase"] = true,
        ["customBranding"] = false,
        ["apiAccess"] = false,
        ["teamMembers"] = false
    });

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Reset Serilog to a fresh bootstrap logger before each factory creation.
        // Program.cs calls CreateBootstrapLogger() which creates a ReloadableLogger,
        // and UseSerilog() freezes it. Subsequent factories fail trying to re-freeze.
        // We must create a fresh ReloadableLogger each time.
        Log.CloseAndFlush();
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Warning()
            .WriteTo.Console()
            .CreateBootstrapLogger();

        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SecretKey"] = JwtSecretKey,
                ["Jwt:Issuer"] = JwtIssuer,
                ["Jwt:Audience"] = JwtAudience,
                ["Paddle:WebhookSecret"] = "test_paddle_webhook_secret_key_for_hmac",
                ["Twilio:AuthToken"] = "test_twilio_auth_token_32chars_min",
                ["Twilio:AccountSid"] = "ACtest00000000000000000000000000",
            });
        });

        builder.ConfigureServices(services =>
        {
            // Remove the existing DbContext registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (descriptor != null)
                services.Remove(descriptor);

            // Use a unique in-memory database per factory instance
            var dbName = $"InMemoryTestDatabase_{Guid.NewGuid()}";
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseInMemoryDatabase(dbName);
            });

            // Remove Hangfire services (avoid PostgreSQL dependency)
            var hangfireDescriptors = services.Where(d =>
                d.ServiceType.FullName?.Contains("Hangfire") == true).ToList();
            foreach (var hd in hangfireDescriptors)
                services.Remove(hd);

            services.AddHangfire(config => config.UseInMemoryStorage());

            // Replace distributed cache with in-memory (avoids Redis connection failures)
            var cacheDescriptors = services.Where(d =>
                d.ServiceType == typeof(IDistributedCache)).ToList();
            foreach (var cd in cacheDescriptors)
                services.Remove(cd);
            services.AddDistributedMemoryCache();

            // Mock IEmailService
            var emailDesc = services.FirstOrDefault(d => d.ServiceType == typeof(IEmailService));
            if (emailDesc != null)
                services.Remove(emailDesc);
            services.AddSingleton<IEmailService>(new FakeEmailService());
        });

        builder.UseEnvironment("Testing");
    }

    /// <summary>
    /// Seed test data after the host is fully built (avoids premature Serilog Freeze via BuildServiceProvider).
    /// </summary>
    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Database.EnsureCreated();
        SeedTestData(db);

        return host;
    }

    private static void SeedTestData(ApplicationDbContext db)
    {
        if (db.Users.Any())
            return;

        // --- Pricing Plan ---
        var pricingPlan = new PricingPlan
        {
            Id = PricingPlanId,
            Name = "Professional",
            Description = "Professional plan for testing",
            DisplayPrice = 99.00m,
            TrialDays = 30,
            AIMinutesIncluded = 1000,
            WhatsAppMessagesIncluded = 500,
            SMSMessagesIncluded = 200,
            ImagesIncluded = 100,
            MaxPhoneNumbers = 3,
            EnforceHardLimits = false,
            EnabledFeatures = AllFeaturesEnabled,
            IsActive = true,
            SortOrder = 1,
            TierSlug = "professional"
        };
        db.Set<PricingPlan>().Add(pricingPlan);

        // --- Industries ---
        var industries = new[]
        {
            new Industry { Id = Guid.NewGuid().ToString(), ServiceType = ServiceType.HVAC, Name = "HVAC", Code = "hvac", IsActive = true, DisplayOrder = 1 },
            new Industry { Id = Guid.NewGuid().ToString(), ServiceType = ServiceType.Cleaning, Name = "Cleaning", Code = "cleaning", IsActive = true, DisplayOrder = 2 },
            new Industry { Id = Guid.NewGuid().ToString(), ServiceType = ServiceType.Plumbing, Name = "Plumbing", Code = "plumbing", IsActive = true, DisplayOrder = 3 },
            new Industry { Id = Guid.NewGuid().ToString(), ServiceType = ServiceType.Electrical, Name = "Electrical", Code = "electrical", IsActive = true, DisplayOrder = 4 },
            new Industry { Id = Guid.NewGuid().ToString(), ServiceType = ServiceType.Landscaping, Name = "Landscaping", Code = "landscaping", IsActive = true, DisplayOrder = 5 },
        };
        db.Set<Industry>().AddRange(industries);

        // --- User 1: Owner (primary test user) ---
        SeedUser(db, OwnerUserId, OwnerEmail, "Test Owner", UserRole.Owner,
            _hashedTestPassword, "Owner Business", ServiceType.Cleaning, AllFeaturesEnabled);

        // --- User 2: Admin ---
        SeedUser(db, AdminUserId, AdminEmail, "Test Admin", UserRole.Admin,
            _hashedAdminPassword, "Admin Corp", ServiceType.HVAC, AllFeaturesEnabled);

        // --- User 3: Member ---
        SeedUser(db, MemberUserId, MemberEmail, "Test Member", UserRole.Member,
            _hashedTestPassword, "Member LLC", ServiceType.Plumbing, LimitedFeaturesEnabled);

        // --- User 4: Second Owner (for data isolation tests) ---
        SeedUser(db, SecondOwnerUserId, SecondOwnerEmail, "Second Owner", UserRole.Owner,
            _hashedTestPassword, "Second Business", ServiceType.Electrical, AllFeaturesEnabled);

        // --- Cookie Policy (required for cookie consent endpoints) ---
        db.Set<CookiePolicy>().Add(new CookiePolicy
        {
            Id = "test-cookie-policy-id",
            Version = "1.0",
            Title = "Test Cookie Policy",
            Content = "<p>Test cookie policy content</p>",
            Summary = "We use cookies for testing",
            EffectiveDate = DateTime.UtcNow.AddDays(-30),
            IsActive = true,
            RequiresConsent = true,
            Language = "en",
            PublishedBy = "test",
            PublishedAt = DateTime.UtcNow.AddDays(-30)
        });

        db.SaveChanges();
    }

    private static void SeedUser(ApplicationDbContext db, string userId, string email, string name,
        UserRole role, string hashedPassword, string businessName, ServiceType industry, string enabledFeatures)
    {
        #pragma warning disable CS0618 // Legacy User.HashedPassword still used by AuthService.Login
        db.Users.Add(new User
        {
            Id = userId,
            Email = email,
            Name = name,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            Role = role,
            HashedPassword = hashedPassword
        });
        #pragma warning restore CS0618

        db.Set<UserAuth>().Add(new UserAuth
        {
            UserId = userId,
            HashedPassword = hashedPassword,
            EmailVerified = true
        });

        db.Set<UserProfile>().Add(new UserProfile
        {
            UserId = userId,
            BusinessName = businessName,
            Industry = industry
        });

        db.Set<UserSubscription>().Add(new UserSubscription
        {
            UserId = userId,
            Plan = "Professional",
            PricingPlanId = PricingPlanId,
            HasActivePayment = true,
            EnabledFeatures = enabledFeatures,
            AIMinutesIncluded = 1000,
            WhatsAppMessagesIncluded = 500,
            SMSMessagesIncluded = 200,
            ImagesIncluded = 100,
            MaxPhoneNumbers = 3,
            BillingCycleStartDate = DateTime.UtcNow.AddDays(-15),
            NextBillingDate = DateTime.UtcNow.AddDays(15)
        });

        db.Set<UserTrial>().Add(new UserTrial
        {
            UserId = userId,
            IsTrial = false
        });
    }
}

// Fake email service for testing
public class FakeEmailService : IEmailService
{
    public List<(string Email, string Subject, string Content)> SentEmails { get; } = new();

    public Task<Result<bool>> SendEmailAsync(string toEmail, string subject, string htmlContent, string? fromEmail = null, CancellationToken cancellationToken = default)
    {
        SentEmails.Add((toEmail, subject, htmlContent));
        return Task.FromResult(Result<bool>.Success(true));
    }

    public Task<Result<bool>> SendTemplateEmailAsync(string toEmail, string templateId, Dictionary<string, string> templateData, CancellationToken cancellationToken = default)
    {
        SentEmails.Add((toEmail, templateId, string.Join(";", templateData.Select(kv => $"{kv.Key}={kv.Value}"))));
        return Task.FromResult(Result<bool>.Success(true));
    }

    public Task<Result<bool>> SendBulkEmailAsync(List<string> toEmails, string subject, string htmlContent, CancellationToken cancellationToken = default)
    {
        foreach (var email in toEmails)
            SentEmails.Add((email, subject, htmlContent));
        return Task.FromResult(Result<bool>.Success(true));
    }

    public Task<Result<bool>> SendTeamInviteEmailAsync(string toEmail, string teamName, string inviterName, string inviteToken, CancellationToken cancellationToken = default)
    {
        SentEmails.Add((toEmail, "Team Invite", $"{teamName}|{inviterName}|{inviteToken}"));
        return Task.FromResult(Result<bool>.Success(true));
    }
}
