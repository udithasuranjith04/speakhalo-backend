using Hangfire;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SpeakHalo.Application.Interfaces;
using SpeakHalo.Infrastructure.Data;
using SpeakHalo.Shared.Models;

namespace SpeakHalo.IntegrationTests;

public class SpeakHaloWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            // Add test-specific configuration
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SecretKey"] = "test_secret_key_minimum_32_characters_long_for_security",
                ["Jwt:Issuer"] = "test_issuer",
                ["Jwt:Audience"] = "test_audience"
            });
        });

        builder.ConfigureServices(services =>
        {
            // Remove the existing DbContext registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));

            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Add in-memory database for testing
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseInMemoryDatabase("InMemoryTestDatabase");
            });

            // Remove Hangfire services for testing (avoid PostgreSQL dependency)
            var hangfireDescriptors = services.Where(d =>
                d.ServiceType.FullName?.Contains("Hangfire") == true).ToList();

            foreach (var hangfireDescriptor in hangfireDescriptors)
            {
                services.Remove(hangfireDescriptor);
            }

            // Use in-memory Hangfire storage for tests
            services.AddHangfire(config =>
            {
                config.UseInMemoryStorage();
            });

            // Mock IEmailService for tests to prevent email sending
            var mockEmailService = new FakeEmailService();

            // Remove real email service and register mock
            var emailServiceDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IEmailService));
            if (emailServiceDescriptor != null)
            {
                services.Remove(emailServiceDescriptor);
            }
            services.AddSingleton<IEmailService>(mockEmailService);

            // Build service provider and create database
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var scopedServices = scope.ServiceProvider;
            var db = scopedServices.GetRequiredService<ApplicationDbContext>();

            db.Database.EnsureCreated();

            // Seed test data
            SeedTestData(db);
        });

        builder.UseEnvironment("Testing");
    }

    private static void SeedTestData(ApplicationDbContext db)
    {
        // Check if data already exists
        if (db.Users.Any())
            return;

        // Create test user with proper entity split (User + UserAuth + UserProfile + UserSubscription)
        var testUser = new Domain.Entities.User
        {
            Id = "test-user-id",
            Email = "test@example.com",
            Name = "Test User",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            Role = Domain.Enums.UserRole.Owner
        };
        db.Users.Add(testUser);

        var testAuth = new Domain.Entities.UserAuth
        {
            UserId = "test-user-id",
            HashedPassword = "hashed_password",
            EmailVerified = true
        };
        db.Set<Domain.Entities.UserAuth>().Add(testAuth);

        var testProfile = new Domain.Entities.UserProfile
        {
            UserId = "test-user-id",
            BusinessName = "Test Business",
            Industry = Domain.Enums.ServiceType.Cleaning
        };
        db.Set<Domain.Entities.UserProfile>().Add(testProfile);

        var testSubscription = new Domain.Entities.UserSubscription
        {
            UserId = "test-user-id",
            Plan = "Professional",
            HasActivePayment = true,
            AIMinutesIncluded = 1000,
            WhatsAppMessagesIncluded = 500,
            SMSMessagesIncluded = 200,
            ImagesIncluded = 100
        };
        db.Set<Domain.Entities.UserSubscription>().Add(testSubscription);

        var testTrial = new Domain.Entities.UserTrial
        {
            UserId = "test-user-id",
            IsTrial = false
        };
        db.Set<Domain.Entities.UserTrial>().Add(testTrial);

        db.SaveChanges();
    }
}

// Fake email service for testing
public class FakeEmailService : IEmailService
{
    public Task<Result<bool>> SendEmailAsync(string toEmail, string subject, string htmlContent, string? fromEmail = null, CancellationToken cancellationToken = default)
    {
        // Don't actually send emails in tests
        return Task.FromResult(Result<bool>.Success(true));
    }

    public Task<Result<bool>> SendTemplateEmailAsync(string toEmail, string templateId, Dictionary<string, string> templateData, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result<bool>.Success(true));
    }

    public Task<Result<bool>> SendBulkEmailAsync(List<string> toEmails, string subject, string htmlContent, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result<bool>.Success(true));
    }

    public Task<Result<bool>> SendTeamInviteEmailAsync(string toEmail, string teamName, string inviterName, string inviteToken, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result<bool>.Success(true));
    }
}
