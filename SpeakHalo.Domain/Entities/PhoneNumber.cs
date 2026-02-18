namespace SpeakHalo.Domain.Entities;

/// <summary>
/// Phone number automatically assigned to user during registration.
/// Each user gets exactly ONE number based on their country.
/// Number supports Voice + SMS + WhatsApp automatically.
/// </summary>
public class PhoneNumber : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public string Number { get; set; } = string.Empty;
    public string? FriendlyName { get; set; }
    public string? TwilioSid { get; set; }
    public string CountryCode { get; set; } = "US";
    public bool IsPrimary { get; set; } = true;
    public bool VoiceEnabled { get; set; } = true;
    public bool SmsEnabled { get; set; } = true;
    public bool WhatsAppEnabled { get; set; } = true;
    public bool WhatsAppVerified { get; set; } = false;
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    public string AssignmentMethod { get; set; } = "OnDemand";
    public bool IsActive { get; set; } = true;

    public virtual User User { get; set; } = null!;
}
