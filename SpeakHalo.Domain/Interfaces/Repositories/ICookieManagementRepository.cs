using SpeakHalo.Domain.Entities;
using SpeakHalo.Domain.Enums;

namespace SpeakHalo.Domain.Interfaces.Repositories;

/// <summary>
/// Repository interface for cookie definitions
/// </summary>
public interface ICookieDefinitionRepository : IRepository<CookieDefinition>
{
    Task<List<CookieDefinition>> GetActiveDefinitionsAsync(CancellationToken cancellationToken = default);
    Task<List<CookieDefinition>> GetByCategoryAsync(CookieCategory category, CancellationToken cancellationToken = default);
    Task<CookieDefinition?> GetByNameAsync(string name, string domain, CancellationToken cancellationToken = default);
    Task<List<CookieDefinition>> GetEssentialCookiesAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Repository interface for cookie policies
/// </summary>
public interface ICookiePolicyRepository : IRepository<CookiePolicy>
{
    Task<CookiePolicy?> GetActivePolicyAsync(string language = "en", CancellationToken cancellationToken = default);
    Task<CookiePolicy?> GetByVersionAsync(string version, CancellationToken cancellationToken = default);
    Task<List<CookiePolicy>> GetPolicyHistoryAsync(string language = "en", CancellationToken cancellationToken = default);
    Task SetActivePolicyAsync(string policyId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Repository interface for user cookie consents
/// </summary>
public interface IUserCookieConsentRepository : IRepository<UserCookieConsent>
{
    Task<List<UserCookieConsent>> GetUserConsentsAsync(string userId, CancellationToken cancellationToken = default);
    Task<List<UserCookieConsent>> GetConsentsByTokenAsync(string consentToken, CancellationToken cancellationToken = default);
    Task<UserCookieConsent?> GetConsentAsync(string userIdOrToken, CookieCategory category, string policyId, CancellationToken cancellationToken = default);
    Task<bool> HasConsentAsync(string userIdOrToken, CookieCategory category, CancellationToken cancellationToken = default);
    Task WithdrawConsentAsync(string consentId, string? reason = null, CancellationToken cancellationToken = default);
    Task<Dictionary<CookieCategory, bool>> GetConsentSummaryAsync(string userIdOrToken, CancellationToken cancellationToken = default);
}

/// <summary>
/// Repository interface for cookie audit logs
/// </summary>
public interface ICookieAuditLogRepository : IRepository<CookieAuditLog>
{
    Task<List<CookieAuditLog>> GetUserAuditLogsAsync(string userId, int limit = 100, CancellationToken cancellationToken = default);
    Task<List<CookieAuditLog>> GetAuditLogsByActionAsync(string action, DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default);
    Task LogConsentActionAsync(string userIdOrToken, string action, CookieCategory? category = null, object? details = null, CancellationToken cancellationToken = default);
}
