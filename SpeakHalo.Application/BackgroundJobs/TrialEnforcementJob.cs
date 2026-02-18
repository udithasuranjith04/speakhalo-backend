using System.Text.Json;
using Hangfire;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SpeakHalo.Application.Interfaces;
using SpeakHalo.Domain.Entities;
using SpeakHalo.Domain.Interfaces.Repositories;

#pragma warning disable CS0618

namespace SpeakHalo.Application.BackgroundJobs;

public class TrialEnforcementJob
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailService _emailService;
    private readonly IDistributedCache _cache;
    private readonly ILogger<TrialEnforcementJob> _logger;
    private readonly string _supportEmail;

    public TrialEnforcementJob(
        IUnitOfWork unitOfWork,
        IEmailService emailService,
        IDistributedCache cache,
        IConfiguration configuration,
        ILogger<TrialEnforcementJob> logger)
    {
        _unitOfWork = unitOfWork;
        _emailService = emailService;
        _cache = cache;
        _logger = logger;
        _supportEmail = configuration["SendGrid:SupportEmail"] ?? "support@speakhalo.com";
    }

    /// <summary>
    /// Expires trials that have passed their TrialEndsAt date.
    /// Sets IsOnTrial=false, HasActivePayment=false to block access.
    /// Runs hourly via Hangfire.
    /// </summary>
    [DisableConcurrentExecution(timeoutInSeconds: 3600)]
    public async Task ExpireTrialsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting trial expiration check");

        var now = DateTime.UtcNow;

        var expiredSubscriptions = await _unitOfWork.Repository<UserSubscription>()
            .FindAsync(s => s.IsOnTrial && s.TrialEndsAt != null && s.TrialEndsAt <= now, cancellationToken);

        var expiredCount = 0;
        var failedCount = 0;

        foreach (var subscription in expiredSubscriptions)
        {
            try
            {
                // Update subscription
                subscription.IsOnTrial = false;
                subscription.HasActivePayment = false;
                _unitOfWork.Repository<UserSubscription>().Update(subscription);

                // Update matching UserTrial record
                var trials = await _unitOfWork.Repository<UserTrial>()
                    .FindAsync(t => t.UserId == subscription.UserId && t.IsTrial, cancellationToken);

                var trial = trials.FirstOrDefault();
                if (trial != null)
                {
                    trial.IsTrial = false;
                    _unitOfWork.Repository<UserTrial>().Update(trial);
                }

                // Create audit log directly (avoid LogActionAsync's internal SaveChanges)
                var auditLog = new AuditLog
                {
                    UserId = subscription.UserId,
                    Action = "TRIAL_EXPIRED",
                    EntityType = "UserSubscription",
                    EntityId = subscription.Id,
                    OldValues = JsonSerializer.Serialize(new { IsOnTrial = true, HasActivePayment = true, subscription.TrialEndsAt }),
                    NewValues = JsonSerializer.Serialize(new { IsOnTrial = false, HasActivePayment = false }),
                    PerformedAt = now
                };
                await _unitOfWork.Repository<AuditLog>().AddAsync(auditLog, cancellationToken);

                // Commit DB changes for this user before sending email
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // Invalidate Redis cache so access is blocked immediately
                await InvalidateUserCacheAsync(subscription.UserId);

                // Send email (best-effort — DB is already committed)
                var user = await _unitOfWork.Users.GetByIdAsync(subscription.UserId, cancellationToken);
                if (user != null)
                {
                    var html = BuildTrialExpiredEmail(user.Name, user.BusinessName);
                    var emailResult = await _emailService.SendEmailAsync(
                        user.Email, "Your SpeakHalo Trial Has Ended", html, cancellationToken: cancellationToken);

                    if (!emailResult.IsSuccess)
                    {
                        _logger.LogWarning("Failed to send trial expiration email to {Email}: {Errors}",
                            user.Email, string.Join(", ", emailResult.Errors));
                    }
                }

                expiredCount++;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                failedCount++;
                _logger.LogError(ex, "Failed to expire trial for user {UserId}", subscription.UserId);
            }
        }

        _logger.LogInformation("Trial expiration check complete. Expired: {Expired}, Failed: {Failed}", expiredCount, failedCount);
    }

    /// <summary>
    /// Sends warning emails to users whose trials expire within 3 days.
    /// Skips users who received a warning in the last 24 hours.
    /// Runs daily via Hangfire.
    /// </summary>
    [DisableConcurrentExecution(timeoutInSeconds: 3600)]
    public async Task SendTrialWarningsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting trial expiration warning check");

        var now = DateTime.UtcNow;
        var warningCutoff = now.AddDays(3);

        var expiringSubscriptions = await _unitOfWork.Repository<UserSubscription>()
            .FindAsync(s => s.IsOnTrial && s.TrialEndsAt != null && s.TrialEndsAt > now && s.TrialEndsAt <= warningCutoff, cancellationToken);

        var warningCount = 0;
        var failedCount = 0;

        foreach (var subscription in expiringSubscriptions)
        {
            try
            {
                var trials = await _unitOfWork.Repository<UserTrial>()
                    .FindAsync(t => t.UserId == subscription.UserId, cancellationToken);

                var trial = trials.FirstOrDefault();
                if (trial == null) continue;

                // Skip if already warned within last 24 hours
                if (trial.LastExpirationWarningAt.HasValue && trial.LastExpirationWarningAt.Value > now.AddHours(-24))
                    continue;

                var daysRemaining = (int)Math.Ceiling((subscription.TrialEndsAt!.Value - now).TotalDays);
                if (daysRemaining < 1) daysRemaining = 1;

                var user = await _unitOfWork.Users.GetByIdAsync(subscription.UserId, cancellationToken);
                if (user == null) continue;

                var html = BuildTrialWarningEmail(user.Name, user.BusinessName, daysRemaining);
                var emailResult = await _emailService.SendEmailAsync(
                    user.Email,
                    $"Your SpeakHalo Trial Expires in {daysRemaining} Day{(daysRemaining == 1 ? "" : "s")}",
                    html,
                    cancellationToken: cancellationToken);

                if (!emailResult.IsSuccess)
                {
                    _logger.LogWarning("Failed to send trial warning email to {Email}: {Errors}",
                        user.Email, string.Join(", ", emailResult.Errors));
                    continue;
                }

                // Only update warning tracking if email succeeded
                trial.LastExpirationWarningAt = now;
                trial.LastWarningDaysRemaining = daysRemaining;
                _unitOfWork.Repository<UserTrial>().Update(trial);

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                warningCount++;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                failedCount++;
                _logger.LogError(ex, "Failed to send trial warning for user {UserId}", subscription.UserId);
            }
        }

        _logger.LogInformation("Trial warning check complete. Sent: {Sent}, Failed: {Failed}", warningCount, failedCount);
    }

    private async Task InvalidateUserCacheAsync(string userId)
    {
        try
        {
            var cacheKey = $"user_active:{userId}";
            await _cache.RemoveAsync(cacheKey);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to invalidate cache for user {UserId}", userId);
        }
    }

    private string BuildTrialExpiredEmail(string userName, string businessName)
    {
        return $"""
            <html>
            <body style="font-family: Arial, sans-serif; line-height: 1.6; color: #333;">
                <div style="max-width: 600px; margin: 0 auto; padding: 20px;">
                    <h2 style="color: #1a73e8;">Your SpeakHalo Trial Has Ended</h2>
                    <p>Hi {userName},</p>
                    <p>Your free trial for <strong>{businessName}</strong> on SpeakHalo has ended.</p>
                    <p>Your account has been deactivated. To continue using SpeakHalo's AI-powered voice assistant for your cleaning business, please contact our team to set up your account.</p>
                    <p>During your trial, you experienced:</p>
                    <ul>
                        <li>AI-powered call handling for your cleaning business</li>
                        <li>Automated appointment scheduling</li>
                        <li>Customer management and follow-ups</li>
                        <li>WhatsApp and SMS integration</li>
                    </ul>
                    <p>Don't lose the momentum you've built! Contact us to keep your AI receptionist running.</p>
                    <p>
                        <a href="mailto:{_supportEmail}" style="display: inline-block; background: #1a73e8; color: white; padding: 12px 24px; text-decoration: none; border-radius: 6px;">Contact Us to Continue</a>
                    </p>
                    <p style="color: #666; font-size: 14px;">Thank you for trying SpeakHalo!</p>
                </div>
            </body>
            </html>
            """;
    }

    private string BuildTrialWarningEmail(string userName, string businessName, int daysRemaining)
    {
        var dayText = daysRemaining == 1 ? "1 day" : $"{daysRemaining} days";
        return $"""
            <html>
            <body style="font-family: Arial, sans-serif; line-height: 1.6; color: #333;">
                <div style="max-width: 600px; margin: 0 auto; padding: 20px;">
                    <h2 style="color: #e8a01a;">Your SpeakHalo Trial Expires in {dayText}</h2>
                    <p>Hi {userName},</p>
                    <p>Just a heads up — the free trial for <strong>{businessName}</strong> on SpeakHalo expires in <strong>{dayText}</strong>.</p>
                    <p>Once your trial ends, your AI receptionist will stop answering calls and your account will be deactivated.</p>
                    <p>To keep your cleaning business running smoothly with SpeakHalo, contact our team to set up your account before your trial ends.</p>
                    <p>
                        <a href="mailto:{_supportEmail}" style="display: inline-block; background: #1a73e8; color: white; padding: 12px 24px; text-decoration: none; border-radius: 6px;">Contact Us Now</a>
                    </p>
                    <p style="color: #666; font-size: 14px;">Thank you for using SpeakHalo!</p>
                </div>
            </body>
            </html>
            """;
    }
}
