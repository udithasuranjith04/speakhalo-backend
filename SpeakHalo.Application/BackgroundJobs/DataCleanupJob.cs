using Microsoft.Extensions.Logging;
using SpeakHalo.Domain.Entities;
using SpeakHalo.Domain.Interfaces.Repositories;

namespace SpeakHalo.Application.BackgroundJobs;

public class DataCleanupJob
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DataCleanupJob> _logger;

    public DataCleanupJob(IUnitOfWork unitOfWork, ILogger<DataCleanupJob> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task CleanupOldDataAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting data cleanup");

        try
        {
            var cutoffDate = DateTime.UtcNow.AddMonths(-12);

            var oldAuditLogs = await _unitOfWork.Repository<AuditLog>()
                .FindAsync(a => a.PerformedAt < cutoffDate, cancellationToken);

            foreach (var log in oldAuditLogs)
            {
                log.IsDeleted = true;
                log.DeletedAt = DateTime.UtcNow;
                log.DeletedBy = "system:cleanup";
                _unitOfWork.Repository<AuditLog>().Update(log);
            }

            _logger.LogInformation("Soft-deleted {Count} old audit logs", oldAuditLogs.Count());

            var expiredTokens = await _unitOfWork.Repository<RefreshToken>()
                .FindAsync(t => t.ExpiresAt < DateTime.UtcNow && !t.Revoked, cancellationToken);

            foreach (var token in expiredTokens)
            {
                token.Revoked = true;
                _unitOfWork.Repository<RefreshToken>().Update(token);
            }

            _logger.LogInformation("Revoked {Count} expired tokens", expiredTokens.Count());

            var oldNotifications = await _unitOfWork.Repository<Notification>()
                .FindAsync(n => n.CreatedAt < cutoffDate && n.IsRead, cancellationToken);

            foreach (var notification in oldNotifications)
            {
                notification.IsDeleted = true;
                notification.DeletedAt = DateTime.UtcNow;
                notification.DeletedBy = "system:cleanup";
                _unitOfWork.Repository<Notification>().Update(notification);
            }

            _logger.LogInformation("Soft-deleted {Count} old read notifications", oldNotifications.Count());

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Completed data cleanup");
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Data cleanup job cancelled");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during data cleanup");
        }
    }

    public async Task CleanupExpiredInvitesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting expired invite cleanup");

        try
        {
            // Soft-delete unredeemed invites that expired more than 30 days ago
            var cutoff = DateTime.UtcNow.AddDays(-30);

            var stale = await _unitOfWork.Repository<TrialInvite>()
                .FindAsync(i => !i.IsDeleted && !i.IsRedeemed && i.ExpiresAt < cutoff, cancellationToken);

            foreach (var invite in stale)
            {
                invite.IsDeleted = true;
                invite.DeletedAt = DateTime.UtcNow;
                invite.DeletedBy = "system:cleanup";
                _unitOfWork.Repository<TrialInvite>().Update(invite);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Soft-deleted {Count} stale unredeemed invites", stale.Count());
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Expired invite cleanup job cancelled");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up expired invites");
        }
    }

    public async Task CleanupFailedCallsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Cleaning up old failed/incomplete calls");

        try
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-90);

            // Only soft-delete failed/incomplete calls (no outcome, no end time)
            // Never delete completed calls - they contain valuable customer history
            var failedCalls = await _unitOfWork.Calls
                .FindAsync(c => c.CreatedAt < cutoffDate
                    && c.Outcome == null
                    && c.EndedAt == null, cancellationToken);

            foreach (var call in failedCalls)
            {
                call.IsDeleted = true;
                call.DeletedAt = DateTime.UtcNow;
                call.DeletedBy = "system:cleanup";
                _unitOfWork.Calls.Update(call);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Soft-deleted {Count} old failed/incomplete calls", failedCalls.Count());
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Failed calls cleanup job cancelled");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up old calls");
        }
    }
}
