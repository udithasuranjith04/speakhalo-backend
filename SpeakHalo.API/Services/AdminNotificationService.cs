using Microsoft.AspNetCore.SignalR;
using SpeakHalo.API.Hubs;
using SpeakHalo.Application.Interfaces;

namespace SpeakHalo.API.Services;

public class AdminNotificationService : IAdminNotificationService
{
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly ILogger<AdminNotificationService> _logger;

    public AdminNotificationService(
        IHubContext<NotificationHub> hubContext,
        ILogger<AdminNotificationService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task NotifyNewUserSignupAsync(string userId, string email, string? name)
    {
        await SendAdminEventAsync(new
        {
            eventType = "user_signup",
            title = "New User Signup",
            message = $"{name ?? email} has created an account",
            userId,
            email,
            severity = "info",
            actionUrl = $"/admin/users/{userId}",
            timestamp = DateTime.UtcNow
        });
    }

    public async Task NotifySubscriptionChangeAsync(string userId, string email, string oldPlan, string newPlan)
    {
        await SendAdminEventAsync(new
        {
            eventType = "subscription_change",
            title = "Subscription Changed",
            message = $"{email} changed plan from {oldPlan} to {newPlan}",
            userId,
            email,
            severity = "info",
            actionUrl = $"/admin/users/{userId}",
            timestamp = DateTime.UtcNow
        });
    }

    public async Task NotifyPaymentFailedAsync(string userId, string email, string? reason)
    {
        await SendAdminEventAsync(new
        {
            eventType = "payment_failed",
            title = "Payment Failed",
            message = $"Payment failed for {email}{(reason != null ? $": {reason}" : "")}",
            userId,
            email,
            severity = "warning",
            actionUrl = $"/admin/users/{userId}",
            timestamp = DateTime.UtcNow
        });
    }

    public async Task NotifyUserSuspendedAsync(string userId, string email, string? reason, string adminId)
    {
        await SendAdminEventAsync(new
        {
            eventType = "user_suspended",
            title = "User Suspended",
            message = $"{email} has been suspended{(reason != null ? $": {reason}" : "")}",
            userId,
            email,
            severity = "warning",
            actionUrl = $"/admin/users/{userId}",
            timestamp = DateTime.UtcNow
        });
    }

    public async Task NotifyUserActivatedAsync(string userId, string email, string adminId)
    {
        await SendAdminEventAsync(new
        {
            eventType = "user_activated",
            title = "User Activated",
            message = $"{email} has been activated",
            userId,
            email,
            severity = "info",
            actionUrl = $"/admin/users/{userId}",
            timestamp = DateTime.UtcNow
        });
    }

    public async Task NotifyTrialExpiringAsync(string userId, string email, DateTime expiresAt)
    {
        var daysLeft = (expiresAt - DateTime.UtcNow).Days;
        await SendAdminEventAsync(new
        {
            eventType = "trial_expiring",
            title = "Trial Expiring Soon",
            message = $"{email}'s trial expires in {daysLeft} day{(daysLeft != 1 ? "s" : "")}",
            userId,
            email,
            severity = "warning",
            actionUrl = $"/admin/users/{userId}",
            timestamp = DateTime.UtcNow
        });
    }

    public async Task NotifyUserDeletedAsync(string userId, string email, string adminId)
    {
        await SendAdminEventAsync(new
        {
            eventType = "user_deleted",
            title = "User Deleted",
            message = $"{email} has been deleted",
            userId,
            email,
            severity = "error",
            actionUrl = "/admin/users",
            timestamp = DateTime.UtcNow
        });
    }

    public async Task NotifySystemAlertAsync(string title, string message, string severity)
    {
        await SendAdminEventAsync(new
        {
            eventType = "system_alert",
            title,
            message,
            severity,
            actionUrl = "/admin/system",
            timestamp = DateTime.UtcNow
        });
    }

    private async Task SendAdminEventAsync(object eventData)
    {
        try
        {
            await _hubContext.Clients.Group(NotificationHub.AdminGroup)
                .SendAsync("ReceiveAdminEvent", eventData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send admin event via SignalR");
        }
    }
}
