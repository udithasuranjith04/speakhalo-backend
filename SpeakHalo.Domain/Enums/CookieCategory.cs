namespace SpeakHalo.Domain.Enums;

/// <summary>
/// Cookie categories for consent management (GDPR/CCPA compliance)
/// </summary>
public enum CookieCategory
{
    /// <summary>
    /// Essential cookies required for basic functionality (cannot be disabled)
    /// Examples: Authentication, session management, security
    /// </summary>
    Essential = 1,

    /// <summary>
    /// Functional cookies that enhance user experience
    /// Examples: Language preference, theme settings, UI customization
    /// </summary>
    Functional = 2,

    /// <summary>
    /// Analytics cookies for understanding user behavior
    /// Examples: Google Analytics, page views, user flow tracking
    /// </summary>
    Analytics = 3,

    /// <summary>
    /// Performance cookies for monitoring and optimization
    /// Examples: Performance metrics, error tracking, load times
    /// </summary>
    Performance = 4,

    /// <summary>
    /// Marketing cookies for advertising and campaigns
    /// Examples: Ad targeting, conversion tracking, remarketing
    /// </summary>
    Marketing = 5,

    /// <summary>
    /// Advertising cookies for personalized ads
    /// Examples: Ad personalization, cross-site tracking
    /// </summary>
    Advertising = 6,

    /// <summary>
    /// Social media cookies for social sharing and integration
    /// Examples: Facebook pixel, Twitter widgets, social login
    /// </summary>
    SocialMedia = 7
}
