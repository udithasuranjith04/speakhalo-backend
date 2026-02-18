using SpeakHalo.Domain.Enums;

namespace SpeakHalo.Application.DTOs;

public record CreateAppointmentRequest
{
    public required string UserId { get; init; }
    public required string CustomerId { get; init; }
    public required DateTime ScheduledAt { get; init; }
    public int DurationMinutes { get; init; } = 60;
    public ServiceType? ServiceType { get; init; }
    public string? Location { get; init; }
    public string? Notes { get; init; }
}

public record UpdateAppointmentRequest
{
    public DateTime? ScheduledAt { get; init; }
    public int? DurationMinutes { get; init; }
    public AppointmentStatus? Status { get; init; }
    public ServiceType? ServiceType { get; init; }
    public string? Location { get; init; }
    public string? Notes { get; init; }
    public string? CompletionNotes { get; init; }
}

public record AppointmentResponse
{
    public required string Id { get; init; }
    public required string UserId { get; init; }
    public required string CustomerId { get; init; }
    public required DateTime ScheduledAt { get; init; }
    public int DurationMinutes { get; init; }
    public AppointmentStatus Status { get; init; }
    public ServiceType? ServiceType { get; init; }
    public string? Location { get; init; }
    public string? Notes { get; init; }
    public DateTime? CompletedAt { get; init; }
    public string? CompletionNotes { get; init; }
    public DateTime? CancelledAt { get; init; }
    public string? CancellationReason { get; init; }
    public bool ReminderSent { get; init; }
    public DateTime CreatedAt { get; init; }
}
