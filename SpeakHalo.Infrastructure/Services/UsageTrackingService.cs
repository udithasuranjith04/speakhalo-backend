using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SpeakHalo.Application.Interfaces;
using SpeakHalo.Domain.Entities;
using SpeakHalo.Infrastructure.Data;

namespace SpeakHalo.Infrastructure.Services;

public class UsageTrackingService : IUsageTrackingService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<UsageTrackingService> _logger;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;

    // Warning thresholds
    private const int WARNING_THRESHOLD_80 = 80;
    private const int WARNING_THRESHOLD_90 = 90;
    private const int WARNING_THRESHOLD_100 = 100;

    public UsageTrackingService(
        ApplicationDbContext context,
        ILogger<UsageTrackingService> logger,
        IEmailService emailService,
        IConfiguration configuration)
    {
        _context = context;
        _logger = logger;
        _emailService = emailService;
        _configuration = configuration;
    }

    public async Task<UsageTrackingResult> TrackAIMinutesAsync(string userId, int minutesUsed)
    {
        try
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead);

            // Atomic increment + row-level lock via UPDATE
            var rowsAffected = await _context.Database.ExecuteSqlRawAsync(
                @"UPDATE ""UserSubscriptions"" SET
                    ""AIMinutesUsedThisMonth"" = ""AIMinutesUsedThisMonth"" + {0},
                    ""OverageChargesThisMonth"" = ""OverageChargesThisMonth"" +
                        GREATEST(0, (""AIMinutesUsedThisMonth"" + {0} - ""AIMinutesIncluded"") -
                        GREATEST(0, ""AIMinutesUsedThisMonth"" - ""AIMinutesIncluded"")) * ""AIOverageRate""
                WHERE ""UserId"" = {1}", minutesUsed, userId);

            if (rowsAffected == 0)
            {
                await transaction.RollbackAsync();
                return new UsageTrackingResult { Success = false, Message = "No subscription found" };
            }

            // Re-read within same transaction for consistent data
            var subscription = await _context.UserSubscriptions
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.UserId == userId);

            await transaction.CommitAsync();

            if (subscription == null)
                return new UsageTrackingResult { Success = false, Message = "No subscription found" };

            var percentageUsed = subscription.AIMinutesIncluded > 0
                ? (int)((subscription.AIMinutesUsedThisMonth / (double)subscription.AIMinutesIncluded) * 100)
                : 100;

            await CheckAndSendWarningsAsync(userId, UsageType.AIMinutes, percentageUsed);

            _logger.LogInformation("Tracked {Minutes} AI minutes for user {UserId}. Total: {Total}/{Included}",
                minutesUsed, userId, subscription.AIMinutesUsedThisMonth, subscription.AIMinutesIncluded);

            return new UsageTrackingResult
            {
                Success = true,
                RemainingQuota = Math.Max(0, subscription.AIMinutesIncluded - subscription.AIMinutesUsedThisMonth),
                PercentageUsed = percentageUsed,
                Message = $"Used {subscription.AIMinutesUsedThisMonth} of {subscription.AIMinutesIncluded} AI minutes"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error tracking AI minutes for user {UserId}", userId);
            throw;
        }
    }

    public async Task<UsageTrackingResult> TrackWhatsAppMessageAsync(string userId)
    {
        try
        {
            // Get country-specific WhatsApp overage rate before atomic update
            var whatsAppOverageRate = await GetCountryWhatsAppOverageRateAsync(userId);

            await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead);

            // Atomic increment + row-level lock via UPDATE
            var rowsAffected = await _context.Database.ExecuteSqlRawAsync(
                @"UPDATE ""UserSubscriptions"" SET
                    ""WhatsAppMessagesUsedThisMonth"" = ""WhatsAppMessagesUsedThisMonth"" + 1,
                    ""OverageChargesThisMonth"" = ""OverageChargesThisMonth"" +
                        CASE WHEN ""WhatsAppMessagesUsedThisMonth"" + 1 > ""WhatsAppMessagesIncluded""
                            THEN {0} ELSE 0 END
                WHERE ""UserId"" = {1}", whatsAppOverageRate, userId);

            if (rowsAffected == 0)
            {
                await transaction.RollbackAsync();
                return new UsageTrackingResult { Success = false, Message = "No subscription found" };
            }

            var subscription = await _context.UserSubscriptions
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.UserId == userId);

            await transaction.CommitAsync();

            if (subscription == null)
                return new UsageTrackingResult { Success = false, Message = "No subscription found" };

            var percentageUsed = subscription.WhatsAppMessagesIncluded > 0
                ? (int)((subscription.WhatsAppMessagesUsedThisMonth / (double)subscription.WhatsAppMessagesIncluded) * 100)
                : 100;

            await CheckAndSendWarningsAsync(userId, UsageType.WhatsAppMessages, percentageUsed);

            return new UsageTrackingResult
            {
                Success = true,
                RemainingQuota = Math.Max(0, subscription.WhatsAppMessagesIncluded - subscription.WhatsAppMessagesUsedThisMonth),
                PercentageUsed = percentageUsed,
                Message = $"Used {subscription.WhatsAppMessagesUsedThisMonth} of {subscription.WhatsAppMessagesIncluded} WhatsApp messages"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error tracking WhatsApp message for user {UserId}", userId);
            throw;
        }
    }

    public async Task<UsageTrackingResult> TrackSMSMessageAsync(string userId)
    {
        try
        {
            // Get country-specific SMS overage rate before atomic update
            var smsOverageRate = await GetCountrySmsOverageRateAsync(userId);

            await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead);

            // Atomic increment + row-level lock via UPDATE
            var rowsAffected = await _context.Database.ExecuteSqlRawAsync(
                @"UPDATE ""UserSubscriptions"" SET
                    ""SMSMessagesUsedThisMonth"" = ""SMSMessagesUsedThisMonth"" + 1,
                    ""OverageChargesThisMonth"" = ""OverageChargesThisMonth"" +
                        CASE WHEN ""SMSMessagesUsedThisMonth"" + 1 > ""SMSMessagesIncluded""
                            THEN {0} ELSE 0 END
                WHERE ""UserId"" = {1}", smsOverageRate, userId);

            if (rowsAffected == 0)
            {
                await transaction.RollbackAsync();
                return new UsageTrackingResult { Success = false, Message = "No subscription found" };
            }

            var subscription = await _context.UserSubscriptions
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.UserId == userId);

            await transaction.CommitAsync();

            if (subscription == null)
                return new UsageTrackingResult { Success = false, Message = "No subscription found" };

            var percentageUsed = subscription.SMSMessagesIncluded > 0
                ? (int)((subscription.SMSMessagesUsedThisMonth / (double)subscription.SMSMessagesIncluded) * 100)
                : 100;

            await CheckAndSendWarningsAsync(userId, UsageType.SMSMessages, percentageUsed);

            return new UsageTrackingResult
            {
                Success = true,
                RemainingQuota = Math.Max(0, subscription.SMSMessagesIncluded - subscription.SMSMessagesUsedThisMonth),
                PercentageUsed = percentageUsed,
                Message = $"Used {subscription.SMSMessagesUsedThisMonth} of {subscription.SMSMessagesIncluded} SMS messages"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error tracking SMS message for user {UserId}", userId);
            throw;
        }
    }

    private async Task<decimal> GetCountrySmsOverageRateAsync(string userId)
    {
        const decimal defaultRate = 0.05m;

        try
        {
            var countryCode = await GetUserPhoneCountryCodeAsync(userId);
            var configRate = _configuration.GetValue<decimal?>($"Twilio:CountryPricing:{countryCode}:SmsOverageRate");
            return configRate ?? defaultRate;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error looking up country SMS overage rate for user {UserId}, using default", userId);
            return defaultRate;
        }
    }

    private async Task<decimal> GetCountryWhatsAppOverageRateAsync(string userId)
    {
        const decimal defaultRate = 0.02m;

        try
        {
            var countryCode = await GetUserPhoneCountryCodeAsync(userId);
            var configRate = _configuration.GetValue<decimal?>($"Twilio:CountryPricing:{countryCode}:WhatsAppOverageRate");
            return configRate ?? defaultRate;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error looking up country WhatsApp overage rate for user {UserId}, using default", userId);
            return defaultRate;
        }
    }

    private async Task<string> GetUserPhoneCountryCodeAsync(string userId)
    {
        var phoneNumber = await _context.Set<PhoneNumber>()
            .AsNoTracking()
            .Where(p => p.UserId == userId && p.IsPrimary && !p.IsDeleted)
            .FirstOrDefaultAsync();

        return phoneNumber?.CountryCode ?? "US";
    }

    public async Task<ImageQuotaStatus> CheckImageQuotaAsync(string userId)
    {
        try
        {
            var subscription = await _context.UserSubscriptions
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (subscription == null)
            {
                return new ImageQuotaStatus
                {
                    CanProcess = false,
                    Status = "no_subscription",
                    Message = "No active subscription found"
                };
            }

            var used = subscription.ImagesUsedThisMonth;
            var included = subscription.ImagesIncluded;
            var gracePeriodLimit = (int)(included * 1.5m); // 150% soft limit
            var percentageUsed = included > 0 ? (int)((used / (double)included) * 100) : 0;

            // Determine status and if can process
            if (used >= gracePeriodLimit)
            {
                // Hard limit reached (150%)
                return new ImageQuotaStatus
                {
                    CanProcess = false,
                    Status = "blocked",
                    Used = used,
                    Included = included,
                    Remaining = 0,
                    GracePeriodLimit = gracePeriodLimit,
                    PercentageUsed = percentageUsed,
                    Message = $"Image quota exceeded! You've used {used}/{included} images plus the 50% grace period. Contact your administrator to increase your limit.",
                    ShouldShowWarning = false,
                    ShouldShowUpgrade = true
                };
            }
            else if (used >= included)
            {
                // In grace period (100-150%)
                var gracePeriodRemaining = gracePeriodLimit - used;
                return new ImageQuotaStatus
                {
                    CanProcess = true,
                    Status = "grace",
                    Used = used,
                    Included = included,
                    Remaining = gracePeriodRemaining,
                    GracePeriodLimit = gracePeriodLimit,
                    PercentageUsed = percentageUsed,
                    Message = $"You're in the grace period! {gracePeriodRemaining} images remaining before blocking. Contact your administrator to increase your limit.",
                    ShouldShowWarning = true,
                    ShouldShowUpgrade = true
                };
            }
            else if (used >= (int)(included * 0.8m))
            {
                // Warning zone (80-100%)
                var remaining = included - used;
                return new ImageQuotaStatus
                {
                    CanProcess = true,
                    Status = "warning",
                    Used = used,
                    Included = included,
                    Remaining = remaining,
                    GracePeriodLimit = gracePeriodLimit,
                    PercentageUsed = percentageUsed,
                    Message = $"You've used {used}/{included} images ({percentageUsed}%). {remaining} remaining this month.",
                    ShouldShowWarning = true,
                    ShouldShowUpgrade = false
                };
            }
            else
            {
                // Normal usage (0-80%)
                var remaining = included - used;
                return new ImageQuotaStatus
                {
                    CanProcess = true,
                    Status = "ok",
                    Used = used,
                    Included = included,
                    Remaining = remaining,
                    GracePeriodLimit = gracePeriodLimit,
                    PercentageUsed = percentageUsed,
                    Message = $"{remaining} images remaining this month.",
                    ShouldShowWarning = false,
                    ShouldShowUpgrade = false
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking image quota for user {UserId}", userId);
            throw;
        }
    }

    public async Task<UsageTrackingResult> TrackImageAnalysisAsync(string userId, string imageUrl)
    {
        try
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead);

            // Check quota and increment atomically within transaction
            var quotaStatus = await CheckImageQuotaAsync(userId);

            if (!quotaStatus.CanProcess)
            {
                await transaction.RollbackAsync();
                return new UsageTrackingResult
                {
                    Success = false,
                    RemainingQuota = 0,
                    PercentageUsed = quotaStatus.PercentageUsed,
                    Message = quotaStatus.Message
                };
            }

            // Atomic increment + row-level lock via UPDATE
            var rowsAffected = await _context.Database.ExecuteSqlRawAsync(
                @"UPDATE ""UserSubscriptions"" SET ""ImagesUsedThisMonth"" = ""ImagesUsedThisMonth"" + 1
                WHERE ""UserId"" = {0}", userId);

            if (rowsAffected == 0)
            {
                await transaction.RollbackAsync();
                return new UsageTrackingResult { Success = false, Message = "No subscription found" };
            }

            var subscription = await _context.UserSubscriptions
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (subscription == null)
                return new UsageTrackingResult { Success = false, Message = "No subscription found" };

            // Create ImageAnalysis record
            var imageAnalysis = new ImageAnalysis
            {
                UserId = userId,
                ImageUrl = imageUrl,
                Status = "completed",
                BillingMonth = DateTime.UtcNow,
                CountsTowardQuota = true,
                ProcessingCost = subscription.ImageOverageRate,
                Model = "gemini-2.0-flash"
            };

            _context.Set<ImageAnalysis>().Add(imageAnalysis);
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            var percentageUsed = subscription.ImagesIncluded > 0
                ? (int)((subscription.ImagesUsedThisMonth / (double)subscription.ImagesIncluded) * 100)
                : 100;

            await CheckAndSendWarningsAsync(userId, UsageType.Images, percentageUsed);

            var updatedStatus = await CheckImageQuotaAsync(userId);

            _logger.LogInformation("Tracked image analysis for user {UserId}. Used: {Used}/{Included}, Status: {Status}",
                userId, subscription.ImagesUsedThisMonth, subscription.ImagesIncluded, updatedStatus.Status);

            return new UsageTrackingResult
            {
                Success = true,
                RemainingQuota = updatedStatus.Remaining,
                PercentageUsed = percentageUsed,
                Message = updatedStatus.Message
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error tracking image analysis for user {UserId}", userId);
            throw;
        }
    }

    public async Task<UserUsageReport> GetUsageReportAsync(string userId)
    {
        var subscription = await _context.UserSubscriptions
            .FirstOrDefaultAsync(s => s.UserId == userId);

        if (subscription == null)
        {
            throw new InvalidOperationException($"No subscription found for user {userId}");
        }

        var report = new UserUsageReport
        {
            UserId = userId,
            Plan = subscription.Plan ?? "Unknown",

            // AI Minutes
            AIMinutesIncluded = subscription.AIMinutesIncluded,
            AIMinutesUsed = subscription.AIMinutesUsedThisMonth,
            AIMinutesRemaining = Math.Max(0, subscription.AIMinutesIncluded - subscription.AIMinutesUsedThisMonth),

            // WhatsApp
            WhatsAppMessagesIncluded = subscription.WhatsAppMessagesIncluded,
            WhatsAppMessagesUsed = subscription.WhatsAppMessagesUsedThisMonth,
            WhatsAppMessagesRemaining = Math.Max(0, subscription.WhatsAppMessagesIncluded - subscription.WhatsAppMessagesUsedThisMonth),

            // SMS
            SMSMessagesIncluded = subscription.SMSMessagesIncluded,
            SMSMessagesUsed = subscription.SMSMessagesUsedThisMonth,
            SMSMessagesRemaining = Math.Max(0, subscription.SMSMessagesIncluded - subscription.SMSMessagesUsedThisMonth),

            // Images (soft limits)
            ImagesIncluded = subscription.ImagesIncluded,
            ImagesUsed = subscription.ImagesUsedThisMonth,
            ImagesRemaining = Math.Max(0, subscription.ImagesIncluded - subscription.ImagesUsedThisMonth),
            ImagesGracePeriod = (int)(subscription.ImagesIncluded * 0.5m),
            ImagesSoftLimitReached = subscription.ImagesUsedThisMonth >= subscription.ImagesIncluded,
            ImagesHardLimitReached = subscription.ImagesUsedThisMonth >= (int)(subscription.ImagesIncluded * 1.5m),
            ImageQuotaStatus = subscription.ImagesUsedThisMonth >= (int)(subscription.ImagesIncluded * 1.5m) ? "blocked" :
                              subscription.ImagesUsedThisMonth >= subscription.ImagesIncluded ? "grace" :
                              subscription.ImagesUsedThisMonth >= (int)(subscription.ImagesIncluded * 0.8m) ? "warning" : "ok",

            // Billing Cycle
            BillingCycleStartDate = subscription.BillingCycleStartDate,
            NextBillingDate = subscription.NextBillingDate
        };

        // Add warning messages
        if (report.AIMinutesUsed >= report.AIMinutesIncluded)
        {
            report.NeedsWarning = true;
            report.WarningMessages.Add($"AI minutes exceeded! Contact your administrator to increase your limit.");
        }
        else if (report.AIMinutesUsed >= report.AIMinutesIncluded * 0.9m)
        {
            report.NeedsWarning = true;
            report.WarningMessages.Add($"AI minutes at {(report.AIMinutesUsed * 100 / report.AIMinutesIncluded)}% capacity");
        }

        // Image warnings (soft limits)
        if (report.ImagesHardLimitReached)
        {
            report.NeedsWarning = true;
            report.WarningMessages.Add($"Image quota blocked! You've exceeded your limit including grace period. Contact your administrator.");
        }
        else if (report.ImagesSoftLimitReached)
        {
            var gracePeriodRemaining = (int)(report.ImagesIncluded * 1.5m) - report.ImagesUsed;
            report.NeedsWarning = true;
            report.WarningMessages.Add($"Image quota in grace period! {gracePeriodRemaining} images remaining before blocking.");
        }
        else if (report.ImagesUsed >= report.ImagesIncluded * 0.8m)
        {
            report.NeedsWarning = true;
            report.WarningMessages.Add($"Images at {(report.ImagesUsed * 100 / report.ImagesIncluded)}% capacity");
        }

        return report;
    }

    public async Task<bool> HasRemainingQuotaAsync(string userId, UsageType usageType, int amount = 1)
    {
        var subscription = await _context.UserSubscriptions
            .FirstOrDefaultAsync(s => s.UserId == userId);

        if (subscription == null) return false;

        return usageType switch
        {
            UsageType.AIMinutes => subscription.AIMinutesUsedThisMonth + amount <= subscription.AIMinutesIncluded,
            UsageType.WhatsAppMessages => subscription.WhatsAppMessagesUsedThisMonth + amount <= subscription.WhatsAppMessagesIncluded,
            UsageType.SMSMessages => subscription.SMSMessagesUsedThisMonth + amount <= subscription.SMSMessagesIncluded,
            UsageType.Images => subscription.ImagesUsedThisMonth + amount <= (int)(subscription.ImagesIncluded * 1.5m), // 150% soft limit
            _ => false
        };
    }

    public async Task ResetMonthlyUsageAsync()
    {
        try
        {
            var now = DateTime.UtcNow;

            var subscriptionsToReset = await _context.UserSubscriptions
                .Where(s => s.NextBillingDate <= now && s.HasActivePayment)
                .ToListAsync();

            _logger.LogInformation("Resetting monthly usage for {Count} subscriptions", subscriptionsToReset.Count);

            foreach (var subscription in subscriptionsToReset)
            {
                // Archive current month's usage (you could save to a usage history table here)
                _logger.LogInformation("User {UserId} used: AI={AI}, WhatsApp={WA}, SMS={SMS}, Images={Images}",
                    subscription.UserId,
                    subscription.AIMinutesUsedThisMonth,
                    subscription.WhatsAppMessagesUsedThisMonth,
                    subscription.SMSMessagesUsedThisMonth,
                    subscription.ImagesUsedThisMonth);

                // Reset usage counters
                subscription.AIMinutesUsedThisMonth = 0;
                subscription.WhatsAppMessagesUsedThisMonth = 0;
                subscription.SMSMessagesUsedThisMonth = 0;
                subscription.ImagesUsedThisMonth = 0;

                // Update billing cycle
                subscription.BillingCycleStartDate = now;
                subscription.NextBillingDate = now.AddMonths(1);
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Monthly usage reset completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting monthly usage");
            throw;
        }
    }

    public async Task SendUsageWarningAsync(string userId, UsageType usageType, int percentageUsed)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return;

            string resourceName = usageType switch
            {
                UsageType.AIMinutes => "AI voice minutes",
                UsageType.WhatsAppMessages => "WhatsApp messages",
                UsageType.SMSMessages => "SMS messages",
                UsageType.Images => "image analysis quota",
                _ => "resources"
            };

            string subject = $"SpeakHalo: {resourceName} usage at {percentageUsed}%";
            string message = $"Hello,\n\n" +
                           $"Your {resourceName} usage has reached {percentageUsed}% of your monthly limit.\n\n" +
                           $"You can view your detailed usage in the dashboard. Contact your administrator to increase your limits.\n\n" +
                           $"Best regards,\nThe SpeakHalo Team";

            await _emailService.SendEmailAsync(user.Email, subject, message);

            _logger.LogInformation("Sent {Percentage}% usage warning to user {UserId} for {Type}",
                percentageUsed, userId, usageType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending usage warning to user {UserId}", userId);
        }
    }

    private async Task CheckAndSendWarningsAsync(string userId, UsageType usageType, int percentageUsed)
    {
        // Determine which threshold we're at
        int threshold;
        if (percentageUsed >= WARNING_THRESHOLD_100)
            threshold = 100;
        else if (percentageUsed >= WARNING_THRESHOLD_90)
            threshold = 90;
        else if (percentageUsed >= WARNING_THRESHOLD_80)
            threshold = 80;
        else
            return; // Below warning threshold

        // Check if we've already sent this specific warning this billing period
        var existingWarning = await _context.Set<Domain.Entities.Notification>()
            .AsNoTracking()
            .AnyAsync(n => n.UserId == userId
                && n.Type == $"usage_warning_{usageType}_{threshold}"
                && n.CreatedAt >= DateTime.UtcNow.AddDays(-30));

        if (existingWarning)
        {
            _logger.LogDebug("Usage warning {Threshold}% for {UsageType} already sent to user {UserId} this period",
                threshold, usageType, userId);
            return;
        }

        await SendUsageWarningAsync(userId, usageType, threshold);

        // Record that we sent this warning
        var notification = new Domain.Entities.Notification
        {
            UserId = userId,
            Type = $"usage_warning_{usageType}_{threshold}",
            Title = $"Usage Warning: {usageType} at {threshold}%",
            Message = $"Your {usageType} usage has reached {threshold}% of your monthly limit.",
            IsRead = false
        };
        _context.Set<Domain.Entities.Notification>().Add(notification);
        await _context.SaveChangesAsync();
    }
}
