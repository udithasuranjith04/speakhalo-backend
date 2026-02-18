using SpeakHalo.Domain.Enums;

namespace SpeakHalo.Domain.Entities;

/// <summary>
/// Defines cookies used in the application for consent management and documentation
/// </summary>
public class CookieDefinition : BaseEntity
{
    /// <summary>
    /// Cookie name (e.g., "session_token", "_ga", "marketing_consent")
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Display name for users (e.g., "Session Cookie", "Google Analytics")
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Detailed description of what this cookie does
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Cookie category for consent management
    /// </summary>
    public CookieCategory Category { get; set; }

    /// <summary>
    /// Cookie domain (e.g., ".speakhalo.com")
    /// </summary>
    public string Domain { get; set; } = string.Empty;

    /// <summary>
    /// Cookie path (e.g., "/", "/api")
    /// </summary>
    public string Path { get; set; } = "/";

    /// <summary>
    /// Cookie expiration in days (null = session cookie)
    /// </summary>
    public int? ExpirationDays { get; set; }

    /// <summary>
    /// Is this an HttpOnly cookie? (not accessible via JavaScript)
    /// </summary>
    public bool IsHttpOnly { get; set; } = true;

    /// <summary>
    /// Is this a Secure cookie? (HTTPS only)
    /// </summary>
    public bool IsSecure { get; set; } = true;

    /// <summary>
    /// SameSite attribute (None, Lax, Strict)
    /// </summary>
    public string SameSite { get; set; } = "Lax";

    /// <summary>
    /// Is this cookie currently active/in use?
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Is this an essential cookie? (cannot be declined)
    /// </summary>
    public bool IsEssential { get; set; } = false;

    /// <summary>
    /// Provider/vendor name (e.g., "SpeakHalo", "Google", "Facebook")
    /// </summary>
    public string? Provider { get; set; }

    /// <summary>
    /// Link to provider's privacy policy
    /// </summary>
    public string? PrivacyPolicyUrl { get; set; }

    /// <summary>
    /// Display order in cookie consent UI
    /// </summary>
    public int DisplayOrder { get; set; } = 0;

    /// <summary>
    /// Additional metadata as JSON
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// When this cookie definition was last reviewed
    /// </summary>
    public DateTime? LastReviewedAt { get; set; }

    /// <summary>
    /// Who last reviewed this cookie definition
    /// </summary>
    public string? LastReviewedBy { get; set; }
}
