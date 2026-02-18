namespace SpeakHalo.Domain.Entities;

/// <summary>
/// Tracks processed webhook events to prevent duplicate processing (idempotency).
/// Stores webhook metadata for auditing and debugging.
/// </summary>
public class WebhookEvent : BaseEntity
{
    /// <summary>
    /// Unique identifier from the webhook provider (e.g., Twilio event ID).
    /// Used for idempotency checking.
    /// </summary>
    public string EventId { get; set; } = string.Empty;

    /// <summary>
    /// Type of webhook event (e.g., "order_created", "subscription_updated").
    /// </summary>
    public string EventName { get; set; } = string.Empty;

    /// <summary>
    /// Source of the webhook (e.g., "twilio").
    /// </summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// When the webhook was successfully processed.
    /// </summary>
    public DateTime ProcessedAt { get; set; }

    /// <summary>
    /// Complete raw JSON payload from the webhook for debugging.
    /// Useful for investigating issues or replaying events.
    /// </summary>
    public string? RawPayload { get; set; }

    /// <summary>
    /// Whether the webhook was processed successfully.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Error message if processing failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Number of times this webhook was retried by the provider.
    /// </summary>
    public int RetryCount { get; set; }

    /// <summary>
    /// HTTP status code returned to the webhook provider.
    /// </summary>
    public int? ResponseStatusCode { get; set; }
}
