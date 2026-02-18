using SpeakHalo.Domain.Enums;

namespace SpeakHalo.Domain.Entities;

/// <summary>
/// Records user cookie consent choices for GDPR/CCPA compliance
/// </summary>
public class UserCookieConsent : BaseEntity
{
    /// <summary>
    /// User ID (null for anonymous users - use ConsentToken instead)
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Consent token for anonymous users (stored in cookie)
    /// </summary>
    public string? ConsentToken { get; set; }

    /// <summary>
    /// Cookie policy version at time of consent
    /// </summary>
    public string PolicyId { get; set; } = string.Empty;

    /// <summary>
    /// Cookie category this consent applies to
    /// </summary>
    public CookieCategory Category { get; set; }

    /// <summary>
    /// Did user consent to this category?
    /// </summary>
    public bool Consented { get; set; } = false;

    /// <summary>
    /// When consent was given or last updated
    /// </summary>
    public DateTime ConsentDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When consent expires (null = indefinite)
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// IP address at time of consent
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// User agent at time of consent
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// Country code from IP geolocation
    /// </summary>
    public string? CountryCode { get; set; }

    /// <summary>
    /// Consent method (banner, preferences page, API)
    /// </summary>
    public string ConsentMethod { get; set; } = "banner";

    /// <summary>
    /// Is this consent still active?
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// When consent was withdrawn (null = not withdrawn)
    /// </summary>
    public DateTime? WithdrawnAt { get; set; }

    /// <summary>
    /// Reason for withdrawal (if provided)
    /// </summary>
    public string? WithdrawalReason { get; set; }

    /// <summary>
    /// Consent source (web, mobile, API)
    /// </summary>
    public string? Source { get; set; }

    /// <summary>
    /// Referring URL at time of consent
    /// </summary>
    public string? ReferrerUrl { get; set; }

    /// <summary>
    /// Additional metadata as JSON
    /// </summary>
    public string? Metadata { get; set; }

    public virtual User? User { get; set; }
    public virtual CookiePolicy? Policy { get; set; }
}
