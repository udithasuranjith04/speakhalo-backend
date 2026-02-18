namespace SpeakHalo.Domain.Entities;

/// <summary>
/// User subscription and usage tracking. All limits set by admin per user.
/// There are no self-service plans or tiers — admin controls everything.
/// The Plan field is always "cleaning" for active users.
/// Many fields are kept for DB compatibility but are no longer actively used.
/// </summary>
public class UserSubscription : BaseEntity
{
    public string UserId { get; set; } = string.Empty;

    /// <summary>Always "cleaning" for active users. Legacy field kept for DB compatibility.</summary>
    public string? Plan { get; set; }

    /// <summary>Legacy field kept for DB compatibility. Not used — all features available to active users.</summary>
    public string? EnabledFeatures { get; set; }

    /// <summary>Admin sets this per user. Controls max phone numbers the user can provision.</summary>
    public int MaxPhoneNumbers { get; set; } = 1;

    // Account Status (admin-controlled)
    /// <summary>Legacy field. Always false — no self-service payment required.</summary>
    public bool RequiresPayment { get; set; } = false;

    /// <summary>Admin sets this. True = user account is active and can use the system.</summary>
    public bool HasActivePayment { get; set; } = false;

    /// <summary>Legacy field kept for DB compatibility.</summary>
    public string? SelectedPlanAtRegistration { get; set; }

    // Usage Tracking - Current Month
    public int AIMinutesUsedThisMonth { get; set; } = 0;
    public int WhatsAppMessagesUsedThisMonth { get; set; } = 0;
    public int SMSMessagesUsedThisMonth { get; set; } = 0;
    public int ImagesUsedThisMonth { get; set; } = 0;

    // Legacy Usage Tracking (kept for backward compatibility)
    public int VoiceMinutesUsed { get; set; } = 0;
    public int WhatsAppMessagesUsed { get; set; } = 0;
    public int BonusMinutes { get; set; } = 0;
    public int BonusWhatsAppMessages { get; set; } = 0;

    // Rollover
    public int RolloverVoiceMinutes { get; set; } = 0;
    public int RolloverWhatsAppMessages { get; set; } = 0;
    public DateTime? RolloverExpiresAt { get; set; }

    // Plan Limits (set by admin per user)
    public int AIMinutesIncluded { get; set; } = 0;
    public int WhatsAppMessagesIncluded { get; set; } = 0;
    public int SMSMessagesIncluded { get; set; } = 0;
    public int ImagesIncluded { get; set; } = 0;

    // Legacy Limits (kept for backward compatibility)
    public int? MonthlyMinutesLimit { get; set; }
    public int? CustomWhatsAppLimit { get; set; }
    public int? CustomSmsLimit { get; set; }

    // Overage Rates (kept for DB compatibility, no longer used)
    public decimal AIOverageRate { get; set; } = 0m;
    public decimal WhatsAppOverageRate { get; set; } = 0m;
    public decimal SMSOverageRate { get; set; } = 0m;
    public decimal ImageOverageRate { get; set; } = 0m;

    // Billing Cycle
    public DateTime BillingCycleStartDate { get; set; } = DateTime.UtcNow;
    public DateTime NextBillingDate { get; set; } = DateTime.UtcNow.AddMonths(1);
    public decimal OverageChargesThisMonth { get; set; } = 0m;

    // Usage Enforcement
    /// <summary>
    /// If true, block usage when limits are exceeded (hard limit).
    /// Admin controls this per user.
    /// </summary>
    public bool EnforceHardLimits { get; set; } = true;

    // Legacy Founder/VIP Pricing (kept for DB compatibility)
    public decimal? CustomPriceOverride { get; set; }
    public int? CustomPriceDurationMonths { get; set; }
    public DateTime? CustomPriceExpiresAt { get; set; }
    public string? CustomPriceReason { get; set; }
    public bool IsFounderCustomer { get; set; } = false;
    public int? FounderNumber { get; set; }

    // Legacy Founder Price Lock (kept for DB compatibility)
    public bool IsFounderPriceLocked { get; set; } = false;
    public decimal? LockedMonthlyPrice { get; set; }
    public DateTime? PriceLockedAt { get; set; }
    public string? PriceLockReason { get; set; }

    // Temporary Suspension
    public bool IsTemporarilySuspended { get; set; } = false;
    public string? SuspendedReason { get; set; }
    public DateTime? SuspendedAt { get; set; }
    public string? SuspendedByAdminId { get; set; }

    // Legacy Custom Pricing (kept for backward compatibility)
    public decimal? CustomMonthlyFee { get; set; }
    public decimal? CustomPerMinuteRate { get; set; }
    public decimal? CustomVoiceMinuteRate { get; set; }
    public decimal? CustomTransferMinuteRate { get; set; }
    public decimal? CustomWhatsAppMessageRate { get; set; }
    public decimal? CustomSmsMessageRate { get; set; }
    public decimal? SetupFee { get; set; }

    // Admin Notes
    public string? BillingNotes { get; set; }
    public string? InvoiceHistory { get; set; }
    public DateTime? PricingNotifiedAt { get; set; }

    // Pricing Plan Assignment
    /// <summary>FK to PricingPlan used to create this subscription. Null for legacy/manual users.</summary>
    public string? PricingPlanId { get; set; }

    /// <summary>When trial expires. Null = no trial or trial already ended.</summary>
    public DateTime? TrialEndsAt { get; set; }

    /// <summary>True if user is currently on a free trial.</summary>
    public bool IsOnTrial { get; set; } = false;

    // Concurrency token
    public uint RowVersion { get; set; }

    public virtual User User { get; set; } = null!;
    public virtual PricingPlan? PricingPlan { get; set; }
}
