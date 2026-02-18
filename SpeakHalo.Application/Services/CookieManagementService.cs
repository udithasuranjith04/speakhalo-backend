using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SpeakHalo.Domain.Entities;
using SpeakHalo.Domain.Enums;
using SpeakHalo.Domain.Interfaces.Repositories;
using System.Security.Cryptography;
using System.Text;

namespace SpeakHalo.Application.Services;

/// <summary>
/// Service for managing cookie consents and compliance (GDPR/CCPA)
/// </summary>
public class CookieManagementService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICookieDefinitionRepository _cookieDefinitionRepo;
    private readonly ICookiePolicyRepository _cookiePolicyRepo;
    private readonly IUserCookieConsentRepository _consentRepo;
    private readonly ICookieAuditLogRepository _auditLogRepo;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<CookieManagementService> _logger;

    public CookieManagementService(
        IUnitOfWork unitOfWork,
        ICookieDefinitionRepository cookieDefinitionRepo,
        ICookiePolicyRepository cookiePolicyRepo,
        IUserCookieConsentRepository consentRepo,
        ICookieAuditLogRepository auditLogRepo,
        IHttpContextAccessor httpContextAccessor,
        ILogger<CookieManagementService> logger)
    {
        _unitOfWork = unitOfWork;
        _cookieDefinitionRepo = cookieDefinitionRepo;
        _cookiePolicyRepo = cookiePolicyRepo;
        _consentRepo = consentRepo;
        _auditLogRepo = auditLogRepo;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    /// <summary>
    /// Get all active cookie definitions grouped by category
    /// </summary>
    public async Task<Dictionary<CookieCategory, List<CookieDefinition>>> GetCookieDefinitionsByCategoryAsync(CancellationToken cancellationToken = default)
    {
        var definitions = await _cookieDefinitionRepo.GetActiveDefinitionsAsync(cancellationToken);
        return definitions.GroupBy(d => d.Category).ToDictionary(g => g.Key, g => g.ToList());
    }

    /// <summary>
    /// Get the active cookie policy
    /// </summary>
    public async Task<CookiePolicy?> GetActivePolicyAsync(string language = "en", CancellationToken cancellationToken = default)
    {
        return await _cookiePolicyRepo.GetActivePolicyAsync(language, cancellationToken);
    }

    /// <summary>
    /// Generate or retrieve consent token for anonymous users
    /// </summary>
    public string GetOrCreateConsentToken()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null) return GenerateConsentToken();

        // Check if consent token cookie exists
        if (httpContext.Request.Cookies.TryGetValue("consent_token", out var token) && !string.IsNullOrEmpty(token))
        {
            return token;
        }

        // Generate new token
        var newToken = GenerateConsentToken();

        // Set cookie
        httpContext.Response.Cookies.Append("consent_token", newToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddYears(1),
            Path = "/"
        });

        return newToken;
    }

    /// <summary>
    /// Save user cookie consent choices
    /// </summary>
    public async Task<bool> SaveConsentAsync(
        string? userId,
        Dictionary<CookieCategory, bool> consents,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var policy = await GetActivePolicyAsync(cancellationToken: cancellationToken);
            if (policy == null) return false;

            var userIdOrToken = userId ?? GetOrCreateConsentToken();
            var httpContext = _httpContextAccessor.HttpContext;

            foreach (var consent in consents)
            {
                // Check if consent already exists
                var existingConsent = await _consentRepo.GetConsentAsync(
                    userIdOrToken,
                    consent.Key,
                    policy.Id,
                    cancellationToken);

                if (existingConsent != null)
                {
                    // Update existing consent
                    existingConsent.Consented = consent.Value;
                    existingConsent.ConsentDate = DateTime.UtcNow;
                    existingConsent.IpAddress = httpContext?.Connection.RemoteIpAddress?.ToString();
                    existingConsent.UserAgent = httpContext?.Request.Headers["User-Agent"].ToString();
                    _consentRepo.Update(existingConsent);
                }
                else
                {
                    // Create new consent record
                    var newConsent = new UserCookieConsent
                    {
                        UserId = userId,
                        ConsentToken = userId == null ? userIdOrToken : null,
                        PolicyId = policy.Id,
                        Category = consent.Key,
                        Consented = consent.Value,
                        ConsentDate = DateTime.UtcNow,
                        IpAddress = httpContext?.Connection.RemoteIpAddress?.ToString(),
                        UserAgent = httpContext?.Request.Headers["User-Agent"].ToString(),
                        ConsentMethod = "api",
                        Source = "web",
                        IsActive = true
                    };

                    await _consentRepo.AddAsync(newConsent, cancellationToken);
                }

                // Log the consent action
                await _auditLogRepo.LogConsentActionAsync(
                    userIdOrToken,
                    consent.Value ? "consent_given" : "consent_declined",
                    consent.Key,
                    new { category = consent.Key.ToString(), consented = consent.Value },
                    cancellationToken);
            }

            // Increment policy acceptance count
            policy.AcceptanceCount++;
            _cookiePolicyRepo.Update(policy);

            // Save all changes
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save cookie consent for user {UserId}", userId);
            return false;
        }
    }

    /// <summary>
    /// Get user's current consent choices
    /// </summary>
    public async Task<Dictionary<CookieCategory, bool>> GetUserConsentsAsync(
        string? userId,
        CancellationToken cancellationToken = default)
    {
        var userIdOrToken = userId ?? GetOrCreateConsentToken();
        var consents = await _consentRepo.GetConsentSummaryAsync(userIdOrToken, cancellationToken);

        // If no consents found, return defaults (only essential cookies accepted)
        if (!consents.Any())
        {
            return Enum.GetValues<CookieCategory>().ToDictionary(
                c => c,
                c => c == CookieCategory.Essential
            );
        }

        return consents;
    }

    /// <summary>
    /// Check if user has consent for a specific category
    /// </summary>
    public async Task<bool> HasConsentAsync(
        string? userId,
        CookieCategory category,
        CancellationToken cancellationToken = default)
    {
        // Essential cookies don't require consent
        if (category == CookieCategory.Essential)
            return true;

        var userIdOrToken = userId ?? GetOrCreateConsentToken();
        return await _consentRepo.HasConsentAsync(userIdOrToken, category, cancellationToken);
    }

    /// <summary>
    /// Withdraw all consents for a user
    /// </summary>
    public async Task<bool> WithdrawAllConsentsAsync(
        string? userId,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userIdOrToken = userId ?? GetOrCreateConsentToken();
            var consents = userId != null
                ? await _consentRepo.GetUserConsentsAsync(userId, cancellationToken)
                : await _consentRepo.GetConsentsByTokenAsync(userIdOrToken, cancellationToken);

            foreach (var consent in consents)
            {
                await _consentRepo.WithdrawConsentAsync(consent.Id, reason, cancellationToken);
            }

            // Log withdrawal
            await _auditLogRepo.LogConsentActionAsync(
                userIdOrToken,
                "all_consents_withdrawn",
                null,
                new { reason },
                cancellationToken);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to withdraw consents for user {UserId}", userId);
            return false;
        }
    }

    /// <summary>
    /// Get consent banner data for UI
    /// </summary>
    public async Task<object> GetConsentBannerDataAsync(
        string? userId,
        string language = "en",
        CancellationToken cancellationToken = default)
    {
        var policy = await GetActivePolicyAsync(language, cancellationToken);
        var definitions = await GetCookieDefinitionsByCategoryAsync(cancellationToken);
        var userConsents = await GetUserConsentsAsync(userId, cancellationToken);

        return new
        {
            Policy = new
            {
                policy?.Version,
                policy?.Title,
                policy?.Summary,
                policy?.PolicyUrl
            },
            Categories = definitions.Select(cat => new
            {
                Category = cat.Key.ToString(),
                Name = GetCategoryDisplayName(cat.Key),
                Description = GetCategoryDescription(cat.Key),
                IsEssential = cat.Key == CookieCategory.Essential,
                Cookies = cat.Value.Select(c => new
                {
                    c.Name,
                    c.DisplayName,
                    c.Description,
                    c.Provider,
                    c.ExpirationDays
                }),
                CurrentConsent = userConsents.TryGetValue(cat.Key, out var consent) ? consent : false
            })
        };
    }

    /// <summary>
    /// Set cookie with consent check
    /// </summary>
    public async Task<bool> SetCookieIfAllowedAsync(
        string? userId,
        string cookieName,
        string value,
        CookieCategory category,
        CookieOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var hasConsent = await HasConsentAsync(userId, category, cancellationToken);
        if (!hasConsent) return false;

        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null) return false;

        httpContext.Response.Cookies.Append(cookieName, value, options ?? new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax
        });

        return true;
    }

    #region Private Helper Methods

    private static string GenerateConsentToken()
    {
        var randomBytes = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }
        return Convert.ToBase64String(randomBytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');
    }

    private static string GetCategoryDisplayName(CookieCategory category)
    {
        return category switch
        {
            CookieCategory.Essential => "Essential",
            CookieCategory.Functional => "Functional",
            CookieCategory.Analytics => "Analytics",
            CookieCategory.Performance => "Performance",
            CookieCategory.Marketing => "Marketing",
            CookieCategory.Advertising => "Advertising",
            CookieCategory.SocialMedia => "Social Media",
            _ => category.ToString()
        };
    }

    private static string GetCategoryDescription(CookieCategory category)
    {
        return category switch
        {
            CookieCategory.Essential => "Required for basic functionality and cannot be disabled.",
            CookieCategory.Functional => "Enable enhanced functionality and personalization.",
            CookieCategory.Analytics => "Help us understand how visitors interact with our website.",
            CookieCategory.Performance => "Help us monitor and improve website performance.",
            CookieCategory.Marketing => "Used to deliver relevant advertisements and track campaigns.",
            CookieCategory.Advertising => "Used to personalize ads and measure advertising effectiveness.",
            CookieCategory.SocialMedia => "Enable social media features and share content.",
            _ => string.Empty
        };
    }

    #endregion
}
