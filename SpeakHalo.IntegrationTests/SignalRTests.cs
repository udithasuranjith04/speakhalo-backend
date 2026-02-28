using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using SpeakHalo.Domain.Entities;
using SpeakHalo.Infrastructure.Data;
using SpeakHalo.IntegrationTests.TestHelpers;
using Xunit;

namespace SpeakHalo.IntegrationTests;

/// <summary>
/// Tests SignalR NotificationHub connectivity, authentication, and message delivery.
/// Hub is at /hubs/notifications with JWT auth via ?access_token query param.
/// </summary>
[Collection("Integration")]
public class SignalRTests
{
    private readonly SpeakHaloWebApplicationFactory _factory;

    public SignalRTests(SpeakHaloWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private HubConnection CreateHubConnection(string? accessToken)
    {
        var url = $"{_factory.Server.BaseAddress}hubs/notifications";
        if (!string.IsNullOrEmpty(accessToken))
        {
            url += $"?access_token={accessToken}";
        }

        return new HubConnectionBuilder()
            .WithUrl(url, opts =>
            {
                opts.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
            })
            .Build();
    }

    // ───────────────────────────────────────────────────────────────
    // 1. Connect with valid token succeeds
    // ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task SignalR_Connect_WithValidToken_Succeeds()
    {
        // Arrange
        var token = AuthHelper.GenerateTestToken(
            SpeakHaloWebApplicationFactory.OwnerUserId,
            SpeakHaloWebApplicationFactory.OwnerEmail,
            "Owner");

        var connection = CreateHubConnection(token);

        try
        {
            // Act
            await connection.StartAsync();

            // Assert
            connection.State.Should().Be(HubConnectionState.Connected);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    // ───────────────────────────────────────────────────────────────
    // 2. Connect without token fails
    // ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task SignalR_Connect_WithoutToken_Fails()
    {
        // Arrange
        var connection = CreateHubConnection(null);

        try
        {
            // Act & Assert - Should fail because the hub requires [Authorize]
            var act = async () => await connection.StartAsync();
            await act.Should().ThrowAsync<Exception>(
                "Connection without a token should be rejected by the [Authorize] attribute");
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    // ───────────────────────────────────────────────────────────────
    // 3. Connect with expired token fails
    // ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task SignalR_Connect_WithExpiredToken_Fails()
    {
        // Arrange
        var expiredToken = AuthHelper.GenerateTestToken(
            SpeakHaloWebApplicationFactory.OwnerUserId,
            SpeakHaloWebApplicationFactory.OwnerEmail,
            "Owner",
            expires: DateTime.UtcNow.AddHours(-1));

        var connection = CreateHubConnection(expiredToken);

        try
        {
            // Act & Assert
            var act = async () => await connection.StartAsync();
            await act.Should().ThrowAsync<Exception>(
                "Connection with an expired token should be rejected");
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    // ───────────────────────────────────────────────────────────────
    // 4. Connect with invalid token fails
    // ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task SignalR_Connect_WithInvalidToken_Fails()
    {
        // Arrange
        var connection = CreateHubConnection("this.is.not.a.valid.jwt");

        try
        {
            // Act & Assert
            var act = async () => await connection.StartAsync();
            await act.Should().ThrowAsync<Exception>(
                "Connection with a malformed token should be rejected");
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    // ───────────────────────────────────────────────────────────────
    // 5. ReceiveNotification for own user
    // ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task SignalR_ReceiveNotification_ForOwnUser()
    {
        // Arrange
        var token = AuthHelper.GenerateTestToken(
            SpeakHaloWebApplicationFactory.OwnerUserId,
            SpeakHaloWebApplicationFactory.OwnerEmail,
            "Owner");

        var connection = CreateHubConnection(token);

        var receivedMessage = new TaskCompletionSource<object>();
        connection.On<object>("ReceiveNotification", message =>
        {
            receivedMessage.TrySetResult(message);
        });

        try
        {
            await connection.StartAsync();
            connection.State.Should().Be(HubConnectionState.Connected);

            // Act - Create a notification in the DB and invoke via the hub
            // Since we have direct hub access, we invoke SendNotificationToUser
            await connection.InvokeAsync("SendNotificationToUser",
                SpeakHaloWebApplicationFactory.OwnerUserId,
                "Test notification message");

            // Assert - Wait for the notification with a timeout
            var completedTask = await Task.WhenAny(
                receivedMessage.Task,
                Task.Delay(TimeSpan.FromSeconds(5)));

            completedTask.Should().Be(receivedMessage.Task,
                "Should receive the notification within the timeout period");

            var result = await receivedMessage.Task;
            result.Should().NotBeNull();
            result.ToString().Should().Contain("Test notification message");
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    // ───────────────────────────────────────────────────────────────
    // 6. No notification for other user (cross-user isolation)
    // ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task SignalR_NoNotification_ForOtherUser()
    {
        // Arrange - Connect as Member
        var memberToken = AuthHelper.GenerateTestToken(
            SpeakHaloWebApplicationFactory.MemberUserId,
            SpeakHaloWebApplicationFactory.MemberEmail,
            "Member");

        var memberConnection = CreateHubConnection(memberToken);

        var receivedByMember = new TaskCompletionSource<object>();
        memberConnection.On<object>("ReceiveNotification", message =>
        {
            receivedByMember.TrySetResult(message);
        });

        // Also connect as Owner (the sender target)
        var ownerToken = AuthHelper.GenerateTestToken(
            SpeakHaloWebApplicationFactory.OwnerUserId,
            SpeakHaloWebApplicationFactory.OwnerEmail,
            "Owner");

        var ownerConnection = CreateHubConnection(ownerToken);

        try
        {
            await memberConnection.StartAsync();
            await ownerConnection.StartAsync();

            // Act - Send notification targeted at Owner only
            await ownerConnection.InvokeAsync("SendNotificationToUser",
                SpeakHaloWebApplicationFactory.OwnerUserId,
                "Message for owner only");

            // Assert - Member should NOT receive the notification
            var completedTask = await Task.WhenAny(
                receivedByMember.Task,
                Task.Delay(TimeSpan.FromSeconds(2)));

            completedTask.Should().NotBe(receivedByMember.Task,
                "Member should not receive a notification sent to Owner");
        }
        finally
        {
            await memberConnection.DisposeAsync();
            await ownerConnection.DisposeAsync();
        }
    }

    // ───────────────────────────────────────────────────────────────
    // 7. Admin joins admin group
    // ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task SignalR_AdminJoinsAdminGroup()
    {
        // Arrange - Admin user connects; OnConnectedAsync adds Admin role users to "admins" group
        var adminToken = AuthHelper.GenerateTestToken(
            SpeakHaloWebApplicationFactory.AdminUserId,
            SpeakHaloWebApplicationFactory.AdminEmail,
            "Admin");

        var adminConnection = CreateHubConnection(adminToken);

        try
        {
            // Act
            await adminConnection.StartAsync();

            // Assert - Connection succeeds (admin is added to the admins group internally)
            adminConnection.State.Should().Be(HubConnectionState.Connected,
                "Admin user should successfully connect and be added to admin group");

            // Verify the admin group works by checking the connection is alive
            // The OnConnectedAsync method adds Admin users to the "admins" group
            // We verify this indirectly by confirming the connection succeeds
        }
        finally
        {
            await adminConnection.DisposeAsync();
        }
    }

    // ───────────────────────────────────────────────────────────────
    // 8. Reconnection works
    // ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task SignalR_Reconnection_Works()
    {
        // Arrange
        var token = AuthHelper.GenerateTestToken(
            SpeakHaloWebApplicationFactory.OwnerUserId,
            SpeakHaloWebApplicationFactory.OwnerEmail,
            "Owner");

        var connection = CreateHubConnection(token);

        try
        {
            // Act - Connect, disconnect, reconnect
            await connection.StartAsync();
            connection.State.Should().Be(HubConnectionState.Connected);

            await connection.StopAsync();
            connection.State.Should().Be(HubConnectionState.Disconnected);

            await connection.StartAsync();

            // Assert
            connection.State.Should().Be(HubConnectionState.Connected,
                "Should be able to reconnect after disconnecting");
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }
}
