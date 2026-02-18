using SpeakHalo.Domain.Enums;

namespace SpeakHalo.Domain.Entities;

/// <summary>
/// Audit trail for cookie operations and consent changes
/// </summary>
public class CookieAuditLog : BaseEntity
{
    /// <summary>
    /// User ID (null for anonymous users)
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Consent token for anonymous users
    /// </summary>
    public string? ConsentToken { get; set; }

    /// <summary>
    /// Cookie consent ID (if related to a consent record)
    /// </summary>
    public string? ConsentId { get; set; }

    /// <summary>
    /// Cookie definition ID (if related to a specific cookie)
    /// </summary>
    public string? CookieDefinitionId { get; set; }

    /// <summary>
    /// Action performed (consent_given, consent_withdrawn, cookie_set, cookie_deleted, policy_viewed)
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Cookie category involved
    /// </summary>
    public CookieCategory? Category { get; set; }

    /// <summary>
    /// Old value before change (JSON)
    /// </summary>
    public string? OldValue { get; set; }

    /// <summary>
    /// New value after change (JSON)
    /// </summary>
    public string? NewValue { get; set; }

    /// <summary>
    /// IP address of the request
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// User agent of the request
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// Country code from IP geolocation
    /// </summary>
    public string? CountryCode { get; set; }

    /// <summary>
    /// Request URL
    /// </summary>
    public string? RequestUrl { get; set; }

    /// <summary>
    /// Referring URL
    /// </summary>
    public string? ReferrerUrl { get; set; }

    /// <summary>
    /// Session ID
    /// </summary>
    public string? SessionId { get; set; }

    /// <summary>
    /// Source of the action (web, mobile, API)
    /// </summary>
    public string? Source { get; set; }

    /// <summary>
    /// Additional details as JSON
    /// </summary>
    public string? Details { get; set; }

    /// <summary>
    /// Was this action successful?
    /// </summary>
    public bool Success { get; set; } = true;

    /// <summary>
    /// Error message if action failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// How long the action took (milliseconds)
    /// </summary>
    public int? DurationMs { get; set; }

    public virtual User? User { get; set; }
    public virtual UserCookieConsent? Consent { get; set; }
    public virtual CookieDefinition? CookieDefinition { get; set; }
}
