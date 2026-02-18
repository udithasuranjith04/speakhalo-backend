namespace SpeakHalo.Domain.Entities;

/// <summary>
/// Tracks processed webhook events to prevent duplicate processing (idempotency).
/// </summary>
public class WebhookEvent : BaseEntity
{
    /// <summary>Unique identifier from the webhook provider, used for idempotency checking.</summary>
    public string EventId { get; set; } = string.Empty;

    public string EventName { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public DateTime ProcessedAt { get; set; }

    /// <summary>Complete raw JSON payload for debugging and event replay.</summary>
    public string? RawPayload { get; set; }

    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; }
    public int? ResponseStatusCode { get; set; }
}
