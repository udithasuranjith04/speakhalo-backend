using SpeakHalo.Domain.Enums;

namespace SpeakHalo.Domain.Entities;

/// <summary>
/// Core User entity (Aggregate Root)
/// Related data is now in separate entities for better organization
/// </summary>
public class User : BaseEntity
{
    // Core Identity
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Owner;
    public bool IsActive { get; set; } = true;

    // Legacy fields kept for backward compatibility (will be migrated to related entities)
    [Obsolete("Use UserAuth.HashedPassword instead")]
    public string HashedPassword { get; set; } = string.Empty;

    [Obsolete("Use UserProfile properties instead")]
    public string? Avatar { get; set; }

    [Obsolete("Use UserProfile.BusinessName instead")]
    public string BusinessName { get; set; } = string.Empty;

    [Obsolete("Use UserProfile.Industry instead")]
    public ServiceType Industry { get; set; }

    [Obsolete("Use UserProfile.IndustrySpecialization instead")]
    public string? IndustrySpecialization { get; set; }

    [Obsolete("Use UserSubscription.Plan instead")]
    public string? Plan { get; set; }

    [Obsolete("Use UserAuth.LastLogin instead")]
    public DateTime? LastLogin { get; set; }

    [Obsolete("Use UserSubscription properties instead")]
    public int BonusMinutes { get; set; } = 0;

    [Obsolete("Use UserSubscription.VoiceMinutesUsed instead")]
    public int VoiceMinutesUsed { get; set; } = 0;

    [Obsolete("Use UserSubscription.WhatsAppMessagesUsed instead")]
    public int WhatsAppMessagesUsed { get; set; } = 0;

    [Obsolete("Use UserSubscription.BonusWhatsAppMessages instead")]
    public int BonusWhatsAppMessages { get; set; } = 0;

    [Obsolete("Use UserSubscription.RolloverVoiceMinutes instead")]
    public int RolloverVoiceMinutes { get; set; } = 0;

    [Obsolete("Use UserSubscription.RolloverWhatsAppMessages instead")]
    public int RolloverWhatsAppMessages { get; set; } = 0;

    [Obsolete("Use UserSubscription.RolloverExpiresAt instead")]
    public DateTime? RolloverExpiresAt { get; set; }

    [Obsolete("Use UserTrial.IsTrial instead")]
    public bool IsTrial { get; set; } = false;

    [Obsolete("Use UserTrial properties instead")]
    public DateTime? TrialStartedAt { get; set; }

    [Obsolete("Use UserTrial.TrialEndsAt instead")]
    public DateTime? TrialEndsAt { get; set; }

    [Obsolete("Use UserTrial.TrialMinutesLimit instead")]
    public int? TrialMinutesLimit { get; set; }

    [Obsolete("Use UserTrial.TrialDurationDays instead")]
    public int? TrialDurationDays { get; set; }

    [Obsolete("Use UserTrial.TrialType instead")]
    public string? TrialType { get; set; }

    [Obsolete("Use UserTrial.TrialAssignedByAdminId instead")]
    public string? TrialAssignedByAdminId { get; set; }

    [Obsolete("Use UserTrial.TrialNotes instead")]
    public string? TrialNotes { get; set; }

    [Obsolete("Use UserProfile.CookieConsent instead")]
    public string? CookieConsent { get; set; }

    [Obsolete("Use UserProfile.CookieConsentAt instead")]
    public DateTime? CookieConsentAt { get; set; }

    [Obsolete("Use UserAuth.PasswordResetToken instead")]
    public string? PasswordResetToken { get; set; }

    [Obsolete("Use UserAuth.PasswordResetExpires instead")]
    public DateTime? PasswordResetExpires { get; set; }

    [Obsolete("Use UserProfile.NotificationSettings instead")]
    public string? NotificationSettings { get; set; }

    [Obsolete("Use UserAuth.SecuritySettings instead")]
    public string? SecuritySettings { get; set; }

    [Obsolete("Use UserProfile.GoogleCalendarToken instead")]
    public string? GoogleCalendarToken { get; set; }

    [Obsolete("Use UserProfile.GoogleCalendarConnected instead")]
    public bool GoogleCalendarConnected { get; set; } = false;

    [Obsolete("Use UserAuth.PasswordMustChange instead")]
    public bool PasswordMustChange { get; set; } = false;

    [Obsolete("Use UserAuth.PasswordExpiresAt instead")]
    public DateTime? PasswordExpiresAt { get; set; }

    [Obsolete("Use UserAuth.TemporaryPassword instead")]
    public bool TemporaryPassword { get; set; } = false;

