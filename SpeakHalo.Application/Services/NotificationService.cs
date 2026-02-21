using Microsoft.Extensions.Logging;
using SpeakHalo.Application.DTOs;
using SpeakHalo.Application.Interfaces;
using SpeakHalo.Domain.Entities;
using SpeakHalo.Domain.Interfaces.Repositories;
using SpeakHalo.Shared.Constants;
using SpeakHalo.Shared.Models;

namespace SpeakHalo.Application.Services;

public class NotificationService : INotificationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(IUnitOfWork unitOfWork, ILogger<NotificationService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<NotificationResponse>> CreateNotificationAsync(CreateNotificationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var notification = new Notification
            {
                UserId = request.UserId,
                Title = request.Title,
                Message = request.Message,
                Type = request.Type,
                ActionUrl = request.ActionUrl,
                Metadata = request.Metadata,
                IsRead = false
            };

            await _unitOfWork.Repository<Notification>().AddAsync(notification, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var response = MapToResponse(notification);

            return Result<NotificationResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create notification for user {UserId}", request.UserId);
            return Result<NotificationResponse>.Failure($"Failed to create notification: {ex.Message}");
        }
    }

    public async Task<Result<NotificationResponse>> GetNotificationByIdAsync(string notificationId, CancellationToken cancellationToken = default)
    {
        try
        {
            var notification = await _unitOfWork.Repository<Notification>().GetByIdAsync(notificationId, cancellationToken);
            if (notification == null)
                return Result<NotificationResponse>.Failure(ErrorMessages.NotFound("Notification"));

            return Result<NotificationResponse>.Success(MapToResponse(notification));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get notification {NotificationId}", notificationId);
            return Result<NotificationResponse>.Failure($"Failed to get notification: {ex.Message}");
        }
    }

    public async Task<Result<PaginatedList<NotificationResponse>>> GetUserNotificationsAsync(string userId, int pageNumber = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        try
        {
            var notifications = await _unitOfWork.Repository<Notification>().FindAsync(n => n.UserId == userId, cancellationToken);
            var orderedNotifications = notifications.OrderByDescending(n => n.CreatedAt).ToList();

            var totalCount = orderedNotifications.Count;
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 20;

            var items = orderedNotifications
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(MapToResponse)
                .ToList();

            var result = new PaginatedList<NotificationResponse>(items, totalCount, pageNumber, pageSize);

            return Result<PaginatedList<NotificationResponse>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get notifications for user {UserId}", userId);
            return Result<PaginatedList<NotificationResponse>>.Failure($"Failed to get notifications: {ex.Message}");
        }
    }

    public async Task<Result<List<NotificationResponse>>> GetUnreadNotificationsAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var notifications = await _unitOfWork.Repository<Notification>().FindAsync(n => n.UserId == userId && !n.IsRead, cancellationToken);
            var responses = notifications.OrderByDescending(n => n.CreatedAt).Select(MapToResponse).ToList();

            return Result<List<NotificationResponse>>.Success(responses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get unread notifications for user {UserId}", userId);
            return Result<List<NotificationResponse>>.Failure($"Failed to get unread notifications: {ex.Message}");
        }
    }

    public async Task<Result<int>> GetUnreadCountAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var count = await _unitOfWork.Repository<Notification>().CountAsync(n => n.UserId == userId && !n.IsRead, cancellationToken);
            return Result<int>.Success(count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get unread count for user {UserId}", userId);
            return Result<int>.Failure($"Failed to get unread count: {ex.Message}");
        }
    }

    public async Task<Result<bool>> MarkAsReadAsync(string notificationId, CancellationToken cancellationToken = default)
    {
        try
        {
            var notification = await _unitOfWork.Repository<Notification>().GetByIdAsync(notificationId, cancellationToken);
            if (notification == null)
                return Result<bool>.Failure(ErrorMessages.NotFound("Notification"));

            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;

            _unitOfWork.Repository<Notification>().Update(notification);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to mark notification {NotificationId} as read", notificationId);
            return Result<bool>.Failure($"Failed to mark notification as read: {ex.Message}");
        }
    }

    public async Task<Result<bool>> MarkAllAsReadAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var notifications = await _unitOfWork.Repository<Notification>().FindAsync(n => n.UserId == userId && !n.IsRead, cancellationToken);
            foreach (var notification in notifications)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
                _unitOfWork.Repository<Notification>().Update(notification);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to mark all notifications as read for user {UserId}", userId);
            return Result<bool>.Failure($"Failed to mark all as read: {ex.Message}");
        }
    }

    public async Task<Result<bool>> DeleteNotificationAsync(string notificationId, CancellationToken cancellationToken = default)
    {
        try
        {
            var notification = await _unitOfWork.Repository<Notification>().GetByIdAsync(notificationId, cancellationToken);
            if (notification == null)
                return Result<bool>.Failure(ErrorMessages.NotFound("Notification"));

            _unitOfWork.Repository<Notification>().Remove(notification);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete notification {NotificationId}", notificationId);
            return Result<bool>.Failure($"Failed to delete notification: {ex.Message}");
        }
    }

    public async Task<Result<bool>> SendAppointmentReminderAsync(string appointmentId, CancellationToken cancellationToken = default)
    {
        try
        {
            var appointment = await _unitOfWork.Appointments.GetByIdAsync(appointmentId, cancellationToken);
            if (appointment == null)
                return Result<bool>.Failure(ErrorMessages.NotFound("Appointment"));

            var notification = new Notification
            {
                UserId = appointment.UserId,
                Title = "Appointment Reminder",
                Message = $"You have an appointment scheduled for {appointment.ScheduledAt:MMMM dd, yyyy 'at' h:mm tt}",
                Type = "appointment_reminder",
                ActionUrl = $"/appointments/{appointmentId}",
                IsRead = false
            };

            await _unitOfWork.Repository<Notification>().AddAsync(notification, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send appointment reminder for {AppointmentId}", appointmentId);
            return Result<bool>.Failure($"Failed to send appointment reminder: {ex.Message}");
        }
    }

    public async Task<Result<bool>> SendQuoteNotificationAsync(string quoteId, string notificationType, CancellationToken cancellationToken = default)
    {
        try
        {
            var quote = await _unitOfWork.Quotes.GetByIdAsync(quoteId, cancellationToken);
            if (quote == null)
                return Result<bool>.Failure(ErrorMessages.NotFound("Quote"));

            var title = notificationType switch
            {
                "sent" => "Quote Sent",
                "viewed" => "Quote Viewed",
                "accepted" => "Quote Accepted",
                "rejected" => "Quote Rejected",
                _ => "Quote Update"
            };

            var message = notificationType switch
            {
                "sent" => $"Quote {quote.QuoteNumber} has been sent to the customer",
                "viewed" => $"Quote {quote.QuoteNumber} has been viewed by the customer",
                "accepted" => $"Quote {quote.QuoteNumber} has been accepted!",
                "rejected" => $"Quote {quote.QuoteNumber} has been rejected",
                _ => $"Quote {quote.QuoteNumber} has been updated"
            };

            var notification = new Notification
            {
                UserId = quote.UserId,
                Title = title,
                Message = message,
                Type = $"quote_{notificationType}",
                ActionUrl = $"/quotes/{quoteId}",
                IsRead = false
            };

            await _unitOfWork.Repository<Notification>().AddAsync(notification, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send quote notification for {QuoteId}", quoteId);
            return Result<bool>.Failure($"Failed to send quote notification: {ex.Message}");
        }
    }

    private static NotificationResponse MapToResponse(Notification notification)
    {
        return new NotificationResponse
        {
            Id = notification.Id,
            UserId = notification.UserId,
            Title = notification.Title,
            Message = notification.Message,
            Type = notification.Type,
            ActionUrl = notification.ActionUrl,
            Metadata = notification.Metadata,
            IsRead = notification.IsRead,
            ReadAt = notification.ReadAt,
            CreatedAt = notification.CreatedAt
        };
    }
}
