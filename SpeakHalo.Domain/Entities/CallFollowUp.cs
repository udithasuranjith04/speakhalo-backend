using SpeakHalo.Domain.Enums;

namespace SpeakHalo.Domain.Entities;

public class CallFollowUp : BaseEntity
{
    public string CallId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string? CustomerId { get; set; }

    public string Channel { get; set; } = "whatsapp";
    public string RecipientPhone { get; set; } = string.Empty;
    public CallOutcome? CallOutcome { get; set; }
    public bool AppointmentScheduled { get; set; } = false;
    public bool QuoteRequested { get; set; } = false;

    public string MessageTemplate { get; set; } = string.Empty;
    public string MessageContent { get; set; } = string.Empty;
    public string? PersonalizationData { get; set; }

    public DateTime ScheduledFor { get; set; }
    public DateTime? SentAt { get; set; }
    public int DelayMinutes { get; set; } = 5;

    public string Status { get; set; } = "pending";
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; } = 0;
    public DateTime? LastRetryAt { get; set; }

    public string? WhatsAppMessageId { get; set; }
    public string? SMSMessageId { get; set; }
    public string? DeliveryStatus { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? ReadAt { get; set; }

    public bool CustomerResponded { get; set; } = false;
    public DateTime? RespondedAt { get; set; }
    public string? ResponseSummary { get; set; }

    public virtual Call Call { get; set; } = null!;
    public virtual User User { get; set; } = null!;
    public virtual Customer? Customer { get; set; }
}
