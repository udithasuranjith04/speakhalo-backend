using SpeakHalo.Domain.Enums;

namespace SpeakHalo.Domain.Entities;

/// <summary>
/// Tracks automated follow-up messages sent after voice calls
/// </summary>
public class CallFollowUp : BaseEntity
{
    public string CallId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string? CustomerId { get; set; }

    // Follow-up Details
    public string Channel { get; set; } = "whatsapp"; // whatsapp, sms
    public string RecipientPhone { get; set; } = string.Empty;
    public CallOutcome? CallOutcome { get; set; }
    public bool AppointmentScheduled { get; set; } = false;
    public bool QuoteRequested { get; set; } = false;

    // Message Details
    public string MessageTemplate { get; set; } = string.Empty; // Template name used
    public string MessageContent { get; set; } = string.Empty; // Actual message sent
    public string? PersonalizationData { get; set; } // JSON with personalization data

    // Timing
    public DateTime ScheduledFor { get; set; } // When to send (usually CallEndTime + 5 minutes)
    public DateTime? SentAt { get; set; } // When actually sent
    public int DelayMinutes { get; set; } = 5; // Delay after call ends (default 5 min)

    // Status
    public string Status { get; set; } = "pending"; // pending, sent, failed, cancelled
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; } = 0;
    public DateTime? LastRetryAt { get; set; }

    // Delivery Tracking
    public string? WhatsAppMessageId { get; set; } // Message ID from WhatsApp
    public string? SMSMessageId { get; set; } // Message ID from Twilio SMS
    public string? DeliveryStatus { get; set; } // delivered, read, failed
    public DateTime? DeliveredAt { get; set; }
    public DateTime? ReadAt { get; set; }

    // Response Tracking
    public bool CustomerResponded { get; set; } = false;
    public DateTime? RespondedAt { get; set; }
    public string? ResponseSummary { get; set; }

    // Navigation Properties
    public virtual Call Call { get; set; } = null!;
    public virtual User User { get; set; } = null!;
    public virtual Customer? Customer { get; set; }
}
