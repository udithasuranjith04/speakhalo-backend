namespace SpeakHalo.Domain.Entities;

/// <summary>
/// Tracks usage warning notifications sent to users
/// </summary>
public class UsageWarning : BaseEntity
{
    /// <summary>
    /// User who received the warning
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Resource that triggered the warning (ai_minutes, whatsapp, sms, images)
    /// </summary>
    public string ResourceType { get; set; } = string.Empty;

    /// <summary>
    /// Percentage of resource used when warning triggered (80, 90, 100, etc.)
    /// </summary>
    public int PercentageUsed { get; set; }

    /// <summary>
    /// Amount used at time of warning
    /// </summary>
    public int AmountUsed { get; set; }

    /// <summary>
    /// Limit at time of warning
    /// </summary>
    public int AmountLimit { get; set; }

    /// <summary>
    /// Warning severity: warning (80%), critical (90%), exceeded (100%+)
    /// </summary>
    public string Severity { get; set; } = string.Empty;

    /// <summary>
    /// Warning message sent to user
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Whether email notification was sent
    /// </summary>
    public bool EmailSent { get; set; }

    /// <summary>
    /// When email was sent
    /// </summary>
    public DateTime? EmailSentAt { get; set; }

    /// <summary>
    /// Whether in-app notification was created
    /// </summary>
    public bool InAppNotificationCreated { get; set; }

    /// <summary>
    /// Whether user acknowledged the warning
    /// </summary>
    public bool Acknowledged { get; set; }

    /// <summary>
    /// When user acknowledged the warning
    /// </summary>
    public DateTime? AcknowledgedAt { get; set; }

    /// <summary>
    /// User's plan at time of warning
    /// </summary>
    public string? UserPlan { get; set; }

    /// <summary>
    /// Billing month this warning relates to
    /// </summary>
    public DateTime BillingMonth { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual User User { get; set; } = null!;
}
