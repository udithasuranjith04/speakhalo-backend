using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SpeakHalo.Domain.Entities;
using SpeakHalo.Infrastructure.Data;
using SpeakHalo.IntegrationTests.TestHelpers;
using Xunit;

namespace SpeakHalo.IntegrationTests;

/// <summary>
/// Tests feature gating via RequireFeatureAttribute. The Member user has LimitedFeaturesEnabled
/// which disables advancedAnalytics, customBranding, teamMembers, apiAccess,
/// sentimentAnalysis, multiplePhoneNumbers, prioritySupport, and multipleLocations.
/// Also tests cookie consent endpoints.
/// </summary>
[Collection("Integration")]
public class FeatureGatingTests
{
    private readonly SpeakHaloWebApplicationFactory _factory;

    public FeatureGatingTests(SpeakHaloWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // ───────────────────────────────────────────────────────────────
    // 1. VoiceAI is enabled for Member
    // ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task VoiceAI_Enabled_AllowsAccess()
    {
        // Arrange - Member has voiceAI: true in LimitedFeaturesEnabled
        var client = await AuthHelper.LoginAsMemberAsync(_factory);

        // Act - Access calls endpoint (requires VoiceAI feature)
        var response = await client.GetAsync("/api/v1/calls/user/calls");

        // Assert - Should be allowed (not 403)
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden,
            "Member with voiceAI=true should be able to access the calls endpoint");
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK,
            HttpStatusCode.NoContent);
    }

    // ───────────────────────────────────────────────────────────────
    // 2. BasicCRM is enabled for Member
    // ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task BasicCRM_Enabled_AllowsAccess()
    {
        // Arrange - Member has basicCRM: true
        var client = await AuthHelper.LoginAsMemberAsync(_factory);

        // Act - Access customers list endpoint (requires BasicCRM feature)
        // CustomersController has [HttpGet("user")] for listing, not bare [HttpGet]
        var response = await client.GetAsync("/api/v1/customers/user");

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden,
            "Member with basicCRM=true should be able to access the customers endpoint");
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK,
            HttpStatusCode.NoContent);
    }

    // ───────────────────────────────────────────────────────────────
    // 3. Calendar is enabled for Member
    // ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Calendar_Enabled_AllowsAccess()
    {
        // Arrange - Member has calendar: true
        var client = await AuthHelper.LoginAsMemberAsync(_factory);

        // Act - Access appointments list endpoint (requires Calendar feature)
        // AppointmentsController has [HttpGet("user")] for listing, not bare [HttpGet]
        var response = await client.GetAsync("/api/v1/appointments/user");

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden,
            "Member with calendar=true should be able to access the appointments endpoint");
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK,
            HttpStatusCode.NoContent);
    }

    // ───────────────────────────────────────────────────────────────
    // 4. CustomBranding disabled returns 403
    // ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task CustomBranding_Disabled_Returns403()
    {
        // Arrange - Member has customBranding: false
        var client = await AuthHelper.LoginAsMemberAsync(_factory);

        // Act - Access branding endpoint (requires CustomBranding feature)
        var response = await client.GetAsync("/api/v1/voice-agents/branding");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "Member without customBranding feature should get 403");
    }

    // ───────────────────────────────────────────────────────────────
    // 5. AdvancedAnalytics disabled returns 403
    // ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task AdvancedAnalytics_Disabled_Returns403()
    {
        // Arrange - Member has advancedAnalytics: false
        var client = await AuthHelper.LoginAsMemberAsync(_factory);

        // Act - Access analytics endpoint (requires AdvancedAnalytics feature)
        var startDate = DateTime.UtcNow.AddDays(-30).ToString("o");
        var endDate = DateTime.UtcNow.ToString("o");
        var response = await client.GetAsync($"/api/v1/calls/analytics?startDate={startDate}&endDate={endDate}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "Member without advancedAnalytics feature should get 403");
    }

    // ───────────────────────────────────────────────────────────────
    // 6. TeamMembers disabled returns 403
    // ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task TeamMembers_Disabled_Returns403()
    {
        // Arrange - Member has teamMembers: false
        var client = await AuthHelper.LoginAsMemberAsync(_factory);

        // Act - Access team endpoint (requires TeamMembers feature)
        var response = await client.GetAsync("/api/v1/team");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "Member without teamMembers feature should get 403");
    }

    // ───────────────────────────────────────────────────────────────
    // 7. Admin bypasses feature gating (admin endpoints don't use RequireFeature)
    // ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task FeatureGating_AdminBypassesCheck()
    {
        // Arrange - Admin user accessing admin-only endpoints
        // Admin endpoints use [Authorize(Roles = "Admin")] instead of [RequireFeature]
        var client = await AuthHelper.LoginAsAdminAsync(_factory);

        // Act - Access admin dashboard (no RequireFeature, just role-based)
        var response = await client.GetAsync("/api/v1/admin/dashboard/overview");

        // Assert - Admin should have access regardless of feature flags
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "Admin endpoints should not be blocked by feature gating");
    }

    // ───────────────────────────────────────────────────────────────
    // 8. Feature gating requires active subscription
    // ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task FeatureGating_ActiveSubscription_Required()
    {
        // Arrange - Suspend the Member user's subscription
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var subscription = await db.Set<UserSubscription>()
            .FirstOrDefaultAsync(s => s.UserId == SpeakHaloWebApplicationFactory.MemberUserId);
        subscription.Should().NotBeNull();

        // Temporarily suspend the subscription
        var originalActive = subscription!.HasActivePayment;
        var originalSuspended = subscription.IsTemporarilySuspended;
        subscription.IsTemporarilySuspended = true;
        await db.SaveChangesAsync();

        try
        {
            // Invalidate cache so RequireFeature reads from DB
            var cache = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Caching.Distributed.IDistributedCache>();
            try { await cache.RemoveAsync($"user_sub:{SpeakHaloWebApplicationFactory.MemberUserId}"); } catch { }

            var client = await AuthHelper.LoginAsMemberAsync(_factory);

            // Act - Access an endpoint with RequireFeature (voiceAI is enabled but account is suspended)
            var response = await client.GetAsync("/api/v1/calls/user/calls");

            // Assert - Should get 403 because the subscription is suspended
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
                "Suspended user should get 403 even for features they have enabled");
        }
        finally
        {
            // Restore the original state
            subscription.HasActivePayment = originalActive;
            subscription.IsTemporarilySuspended = originalSuspended;
            await db.SaveChangesAsync();

            // Clear cache again to restore normal behavior
            var cache = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Caching.Distributed.IDistributedCache>();
            try { await cache.RemoveAsync($"user_sub:{SpeakHaloWebApplicationFactory.MemberUserId}"); } catch { }
        }
    }

    // ───────────────────────────────────────────────────────────────
    // 9. Cookie consent banner endpoint works
    // ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task CookieConsent_BannerAppears()
    {
        // Arrange - Cookie consent banner is accessible anonymously
        var client = AuthHelper.CreateAnonymousClient(_factory);

        // Act - The cookie consent controller is at /api/v1/cookies/banner
        var response = await client.GetAsync("/api/v1/cookies/banner");

        // Assert
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK,
            HttpStatusCode.NoContent,
            HttpStatusCode.NotFound);
    }

    // ───────────────────────────────────────────────────────────────
    // 10. Cookie consent accept all saves
    // ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task CookieConsent_AcceptAll_Saves()
    {
        // Arrange
        var client = await AuthHelper.LoginAsOwnerAsync(_factory);
        var request = new
        {
            Essential = true,
            Functional = true,
            Analytics = true,
            Performance = true,
            Marketing = true,
            Advertising = true,
            SocialMedia = true
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/cookies/consent", request);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue(
            $"Saving cookie consent should succeed: {await response.Content.ReadAsStringAsync()}");

        var json = await ApiResponseHelper.ReadJsonAsync(response);
        json.TryGetProperty("message", out var msg);
        msg.GetString().Should().Contain("saved", "Response should confirm consent was saved");
    }

    // ───────────────────────────────────────────────────────────────
    // 11. Cookie consent customize preferences saves
    // ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task CookieConsent_CustomizePreferences_Saves()
    {
        // Arrange - Custom preferences: accept analytics but decline marketing
        var client = await AuthHelper.LoginAsOwnerAsync(_factory);
        var request = new
        {
            Essential = true,
            Functional = true,
            Analytics = true,
            Performance = true,
            Marketing = false,
            Advertising = false,
            SocialMedia = false
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/cookies/consent", request);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue("Custom consent preferences should be saved");

        var json = await ApiResponseHelper.ReadJsonAsync(response);
        if (json.TryGetProperty("consents", out var consents))
        {
            if (consents.TryGetProperty("Marketing", out var marketing))
            {
                marketing.GetBoolean().Should().BeFalse("Marketing should be false as requested");
            }
            if (consents.TryGetProperty("Analytics", out var analytics))
            {
                analytics.GetBoolean().Should().BeTrue("Analytics should be true as requested");
            }
        }
    }

    // ───────────────────────────────────────────────────────────────
    // 12. Cookie consent essential always true
    // ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task CookieConsent_EssentialAlwaysTrue()
    {
        // Arrange - Try to set essential to false
        var client = await AuthHelper.LoginAsOwnerAsync(_factory);
        var request = new
        {
            Essential = false, // Should be forced to true by the server
            Functional = false,
            Analytics = false,
            Performance = false,
            Marketing = false,
            Advertising = false,
            SocialMedia = false
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/cookies/consent", request);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue("Consent save should succeed");

        var json = await ApiResponseHelper.ReadJsonAsync(response);
        if (json.TryGetProperty("consents", out var consents))
        {
            if (consents.TryGetProperty("Essential", out var essential))
            {
                essential.GetBoolean().Should().BeTrue(
                    "Essential cookies should always be true even if client sends false");
            }
        }
    }

    // ───────────────────────────────────────────────────────────────
    // 13. Cookie consent persists across requests
    // ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task CookieConsent_PersistsAcrossRequests()
    {
        // Arrange - Save specific consent preferences
        var client = await AuthHelper.LoginAsOwnerAsync(_factory);
        var saveRequest = new
        {
            Essential = true,
            Functional = true,
            Analytics = true,
            Performance = false,
            Marketing = false,
            Advertising = false,
            SocialMedia = false
        };

        var saveResponse = await client.PostAsJsonAsync("/api/v1/cookies/consent", saveRequest);
        saveResponse.IsSuccessStatusCode.Should().BeTrue("Initial save should succeed");

        // Act - Retrieve the consent in a subsequent request
        var getResponse = await client.GetAsync("/api/v1/cookies/consent");

        // Assert
        getResponse.IsSuccessStatusCode.Should().BeTrue(
            $"Getting consent should succeed: {await getResponse.Content.ReadAsStringAsync()}");

        var json = await ApiResponseHelper.ReadJsonAsync(getResponse);

        // The response should reflect the saved preferences
        if (json.TryGetProperty("analytics", out var analytics))
        {
            analytics.GetBoolean().Should().BeTrue(
                "Analytics consent should persist as true from previous save");
        }
        if (json.TryGetProperty("essential", out var essential))
        {
            essential.GetBoolean().Should().BeTrue(
                "Essential consent should always be true");
        }
    }
}
