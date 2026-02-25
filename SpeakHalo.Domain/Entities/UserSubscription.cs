namespace SpeakHalo.Domain.Entities;

/// <summary>
/// User subscription and usage tracking. All limits set by admin per user.
/// There are no self-service plans or tiers -- admin controls everything.
/// The Plan field is always "cleaning" for active users.
/// </summary>
public class UserSubscription : BaseEntity
{
    public string UserId { get; set; } = string.Empty;

    public string? Plan { get; set; }
    public string? EnabledFeatures { get; set; }
    public int MaxPhoneNumbers { get; set; } = 1;

    public bool RequiresPayment { get; set; } = false;
    public bool HasActivePayment { get; set; } = false;
    public string? SelectedPlanAtRegistration { get; set; }

    public int AIMinutesUsedThisMonth { get; set; } = 0;
    public int WhatsAppMessagesUsedThisMonth { get; set; } = 0;
    public int SMSMessagesUsedThisMonth { get; set; } = 0;
    public int ImagesUsedThisMonth { get; set; } = 0;

    public int VoiceMinutesUsed { get; set; } = 0;
    public int WhatsAppMessagesUsed { get; set; } = 0;
    public int BonusMinutes { get; set; } = 0;
    public int BonusWhatsAppMessages { get; set; } = 0;

    public int RolloverVoiceMinutes { get; set; } = 0;
    public int RolloverWhatsAppMessages { get; set; } = 0;
    public DateTime? RolloverExpiresAt { get; set; }

    public int AIMinutesIncluded { get; set; } = 0;
    public int WhatsAppMessagesIncluded { get; set; } = 0;
    public int SMSMessagesIncluded { get; set; } = 0;
    public int ImagesIncluded { get; set; } = 0;

    public int? MonthlyMinutesLimit { get; set; }
    public int? CustomWhatsAppLimit { get; set; }
    public int? CustomSmsLimit { get; set; }

    public decimal AIOverageRate { get; set; } = 0m;
    public decimal WhatsAppOverageRate { get; set; } = 0m;
    public decimal SMSOverageRate { get; set; } = 0m;
    public decimal ImageOverageRate { get; set; } = 0m;

    public DateTime BillingCycleStartDate { get; set; } = DateTime.UtcNow;
    public DateTime NextBillingDate { get; set; } = DateTime.UtcNow.AddMonths(1);
    public decimal OverageChargesThisMonth { get; set; } = 0m;

    /// <summary>If true, block usage when limits are exceeded. Admin controls this per user.</summary>
    public bool EnforceHardLimits { get; set; } = true;

    public decimal? CustomPriceOverride { get; set; }
    public int? CustomPriceDurationMonths { get; set; }
    public DateTime? CustomPriceExpiresAt { get; set; }
    public string? CustomPriceReason { get; set; }
    public bool IsFounderCustomer { get; set; } = false;
    public int? FounderNumber { get; set; }

    public bool IsFounderPriceLocked { get; set; } = false;
    public decimal? LockedMonthlyPrice { get; set; }
    public DateTime? PriceLockedAt { get; set; }
    public string? PriceLockReason { get; set; }

    public bool IsTemporarilySuspended { get; set; } = false;
    public string? SuspendedReason { get; set; }
    public DateTime? SuspendedAt { get; set; }
    public string? SuspendedByAdminId { get; set; }

    public decimal? CustomMonthlyFee { get; set; }
    public decimal? CustomPerMinuteRate { get; set; }
    public decimal? CustomVoiceMinuteRate { get; set; }
    public decimal? CustomTransferMinuteRate { get; set; }
    public decimal? CustomWhatsAppMessageRate { get; set; }
    public decimal? CustomSmsMessageRate { get; set; }
    public decimal? SetupFee { get; set; }

    public string? BillingNotes { get; set; }
    public string? InvoiceHistory { get; set; }
    public DateTime? PricingNotifiedAt { get; set; }

    public string? PricingPlanId { get; set; }
    public DateTime? TrialEndsAt { get; set; }
    public bool IsOnTrial { get; set; } = false;

    // Paddle Billing fields
    public string? PaddleCustomerId { get; set; }
    public string? PaddleSubscriptionId { get; set; }
    public string? PaddlePriceId { get; set; }
    public string? PaddleSubscriptionStatus { get; set; }
    public DateTime? PaddleSubscriptionCreatedAt { get; set; }
    public DateTime? PaddleCanceledAt { get; set; }
    public DateTime? PaddleCurrentPeriodEnd { get; set; }
    public string? PaddleUpdatePaymentMethodUrl { get; set; }
    public string? PaddleCancelUrl { get; set; }
    public DateTime? FirstPaymentAt { get; set; }
    public DateTime? RefundedAt { get; set; }
    public string? RefundReason { get; set; }

    public uint RowVersion { get; set; }

    public virtual User User { get; set; } = null!;
    public virtual PricingPlan? PricingPlan { get; set; }
}
