namespace SpeakHalo.Domain.Entities;

/// <summary>
/// Archives usage data for each completed billing cycle
/// </summary>
public class MonthlyUsageArchive : BaseEntity
{
    /// <summary>
    /// User this archive belongs to
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Billing month this archive represents (first day of the month)
    /// </summary>
    public DateTime BillingMonth { get; set; }

    /// <summary>
    /// Start date of billing period
    /// </summary>
    public DateTime PeriodStart { get; set; }

    /// <summary>
    /// End date of billing period
    /// </summary>
    public DateTime PeriodEnd { get; set; }

    /// <summary>
    /// User's plan during this period
    /// </summary>
    public string Plan { get; set; } = string.Empty;

    public int AIMinutesUsed { get; set; }
    public int AIMinutesIncluded { get; set; }
    public int AIMinutesOverage { get; set; }
    public decimal AIOverageRate { get; set; }
    public decimal AIOverageCost { get; set; }

    public int WhatsAppMessagesUsed { get; set; }
    public int WhatsAppMessagesIncluded { get; set; }
    public int WhatsAppMessagesOverage { get; set; }
    public decimal WhatsAppOverageRate { get; set; }
    public decimal WhatsAppOverageCost { get; set; }

    public int SMSMessagesUsed { get; set; }
    public int SMSMessagesIncluded { get; set; }
    public int SMSMessagesOverage { get; set; }
    public decimal SMSOverageRate { get; set; }
    public decimal SMSOverageCost { get; set; }

    public int ImagesUsed { get; set; }
    public int ImagesIncluded { get; set; }
    public int ImagesOverage { get; set; }
    public decimal ImagesOverageRate { get; set; }
    public decimal ImagesOverageCost { get; set; }

    public decimal TotalOverageCost { get; set; }
    public int TotalCalls { get; set; }
    public int TotalCustomersContacted { get; set; }

    /// <summary>
    /// Whether monthly summary email was sent
    /// </summary>
    public bool SummaryEmailSent { get; set; }

    /// <summary>
    /// When summary email was sent
    /// </summary>
    public DateTime? SummaryEmailSentAt { get; set; }

    /// <summary>
    /// Whether usage was successfully reset after archiving
    /// </summary>
    public bool UsageReset { get; set; }

    /// <summary>
    /// When usage was reset
    /// </summary>
    public DateTime? UsageResetAt { get; set; }

    public virtual User User { get; set; } = null!;
}
