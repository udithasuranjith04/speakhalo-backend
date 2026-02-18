namespace SpeakHalo.Domain.Entities;

/// <summary>
/// Reusable pricing plan template. Admin creates plans and assigns them to users during account creation.
/// Plans define usage limits, trial days, and enabled features — no actual billing.
/// </summary>
public class PricingPlan : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    /// <summary>Display-only monthly price shown to users. No actual billing occurs.</summary>
    public decimal DisplayPrice { get; set; }

    /// <summary>Free trial period in days. 0 = no trial.</summary>
    public int TrialDays { get; set; } = 0;

    // Usage Limits
    public int AIMinutesIncluded { get; set; }
    public int WhatsAppMessagesIncluded { get; set; }
    public int SMSMessagesIncluded { get; set; }
    public int ImagesIncluded { get; set; }
    public int MaxPhoneNumbers { get; set; } = 1;

    /// <summary>If true, block usage when limits are exceeded.</summary>
    public bool EnforceHardLimits { get; set; } = true;

    /// <summary>JSON object of enabled features: {"voiceAI":true,"whatsApp":true,...}</summary>
    public string? EnabledFeatures { get; set; }

    /// <summary>Soft-toggle to hide from plan selection without deleting.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Display ordering in plan selection UI.</summary>
    public int SortOrder { get; set; } = 0;
}
