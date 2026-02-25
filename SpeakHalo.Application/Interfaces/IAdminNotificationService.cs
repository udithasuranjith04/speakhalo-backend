namespace SpeakHalo.Application.Interfaces;

public interface IAdminNotificationService
{
    Task NotifyNewUserSignupAsync(string userId, string email, string? name);
    Task NotifySubscriptionChangeAsync(string userId, string email, string oldPlan, string newPlan);
    Task NotifyPaymentFailedAsync(string userId, string email, string? reason);
    Task NotifyUserSuspendedAsync(string userId, string email, string? reason, string adminId);
    Task NotifyUserActivatedAsync(string userId, string email, string adminId);
    Task NotifyTrialExpiringAsync(string userId, string email, DateTime expiresAt);
    Task NotifyUserDeletedAsync(string userId, string email, string adminId);
    Task NotifySystemAlertAsync(string title, string message, string severity);
}
