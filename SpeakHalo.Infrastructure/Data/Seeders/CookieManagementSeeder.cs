using Microsoft.EntityFrameworkCore;
using SpeakHalo.Domain.Entities;
using SpeakHalo.Domain.Enums;

namespace SpeakHalo.Infrastructure.Data.Seeders;

/// <summary>
/// Seeds initial cookie definitions and policies for GDPR/CCPA compliance
/// </summary>
public static class CookieManagementSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        // Check if already seeded
        if (await context.CookieDefinitions.AnyAsync())
        {
            return; // Already seeded
        }

        // Seed Cookie Definitions
        var definitions = new List<CookieDefinition>
        {
            // ========== ESSENTIAL COOKIES ==========
            new CookieDefinition
            {
                Name = "session_token",
                DisplayName = "Session Cookie",
                Description = "Maintains your authenticated session and keeps you logged in as you navigate the site.",
                Category = CookieCategory.Essential,
                Domain = ".speakhalo.com",
                Path = "/",
                ExpirationDays = 1,
                IsHttpOnly = true,
                IsSecure = true,
                SameSite = "Lax",
                IsActive = true,
                IsEssential = true,
                Provider = "SpeakHalo",
                DisplayOrder = 1
            },
            new CookieDefinition
            {
                Name = "csrf_token",
                DisplayName = "CSRF Protection Token",
                Description = "Protects against Cross-Site Request Forgery attacks to keep your account secure.",
                Category = CookieCategory.Essential,
                Domain = ".speakhalo.com",
                Path = "/",
                ExpirationDays = null, // Session cookie
                IsHttpOnly = true,
                IsSecure = true,
                SameSite = "Strict",
                IsActive = true,
                IsEssential = true,
                Provider = "SpeakHalo",
                DisplayOrder = 2
            },
            new CookieDefinition
            {
                Name = "consent_token",
                DisplayName = "Cookie Consent Token",
                Description = "Stores your cookie consent preferences.",
                Category = CookieCategory.Essential,
                Domain = ".speakhalo.com",
                Path = "/",
                ExpirationDays = 365,
                IsHttpOnly = true,
                IsSecure = true,
                SameSite = "Lax",
                IsActive = true,
                IsEssential = true,
                Provider = "SpeakHalo",
                DisplayOrder = 3
            },

            // ========== FUNCTIONAL COOKIES ==========
            new CookieDefinition
            {
                Name = "user_preferences",
                DisplayName = "User Preferences",
                Description = "Remembers your preferences like language, theme, and display settings.",
                Category = CookieCategory.Functional,
                Domain = ".speakhalo.com",
                Path = "/",
                ExpirationDays = 90,
                IsHttpOnly = false,
                IsSecure = true,
                SameSite = "Lax",
                IsActive = true,
                IsEssential = false,
                Provider = "SpeakHalo",
                DisplayOrder = 10
            },
            new CookieDefinition
            {
                Name = "language",
                DisplayName = "Language Preference",
                Description = "Stores your preferred language for the interface.",
                Category = CookieCategory.Functional,
                Domain = ".speakhalo.com",
                Path = "/",
                ExpirationDays = 365,
                IsHttpOnly = false,
                IsSecure = true,
                SameSite = "Lax",
                IsActive = true,
                IsEssential = false,
                Provider = "SpeakHalo",
                DisplayOrder = 11
            },

            // ========== ANALYTICS COOKIES ==========
            new CookieDefinition
            {
                Name = "_ga",
                DisplayName = "Google Analytics",
                Description = "Tracks website usage and visitor behavior to help us improve our service.",
                Category = CookieCategory.Analytics,
                Domain = ".speakhalo.com",
                Path = "/",
                ExpirationDays = 730,
                IsHttpOnly = false,
                IsSecure = true,
                SameSite = "Lax",
                IsActive = true,
                IsEssential = false,
                Provider = "Google",
                PrivacyPolicyUrl = "https://policies.google.com/privacy",
                DisplayOrder = 20
            },
            new CookieDefinition
            {
                Name = "_gid",
                DisplayName = "Google Analytics ID",
                Description = "Distinguishes users for Google Analytics.",
                Category = CookieCategory.Analytics,
                Domain = ".speakhalo.com",
                Path = "/",
                ExpirationDays = 1,
                IsHttpOnly = false,
                IsSecure = true,
                SameSite = "Lax",
                IsActive = true,
                IsEssential = false,
                Provider = "Google",
                PrivacyPolicyUrl = "https://policies.google.com/privacy",
                DisplayOrder = 21
            },
            new CookieDefinition
            {
                Name = "_gat",
                DisplayName = "Google Analytics Throttle",
                Description = "Throttles request rate for Google Analytics.",
                Category = CookieCategory.Analytics,
                Domain = ".speakhalo.com",
                Path = "/",
                ExpirationDays = null, // 1 minute
                IsHttpOnly = false,
                IsSecure = true,
                SameSite = "Lax",
                IsActive = true,
                IsEssential = false,
                Provider = "Google",
                PrivacyPolicyUrl = "https://policies.google.com/privacy",
                DisplayOrder = 22
            },

            // ========== PERFORMANCE COOKIES ==========
            new CookieDefinition
            {
                Name = "performance_metrics",
                DisplayName = "Performance Monitoring",
                Description = "Monitors application performance and load times to optimize user experience.",
                Category = CookieCategory.Performance,
                Domain = ".speakhalo.com",
                Path = "/",
                ExpirationDays = 7,
                IsHttpOnly = false,
                IsSecure = true,
                SameSite = "Lax",
                IsActive = true,
                IsEssential = false,
                Provider = "SpeakHalo",
                DisplayOrder = 30
            },

            // ========== MARKETING COOKIES ==========
            new CookieDefinition
            {
                Name = "marketing_campaign",
                DisplayName = "Marketing Campaign Tracking",
                Description = "Tracks which marketing campaigns led you to our site to measure effectiveness.",
                Category = CookieCategory.Marketing,
                Domain = ".speakhalo.com",
                Path = "/",
                ExpirationDays = 30,
                IsHttpOnly = false,
                IsSecure = true,
                SameSite = "Lax",
                IsActive = true,
                IsEssential = false,
                Provider = "SpeakHalo",
                DisplayOrder = 40
            },
            new CookieDefinition
            {
                Name = "email_campaign",
                DisplayName = "Email Campaign Tracking",
                Description = "Tracks email marketing campaign interactions.",
                Category = CookieCategory.Marketing,
                Domain = ".speakhalo.com",
                Path = "/",
                ExpirationDays = 30,
                IsHttpOnly = false,
                IsSecure = true,
                SameSite = "Lax",
                IsActive = true,
                IsEssential = false,
                Provider = "SpeakHalo",
                DisplayOrder = 41
            },

            // ========== ADVERTISING COOKIES ==========
            new CookieDefinition
            {
                Name = "_fbp",
                DisplayName = "Facebook Pixel",
                Description = "Used by Facebook to deliver advertising and measure ad effectiveness.",
                Category = CookieCategory.Advertising,
                Domain = ".speakhalo.com",
                Path = "/",
                ExpirationDays = 90,
                IsHttpOnly = false,
                IsSecure = true,
                SameSite = "Lax",
                IsActive = true,
                IsEssential = false,
                Provider = "Facebook",
                PrivacyPolicyUrl = "https://www.facebook.com/privacy/explanation",
                DisplayOrder = 50
            },

            // ========== SOCIAL MEDIA COOKIES ==========
            new CookieDefinition
            {
                Name = "social_share",
                DisplayName = "Social Sharing",
                Description = "Enables social media sharing features and tracks shared content.",
                Category = CookieCategory.SocialMedia,
                Domain = ".speakhalo.com",
                Path = "/",
                ExpirationDays = 30,
                IsHttpOnly = false,
                IsSecure = true,
                SameSite = "Lax",
                IsActive = true,
                IsEssential = false,
                Provider = "SpeakHalo",
                DisplayOrder = 60
            }
        };

        await context.CookieDefinitions.AddRangeAsync(definitions);

        // Seed Initial Cookie Policy
        var policy = new CookiePolicy
        {
            Version = "1.0",
            Title = "SpeakHalo Cookie Policy",
            Summary = "We use cookies to improve your experience, analyze site traffic, and deliver personalized content. You can manage your preferences at any time.",
            Content = @"
<div class=""cookie-policy"">
    <h1>Cookie Policy</h1>
    <p><strong>Last Updated:</strong> February 9, 2026</p>

    <h2>What Are Cookies?</h2>
    <p>Cookies are small text files that are stored on your device when you visit our website. They help us provide you with a better experience by remembering your preferences and understanding how you use our service.</p>

    <h2>How We Use Cookies</h2>
    <p>We use cookies for the following purposes:</p>
    <ul>
        <li><strong>Essential:</strong> Required for basic functionality like authentication and security</li>
        <li><strong>Functional:</strong> Remember your preferences and settings</li>
        <li><strong>Analytics:</strong> Understand how visitors interact with our website</li>
        <li><strong>Performance:</strong> Monitor and improve website performance</li>
        <li><strong>Marketing:</strong> Track campaign effectiveness and conversions</li>
        <li><strong>Advertising:</strong> Deliver personalized advertisements</li>
        <li><strong>Social Media:</strong> Enable social sharing features</li>
    </ul>

    <h2>Your Choices</h2>
    <p>You can manage your cookie preferences at any time using our cookie consent banner. Essential cookies cannot be disabled as they are necessary for the website to function.</p>

    <h2>Third-Party Cookies</h2>
    <p>We use services from trusted third parties like Google Analytics and Facebook Pixel. These services may set their own cookies to provide functionality. Please refer to their privacy policies for more information.</p>

    <h2>Managing Cookies</h2>
    <p>You can also manage cookies through your browser settings. However, disabling cookies may affect your ability to use certain features of our website.</p>

    <h2>Contact Us</h2>
    <p>If you have questions about our cookie policy, please contact us at <a href=""mailto:hello@speakhalo.com"">hello@speakhalo.com</a></p>
</div>
",
            EffectiveDate = DateTime.UtcNow,
            IsActive = true,
            RequiresConsent = true,
            Language = "en",
            PolicyUrl = "https://speakhalo.com/cookie-policy",
            PublishedBy = "System",
            PublishedAt = DateTime.UtcNow,
            ChangeNotes = "Initial cookie policy version"
        };

        await context.CookiePolicies.AddAsync(policy);

        await context.SaveChangesAsync();

        // Cookie management data seeded: definitions + policy created
    }
}
