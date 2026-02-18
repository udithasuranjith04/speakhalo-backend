namespace SpeakHalo.Domain.Entities;

/// <summary>
/// Phone number automatically assigned to user during registration
/// Each user gets exactly ONE number based on their country
/// Number supports Voice + SMS + WhatsApp automatically
/// </summary>
public class PhoneNumber : BaseEntity
{
    /// <summary>
    /// User who owns this number
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Phone number in E.164 format (e.g., +14155551234)
    /// </summary>
    public string Number { get; set; } = string.Empty;

    /// <summary>
    /// Friendly name (optional, user can customize)
    /// </summary>
    public string? FriendlyName { get; set; }

    /// <summary>
    /// Twilio SID for this phone number
    /// </summary>
    public string? TwilioSid { get; set; }

    /// <summary>
    /// ISO country code (US, GB, CA, etc.)
    /// </summary>
    public string CountryCode { get; set; } = "US";

    /// <summary>
    /// Always true - this is the user's only number
    /// </summary>
    public bool IsPrimary { get; set; } = true;

    /// <summary>
    /// Voice calls enabled (always true for auto-assigned numbers)
    /// </summary>
    public bool VoiceEnabled { get; set; } = true;

    /// <summary>
    /// SMS enabled (always true for auto-assigned numbers)
    /// </summary>
    public bool SmsEnabled { get; set; } = true;

    /// <summary>
    /// WhatsApp enabled (always true for auto-assigned numbers)
    /// </summary>
    public bool WhatsAppEnabled { get; set; } = true;

    /// <summary>
    /// WhatsApp verification status
    /// </summary>
    public bool WhatsAppVerified { get; set; } = false;

    /// <summary>
    /// When this number was assigned to the user
    /// </summary>
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// How this number was assigned (OnDemand, Manual, Migration)
    /// </summary>
    public string AssignmentMethod { get; set; } = "OnDemand";

    /// <summary>
    /// Whether this number is currently active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Navigation property to user
    /// </summary>
    public virtual User User { get; set; } = null!;
}