    [Obsolete("Use UserAuth.PasswordChangedAt instead")]
    public DateTime? PasswordChangedAt { get; set; }

    [Obsolete("Use UserAdmin.CreatedByAdminId instead")]
    public string? CreatedByAdminId { get; set; }

    [Obsolete("Use UserAdmin.AssignedSalesRepId instead")]
    public string? AssignedSalesRepId { get; set; }

    [Obsolete("Use UserSubscription.MonthlyMinutesLimit instead")]
    public int? MonthlyMinutesLimit { get; set; }

    [Obsolete("Use UserSubscription.CustomWhatsAppLimit instead")]
    public int? CustomWhatsAppLimit { get; set; }

    [Obsolete("Use UserSubscription.CustomSmsLimit instead")]
    public int? CustomSmsLimit { get; set; }

    [Obsolete("Use UserSubscription.CustomMonthlyFee instead")]
    public decimal? CustomMonthlyFee { get; set; }

    [Obsolete("Use UserSubscription.CustomPerMinuteRate instead")]
    public decimal? CustomPerMinuteRate { get; set; }

    [Obsolete("Use UserSubscription.SetupFee instead")]
    public decimal? SetupFee { get; set; }

    [Obsolete("Use UserSubscription.CustomVoiceMinuteRate instead")]
    public decimal? CustomVoiceMinuteRate { get; set; }

    [Obsolete("Use UserSubscription.CustomTransferMinuteRate instead")]
    public decimal? CustomTransferMinuteRate { get; set; }

    [Obsolete("Use UserSubscription.CustomWhatsAppMessageRate instead")]
    public decimal? CustomWhatsAppMessageRate { get; set; }

    [Obsolete("Use UserSubscription.CustomSmsMessageRate instead")]
    public decimal? CustomSmsMessageRate { get; set; }

    [Obsolete("Use UserSubscription.EnabledFeatures instead")]
    public string? EnabledFeatures { get; set; }

    [Obsolete("Use UserAdmin.AccountManagerNotes instead")]
    public string? AccountManagerNotes { get; set; }

    [Obsolete("Use UserSubscription.BillingNotes instead")]
    public string? BillingNotes { get; set; }

    [Obsolete("Use UserSubscription.InvoiceHistory instead")]
    public string? InvoiceHistory { get; set; }

    [Obsolete("Use UserProfile.OnboardingCompleted instead")]
    public bool OnboardingCompleted { get; set; } = false;

    [Obsolete("Use UserProfile.OnboardingStep instead")]
    public int OnboardingStep { get; set; } = 0;

    [Obsolete("Use UserAuth.TotpSecret instead")]
    public string? TotpSecret { get; set; }

    [Obsolete("Use UserAuth.TotpEnabled instead")]
    public bool TotpEnabled { get; set; } = false;

    [Obsolete("Use UserAuth.TotpBackupCodes instead")]
    public string? TotpBackupCodes { get; set; }

    [Obsolete("Use UserProfile.EnabledLanguages instead")]
    public string? EnabledLanguages { get; set; }

    [Obsolete("Use UserProfile.AssignedCountries instead")]
    public string? AssignedCountries { get; set; }

    [Obsolete("Use UserAdmin.BlockReason instead")]
    public string? BlockReason { get; set; }

    [Obsolete("Use UserAdmin.BlockedAt instead")]
    public DateTime? BlockedAt { get; set; }

    [Obsolete("Use UserAdmin.BlockedByAdminId instead")]
    public string? BlockedByAdminId { get; set; }

    [Obsolete("Use UserProfile.AiAgentConfig instead")]
    public string? AiAgentConfig { get; set; }

    [Obsolete("Use UserAdmin.AdminNotes instead")]
    public string? AdminNotes { get; set; }

    [Obsolete("Use UserProfile.ConfigUpdatedAt instead")]
    public DateTime? ConfigUpdatedAt { get; set; }

    [Obsolete("Use UserProfile.ConfigUpdatedByAdminId instead")]
    public string? ConfigUpdatedByAdminId { get; set; }

    [Obsolete("Use UserSubscription.PricingNotifiedAt instead")]
    public DateTime? PricingNotifiedAt { get; set; }

    // Navigation Properties to Related Entities
    public virtual UserAuth? Auth { get; set; }
    public virtual UserProfile? Profile { get; set; }
    public virtual UserSubscription? Subscription { get; set; }
    public virtual UserTrial? Trial { get; set; }
    public virtual UserAdmin? Admin { get; set; }

    // Existing Navigation Properties
    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public virtual ICollection<PhoneNumber> PhoneNumbers { get; set; } = new List<PhoneNumber>();
}
