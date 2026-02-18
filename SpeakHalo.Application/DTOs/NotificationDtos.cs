namespace SpeakHalo.Application.DTOs;

public record CreateNotificationRequest
{
    public required string UserId { get; init; }
    public required string Title { get; init; }
    public required string Message { get; init; }
    public required string Type { get; init; }
    public string? ActionUrl { get; init; }
    public string? Metadata { get; init; }
}

public record NotificationResponse
{
    public required string Id { get; init; }
    public required string UserId { get; init; }
    public required string Title { get; init; }
    public required string Message { get; init; }
    public required string Type { get; init; }
    public string? ActionUrl { get; init; }
    public string? Metadata { get; init; }
    public bool IsRead { get; init; }
    public DateTime? ReadAt { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record NotificationPreferences
{
    public bool EmailNotifications { get; init; } = true;
    public bool SmsNotifications { get; init; } = true;
    public bool PushNotifications { get; init; } = true;
    public bool AppointmentReminders { get; init; } = true;
    public bool QuoteUpdates { get; init; } = true;
    public bool CallSummaries { get; init; } = true;
}
