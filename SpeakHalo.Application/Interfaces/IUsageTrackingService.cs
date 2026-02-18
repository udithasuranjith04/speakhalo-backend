namespace SpeakHalo.Application.Interfaces;

public interface IUsageTrackingService
{
    /// <summary>
    /// Track AI voice minutes used
    /// </summary>
    Task<UsageTrackingResult> TrackAIMinutesAsync(string userId, int minutesUsed);

    /// <summary>
    /// Track WhatsApp message sent
    /// </summary>
    Task<UsageTrackingResult> TrackWhatsAppMessageAsync(string userId);

    /// <summary>
    /// Track SMS message sent
    /// </summary>
    Task<UsageTrackingResult> TrackSMSMessageAsync(string userId);

    /// <summary>
    /// Track image analysis with soft limit enforcement
    /// </summary>
    Task<UsageTrackingResult> TrackImageAnalysisAsync(string userId, string imageUrl);

    /// <summary>
    /// Check image quota status (returns whether at limit, in grace period, or blocked)
    /// </summary>
    Task<ImageQuotaStatus> CheckImageQuotaAsync(string userId);

    /// <summary>
    /// Get current usage for a user
    /// </summary>
    Task<UserUsageReport> GetUsageReportAsync(string userId);

    /// <summary>
    /// Check if user has remaining quota for a service
    /// </summary>
    Task<bool> HasRemainingQuotaAsync(string userId, UsageType usageType, int amount = 1);

    /// <summary>
    /// Reset monthly usage for all users (called by background job)
    /// </summary>
    Task ResetMonthlyUsageAsync();

    /// <summary>
    /// Send usage warning notification
    /// </summary>
    Task SendUsageWarningAsync(string userId, UsageType usageType, int percentageUsed);
}

public enum UsageType
{
    AIMinutes,
    WhatsAppMessages,
    SMSMessages,
    Images
}

public class UsageTrackingResult
{
    public bool Success { get; set; }
    public int RemainingQuota { get; set; }
    public string Message { get; set; } = string.Empty;
    public int PercentageUsed { get; set; }
}

public class UserUsageReport
{
    public string UserId { get; set; } = string.Empty;
    public string Plan { get; set; } = string.Empty;

    // AI Minutes
    public int AIMinutesIncluded { get; set; }
    public int AIMinutesUsed { get; set; }
    public int AIMinutesRemaining { get; set; }

    // WhatsApp
    public int WhatsAppMessagesIncluded { get; set; }
    public int WhatsAppMessagesUsed { get; set; }
    public int WhatsAppMessagesRemaining { get; set; }

    // SMS
    public int SMSMessagesIncluded { get; set; }
    public int SMSMessagesUsed { get; set; }
    public int SMSMessagesRemaining { get; set; }

    // Images
    public int ImagesIncluded { get; set; }
    public int ImagesUsed { get; set; }
    public int ImagesRemaining { get; set; }
    public int ImagesGracePeriod { get; set; } // 50% grace = half of included
    public bool ImagesSoftLimitReached { get; set; } // At 100%
    public bool ImagesHardLimitReached { get; set; } // At 150%
    public string ImageQuotaStatus { get; set; } = string.Empty; // "ok", "warning", "grace", "blocked"

    // Billing Cycle
    public DateTime BillingCycleStartDate { get; set; }
    public DateTime NextBillingDate { get; set; }

    // Warnings
    public bool NeedsWarning { get; set; }
    public List<string> WarningMessages { get; set; } = new();
}

public class ImageQuotaStatus
{
    public bool CanProcess { get; set; }
    public string Status { get; set; } = string.Empty; // "ok", "warning", "grace", "blocked"
    public int Used { get; set; }
    public int Included { get; set; }
    public int Remaining { get; set; }
    public int GracePeriodLimit { get; set; } // 150% of included
    public int PercentageUsed { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool ShouldShowWarning { get; set; }
    public bool ShouldShowUpgrade { get; set; }
}
