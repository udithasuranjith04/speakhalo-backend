using SpeakHalo.Domain.Enums;

namespace SpeakHalo.Application.DTOs;

/// <summary>
/// Request to save user cookie consent choices
/// </summary>
public class SaveConsentRequest
{
    public bool Essential { get; set; } = true; // Always true
    public bool Functional { get; set; }
    public bool Analytics { get; set; }
    public bool Performance { get; set; }
    public bool Marketing { get; set; }
    public bool Advertising { get; set; }
    public bool SocialMedia { get; set; }
}

/// <summary>
/// Response containing user's current consent choices
/// </summary>
public class UserConsentsResponse
{
    public bool Essential { get; set; }
    public bool Functional { get; set; }
    public bool Analytics { get; set; }
    public bool Performance { get; set; }
    public bool Marketing { get; set; }
    public bool Advertising { get; set; }
    public bool SocialMedia { get; set; }
    public DateTime LastUpdated { get; set; }
    public string? ConsentToken { get; set; }
}

/// <summary>
/// Cookie definition response
/// </summary>
public class CookieDefinitionDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
    public int? ExpirationDays { get; set; }
    public bool IsEssential { get; set; }
    public string? Provider { get; set; }
    public string? PrivacyPolicyUrl { get; set; }
}

/// <summary>
/// Cookie policy response
/// </summary>
public class CookiePolicyDto
{
    public string Id { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public DateTime EffectiveDate { get; set; }
    public string Language { get; set; } = string.Empty;
    public string? PolicyUrl { get; set; }
}

/// <summary>
/// Cookie category information
/// </summary>
public class CookieCategoryDto
{
    public string Category { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsEssential { get; set; }
    public bool CurrentConsent { get; set; }
    public List<CookieDefinitionDto> Cookies { get; set; } = new();
}

/// <summary>
/// Consent banner data response
/// </summary>
public class ConsentBannerResponse
{
    public CookiePolicyDto? Policy { get; set; }
    public List<CookieCategoryDto> Categories { get; set; } = new();
    public bool HasExistingConsent { get; set; }
}

/// <summary>
/// Request to withdraw consent
/// </summary>
public class WithdrawConsentRequest
{
    public string? Reason { get; set; }
}

/// <summary>
/// Request to create/update cookie definition (Admin only)
/// </summary>
public class CreateCookieDefinitionRequest
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public CookieCategory Category { get; set; }
    public string Domain { get; set; } = string.Empty;
    public string Path { get; set; } = "/";
    public int? ExpirationDays { get; set; }
    public bool IsHttpOnly { get; set; } = true;
    public bool IsSecure { get; set; } = true;
    public string SameSite { get; set; } = "Lax";
    public bool IsActive { get; set; } = true;
    public bool IsEssential { get; set; }
    public string? Provider { get; set; }
    public string? PrivacyPolicyUrl { get; set; }
    public int DisplayOrder { get; set; }
}

/// <summary>
/// Request to create/update cookie policy (Admin only)
/// </summary>
public class CreateCookiePolicyRequest
{
    public string Version { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public DateTime EffectiveDate { get; set; }
    public bool RequiresConsent { get; set; } = true;
    public string Language { get; set; } = "en";
    public string? PolicyUrl { get; set; }
    public string? ChangeNotes { get; set; }
}

/// <summary>
/// Consent statistics response (Admin only)
/// </summary>
public class ConsentStatisticsResponse
{
    public string Category { get; set; } = string.Empty;
    public int TotalConsents { get; set; }
    public int Accepted { get; set; }
    public int Declined { get; set; }
    public double AcceptanceRate { get; set; }
}
