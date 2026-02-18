namespace SpeakHalo.Domain.Entities;

/// <summary>
/// Cookie policy versions for tracking policy changes and user consent
/// </summary>
public class CookiePolicy : BaseEntity
{
    /// <summary>
    /// Policy version (e.g., "1.0", "2.0", "2024.1")
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Policy title/name
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Full policy content in HTML
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Short summary for cookie banner
    /// </summary>
    public string Summary { get; set; } = string.Empty;

    /// <summary>
    /// When this policy becomes effective
    /// </summary>
    public DateTime EffectiveDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When this policy expires (null = current policy)
    /// </summary>
    public DateTime? ExpirationDate { get; set; }

    /// <summary>
    /// Is this the currently active policy?
    /// </summary>
    public bool IsActive { get; set; } = false;

    /// <summary>
    /// Is user consent required for this policy version?
    /// </summary>
    public bool RequiresConsent { get; set; } = true;

    /// <summary>
    /// Language/locale code (e.g., "en", "en-US", "es")
    /// </summary>
    public string Language { get; set; } = "en";

    /// <summary>
    /// Policy URL for "Read More" links
    /// </summary>
    public string? PolicyUrl { get; set; }

    /// <summary>
    /// Who published this policy version
    /// </summary>
    public string? PublishedBy { get; set; }

    /// <summary>
    /// When this policy was published
    /// </summary>
    public DateTime? PublishedAt { get; set; }

    /// <summary>
    /// Change notes for this version
    /// </summary>
    public string? ChangeNotes { get; set; }

    /// <summary>
    /// Number of users who accepted this policy
    /// </summary>
    public int AcceptanceCount { get; set; } = 0;

    /// <summary>
    /// Additional metadata as JSON
    /// </summary>
    public string? Metadata { get; set; }

    // Navigation properties
    public virtual ICollection<UserCookieConsent> UserConsents { get; set; } = new List<UserCookieConsent>();
}
