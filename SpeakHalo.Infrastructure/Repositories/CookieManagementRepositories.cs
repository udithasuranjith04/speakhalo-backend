using Microsoft.EntityFrameworkCore;
using SpeakHalo.Domain.Entities;
using SpeakHalo.Domain.Enums;
using SpeakHalo.Domain.Interfaces.Repositories;
using SpeakHalo.Infrastructure.Data;
using System.Text.Json;

namespace SpeakHalo.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for cookie definitions
/// </summary>
public class CookieDefinitionRepository : Repository<CookieDefinition>, ICookieDefinitionRepository
{
    public CookieDefinitionRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<List<CookieDefinition>> GetActiveDefinitionsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<CookieDefinition>> GetByCategoryAsync(CookieCategory category, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(c => c.IsActive && c.Category == category)
            .OrderBy(c => c.DisplayOrder)
            .ToListAsync(cancellationToken);
    }

    public async Task<CookieDefinition?> GetByNameAsync(string name, string domain, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Name == name && c.Domain == domain && c.IsActive, cancellationToken);
    }

    public async Task<List<CookieDefinition>> GetEssentialCookiesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(c => c.IsActive && c.IsEssential)
            .OrderBy(c => c.DisplayOrder)
            .ToListAsync(cancellationToken);
    }
}

/// <summary>
/// Repository implementation for cookie policies
/// </summary>
public class CookiePolicyRepository : Repository<CookiePolicy>, ICookiePolicyRepository
{
    public CookiePolicyRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<CookiePolicy?> GetActivePolicyAsync(string language = "en", CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.IsActive && p.Language == language && p.EffectiveDate <= DateTime.UtcNow, cancellationToken);
    }

    public async Task<CookiePolicy?> GetByVersionAsync(string version, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Version == version, cancellationToken);
    }

    public async Task<List<CookiePolicy>> GetPolicyHistoryAsync(string language = "en", CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(p => p.Language == language)
            .OrderByDescending(p => p.EffectiveDate)
            .ToListAsync(cancellationToken);
    }

    public async Task SetActivePolicyAsync(string policyId, CancellationToken cancellationToken = default)
    {
        var policy = await _dbSet.FindAsync(new object[] { policyId }, cancellationToken);
        if (policy == null) return;

        // Deactivate all other policies for this language
        var activePolicies = await _dbSet
            .Where(p => p.IsActive && p.Language == policy.Language && p.Id != policyId)
            .ToListAsync(cancellationToken);

        foreach (var p in activePolicies)
        {
            p.IsActive = false;
            p.ExpirationDate = DateTime.UtcNow;
        }

        // Activate the new policy
        policy.IsActive = true;
        policy.PublishedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>
/// Repository implementation for user cookie consents
/// </summary>
public class UserCookieConsentRepository : Repository<UserCookieConsent>, IUserCookieConsentRepository
{
    public UserCookieConsentRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<List<UserCookieConsent>> GetUserConsentsAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(c => c.UserId == userId && c.IsActive)
            .OrderByDescending(c => c.ConsentDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<UserCookieConsent>> GetConsentsByTokenAsync(string consentToken, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(c => c.ConsentToken == consentToken && c.IsActive)
            .OrderByDescending(c => c.ConsentDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<UserCookieConsent?> GetConsentAsync(string userIdOrToken, CookieCategory category, string policyId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(c => c.IsActive &&
                        c.Category == category &&
                        c.PolicyId == policyId &&
                        (c.UserId == userIdOrToken || c.ConsentToken == userIdOrToken))
            .OrderByDescending(c => c.ConsentDate)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> HasConsentAsync(string userIdOrToken, CookieCategory category, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .AnyAsync(c => c.IsActive &&
                          c.Consented &&
                          c.Category == category &&
                          (c.UserId == userIdOrToken || c.ConsentToken == userIdOrToken) &&
                          (c.ExpiresAt == null || c.ExpiresAt > DateTime.UtcNow), cancellationToken);
    }

    public async Task WithdrawConsentAsync(string consentId, string? reason = null, CancellationToken cancellationToken = default)
    {
        var consent = await _dbSet.FindAsync(new object[] { consentId }, cancellationToken);
        if (consent != null && consent.IsActive)
        {
            consent.IsActive = false;
            consent.WithdrawnAt = DateTime.UtcNow;
            consent.WithdrawalReason = reason;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<Dictionary<CookieCategory, bool>> GetConsentSummaryAsync(string userIdOrToken, CancellationToken cancellationToken = default)
    {
        var consents = await _dbSet
            .AsNoTracking()
            .Where(c => c.IsActive &&
                       (c.UserId == userIdOrToken || c.ConsentToken == userIdOrToken) &&
                       (c.ExpiresAt == null || c.ExpiresAt > DateTime.UtcNow))
            .GroupBy(c => c.Category)
            .Select(g => new
            {
                Category = g.Key,
                Consented = g.OrderByDescending(c => c.ConsentDate).First().Consented
            })
            .ToListAsync(cancellationToken);

        return consents.ToDictionary(c => c.Category, c => c.Consented);
    }
}

/// <summary>
/// Repository implementation for cookie audit logs
/// </summary>
public class CookieAuditLogRepository : Repository<CookieAuditLog>, ICookieAuditLogRepository
{
    public CookieAuditLogRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<List<CookieAuditLog>> GetUserAuditLogsAsync(string userId, int limit = 100, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(l => l.UserId == userId)
            .OrderByDescending(l => l.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<CookieAuditLog>> GetAuditLogsByActionAsync(string action, DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsNoTracking().Where(l => l.Action == action);

        if (from.HasValue)
            query = query.Where(l => l.CreatedAt >= from.Value);

        if (to.HasValue)
            query = query.Where(l => l.CreatedAt <= to.Value);

        return await query
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task LogConsentActionAsync(string userIdOrToken, string action, CookieCategory? category = null, object? details = null, CancellationToken cancellationToken = default)
    {
        var log = new CookieAuditLog
        {
            Action = action,
            Category = category,
            Success = true,
            Details = details != null ? JsonSerializer.Serialize(details) : null
        };

        // Determine if it's a userId or token
        if (Guid.TryParse(userIdOrToken, out _))
        {
            log.UserId = userIdOrToken;
        }
        else
        {
            log.ConsentToken = userIdOrToken;
        }

        await _dbSet.AddAsync(log, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
