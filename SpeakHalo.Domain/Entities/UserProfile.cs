using SpeakHalo.Domain.Enums;

namespace SpeakHalo.Domain.Entities;

/// <summary>
/// User profile and business information
/// </summary>
public class UserProfile : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public string? Avatar { get; set; }
    public string BusinessName { get; set; } = string.Empty;
    public string? BusinessPhone { get; set; }
    public string? BusinessAddress { get; set; }
    public string? BusinessWebsite { get; set; }
    public ServiceType Industry { get; set; }
    public string? IndustrySpecialization { get; set; }

    // Onboarding
    public bool OnboardingCompleted { get; set; } = false;
    public int OnboardingStep { get; set; } = 0;

    // Integrations
    public string? GoogleCalendarToken { get; set; }
    public bool GoogleCalendarConnected { get; set; } = false;

    // Configuration
    public string? NotificationSettings { get; set; }
    public string? EnabledLanguages { get; set; }
    public string? AssignedCountries { get; set; }
    public string? AiAgentConfig { get; set; }
    public DateTime? ConfigUpdatedAt { get; set; }
    public string? ConfigUpdatedByAdminId { get; set; }

    // Caller Recognition
    public bool CallerRecognitionEnabled { get; set; } = false;

    // Consent
    public string? CookieConsent { get; set; }
    public DateTime? CookieConsentAt { get; set; }

    public virtual User User { get; set; } = null!;
}
