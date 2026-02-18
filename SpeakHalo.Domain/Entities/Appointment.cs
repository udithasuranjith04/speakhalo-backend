using SpeakHalo.Domain.Enums;

namespace SpeakHalo.Domain.Entities;

public class Appointment : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;

    public DateTime ScheduledAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int DurationMinutes { get; set; } = 60;

    public AppointmentStatus Status { get; set; } = AppointmentStatus.Scheduled;
    public ServiceType ServiceType { get; set; }

    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Location { get; set; }

    public decimal EstimatedCost { get; set; } = 0;
    public decimal? ActualCost { get; set; }

    public string? GoogleCalendarEventId { get; set; }

    public bool ReminderSent24h { get; set; } = false;
    public bool ReminderSent2h { get; set; } = false;
    public DateTime? Reminder24hSentAt { get; set; }
    public DateTime? Reminder2hSentAt { get; set; }

    public string? RecurrenceRule { get; set; }
    public string? ParentAppointmentId { get; set; }

    public string? Notes { get; set; }
    public string? CompletionNotes { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }
    public bool ReminderSent { get; set; } = false;

    // Concurrency token
    public uint RowVersion { get; set; }

    public virtual User User { get; set; } = null!;
    public virtual Customer Customer { get; set; } = null!;
}
