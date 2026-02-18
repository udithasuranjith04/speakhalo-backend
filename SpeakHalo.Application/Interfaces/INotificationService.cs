using SpeakHalo.Application.DTOs;
using SpeakHalo.Shared.Models;

namespace SpeakHalo.Application.Interfaces;

public interface INotificationService
{
    Task<Result<NotificationResponse>> CreateNotificationAsync(CreateNotificationRequest request, CancellationToken cancellationToken = default);
    Task<Result<NotificationResponse>> GetNotificationByIdAsync(string notificationId, CancellationToken cancellationToken = default);
    Task<Result<PaginatedList<NotificationResponse>>> GetUserNotificationsAsync(string userId, int pageNumber = 1, int pageSize = 20, CancellationToken cancellationToken = default);
    Task<Result<List<NotificationResponse>>> GetUnreadNotificationsAsync(string userId, CancellationToken cancellationToken = default);
    Task<Result<int>> GetUnreadCountAsync(string userId, CancellationToken cancellationToken = default);
    Task<Result<bool>> MarkAsReadAsync(string notificationId, CancellationToken cancellationToken = default);
    Task<Result<bool>> MarkAllAsReadAsync(string userId, CancellationToken cancellationToken = default);
    Task<Result<bool>> DeleteNotificationAsync(string notificationId, CancellationToken cancellationToken = default);
    Task<Result<bool>> SendAppointmentReminderAsync(string appointmentId, CancellationToken cancellationToken = default);
    Task<Result<bool>> SendQuoteNotificationAsync(string quoteId, string notificationType, CancellationToken cancellationToken = default);
}
