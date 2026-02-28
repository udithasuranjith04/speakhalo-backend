using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SpeakHalo.IntegrationTests.TestHelpers;
using Xunit;

namespace SpeakHalo.IntegrationTests;

/// <summary>
/// Tests role-based access control (RBAC), self-access restrictions,
/// and unauthenticated access across the API.
/// </summary>
[Collection("Integration")]
public class AuthorizationTests
{
    private readonly SpeakHaloWebApplicationFactory _factory;

    public AuthorizationTests(SpeakHaloWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // ───────────────────────────────────────────────────────────────
    // Admin-only endpoints: Owner must be rejected
    // ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task AdminDashboard_AsOwner_Returns403()
    {
        // Arrange
        var client = await AuthHelper.LoginAsOwnerAsync(_factory);

        // Act
        var response = await client.GetAsync("/api/v1/admin/dashboard/overview");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AdminDashboard_AsMember_Returns403()
    {
        // Arrange
        var client = await AuthHelper.LoginAsMemberAsync(_factory);

        // Act
        var response = await client.GetAsync("/api/v1/admin/dashboard/overview");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AdminUserManagement_AsOwner_Returns403()
    {
        // Arrange
        var client = await AuthHelper.LoginAsOwnerAsync(_factory);

        // Act
        var response = await client.GetAsync("/api/v1/admin/users");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AdminUserManagement_AsMember_Returns403()
    {
        // Arrange
        var client = await AuthHelper.LoginAsMemberAsync(_factory);

        // Act
        var response = await client.GetAsync("/api/v1/admin/users");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AdminPricingPlans_AsOwner_Returns403()
    {
        // Arrange
        var client = await AuthHelper.LoginAsOwnerAsync(_factory);

        // Act
        var response = await client.GetAsync("/api/v1/admin/pricing-plans");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AdminPricingPlans_AsMember_Returns403()
    {
        // Arrange
        var client = await AuthHelper.LoginAsMemberAsync(_factory);

        // Act
        var response = await client.GetAsync("/api/v1/admin/pricing-plans");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AdminTrialInvites_AsOwner_Returns403()
    {
        // Arrange
        var client = await AuthHelper.LoginAsOwnerAsync(_factory);

        // Act
        var response = await client.GetAsync("/api/v1/admin/trial-invites");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AdminTrialInvites_AsMember_Returns403()
    {
        // Arrange
        var client = await AuthHelper.LoginAsMemberAsync(_factory);

        // Act
        var response = await client.GetAsync("/api/v1/admin/trial-invites");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AdminAnalytics_AsOwner_Returns403()
    {
        // Arrange
        var client = await AuthHelper.LoginAsOwnerAsync(_factory);

        // Act
        var response = await client.GetAsync("/api/v1/admin/analytics/overview");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AdminAnalytics_AsMember_Returns403()
    {
        // Arrange
        var client = await AuthHelper.LoginAsMemberAsync(_factory);

        // Act
        var response = await client.GetAsync("/api/v1/admin/analytics/overview");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AdminKnowledgeTemplates_AsOwner_Returns403()
    {
        // Arrange
        var client = await AuthHelper.LoginAsOwnerAsync(_factory);

        // Act
        var response = await client.GetAsync("/api/v1/admin/knowledge-templates");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AdminKnowledgeTemplates_AsMember_Returns403()
    {
        // Arrange
        var client = await AuthHelper.LoginAsMemberAsync(_factory);

        // Act
        var response = await client.GetAsync("/api/v1/admin/knowledge-templates");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ───────────────────────────────────────────────────────────────
    // Admin CAN access admin endpoints
    // ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task AdminDashboard_AsAdmin_Returns200()
    {
        // Arrange
        var client = await AuthHelper.LoginAsAdminAsync(_factory);

        // Act
        var response = await client.GetAsync("/api/v1/admin/dashboard/overview");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ───────────────────────────────────────────────────────────────
    // Self-access restrictions: users cannot access other users' data
    // ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Users_GetOtherUser_Returns403()
    {
        // Arrange - Owner tries to access Admin's user profile
        var client = await AuthHelper.LoginAsOwnerAsync(_factory);

        // Act
        var response = await client.GetAsync($"/api/v1/users/{SpeakHaloWebApplicationFactory.AdminUserId}");

        // Assert - The Users controller returns Forbid() for non-self, non-admin access
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Users_ChangeOtherUserPassword_Returns403()
    {
        // Arrange - Owner tries to change Admin's password
        var client = await AuthHelper.LoginAsOwnerAsync(_factory);
        var payload = new { CurrentPassword = "anything", NewPassword = "NewPassword123!" };

        // Act
        var response = await client.PostAsJsonAsync(
            $"/api/v1/users/{SpeakHaloWebApplicationFactory.AdminUserId}/password", payload);

        // Assert - The Users controller returns Forbid() for non-self password changes
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ───────────────────────────────────────────────────────────────
    // Unauthenticated access
    // ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task ProtectedEndpoint_NoToken_Returns401()
    {
        // Arrange
        var client = AuthHelper.CreateAnonymousClient(_factory);

        // Act
        var response = await client.GetAsync("/api/v1/calls/user/calls");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ProtectedEndpoint_ExpiredToken_Returns401()
    {
        // Arrange
        var client = AuthHelper.CreateExpiredTokenClient(_factory);

        // Act
        var response = await client.GetAsync("/api/v1/calls/user/calls");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ProtectedEndpoint_MalformedToken_Returns401()
    {
        // Arrange
        var client = AuthHelper.CreateMalformedTokenClient(_factory);

        // Act
        var response = await client.GetAsync("/api/v1/calls/user/calls");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ProtectedEndpoint_WrongKeyToken_Returns401()
    {
        // Arrange
        var client = AuthHelper.CreateWrongKeyTokenClient(_factory);

        // Act
        var response = await client.GetAsync("/api/v1/calls/user/calls");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PublicEndpoint_NoToken_Returns200()
    {
        // Arrange
        var client = AuthHelper.CreateAnonymousClient(_factory);

        // Act
        var response = await client.GetAsync("/api/v1/subscriptions/plans");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AdminEndpoint_NoToken_Returns401()
    {
        // Arrange
        var client = AuthHelper.CreateAnonymousClient(_factory);

        // Act
        var response = await client.GetAsync("/api/v1/admin/dashboard/overview");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
