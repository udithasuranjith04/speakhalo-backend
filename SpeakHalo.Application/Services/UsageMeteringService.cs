using Microsoft.Extensions.Logging;
using SpeakHalo.Application.DTOs;
using SpeakHalo.Application.Interfaces;
using SpeakHalo.Domain.Entities;
using SpeakHalo.Domain.Interfaces.Repositories;
using SpeakHalo.Shared.Constants;
using SpeakHalo.Shared.Models;

namespace SpeakHalo.Application.Services;

public class UsageMeteringService : IUsageMeteringService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UsageMeteringService> _logger;

    public UsageMeteringService(IUnitOfWork unitOfWork, ILogger<UsageMeteringService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<bool>> RecordUsageAsync(RecordUsageRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var usageCosts = await GetOrCreateCurrentPeriodAsync(request.UserId, cancellationToken);

            if (request.AiCosts.HasValue)
                usageCosts.AiCosts += request.AiCosts.Value;

            if (request.TwilioCosts.HasValue)
                usageCosts.TwilioCosts += request.TwilioCosts.Value;

            if (request.SendGridCosts.HasValue)
                usageCosts.SendGridCosts += request.SendGridCosts.Value;

            if (request.StorageCosts.HasValue)
                usageCosts.StorageCosts += request.StorageCosts.Value;

            if (request.CallMinutes.HasValue)
            {
                usageCosts.TotalMinutes += request.CallMinutes.Value;
                usageCosts.TotalCalls++;
            }

            if (request.SmsCount.HasValue)
                usageCosts.TotalSms += request.SmsCount.Value;

            if (request.EmailCount.HasValue)
                usageCosts.TotalEmails += request.EmailCount.Value;

            usageCosts.TotalCosts = usageCosts.AiCosts + usageCosts.TwilioCosts + usageCosts.SendGridCosts + usageCosts.StorageCosts;

            _unitOfWork.Repository<UsageCosts>().Update(usageCosts);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record usage for user {UserId}", request.UserId);
            return Result<bool>.Failure($"Failed to record usage: {ex.Message}");
        }
    }

    public async Task<Result<UsageCostsResponse>> GetCurrentPeriodUsageAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var usageCosts = await GetOrCreateCurrentPeriodAsync(userId, cancellationToken);
            return Result<UsageCostsResponse>.Success(MapToResponse(usageCosts));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get current period usage for user {UserId}", userId);
            return Result<UsageCostsResponse>.Failure($"Failed to get current period usage: {ex.Message}");
        }
    }

    public async Task<Result<UsageSummaryResponse>> GetUsageSummaryAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentPeriod = await GetOrCreateCurrentPeriodAsync(userId, cancellationToken);

            var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var yearStart = new DateTime(DateTime.UtcNow.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            var allUsage = await _unitOfWork.Repository<UsageCosts>()
                .FindAsync(u => u.UserId == userId, cancellationToken);

            var monthToDateCosts = allUsage
                .Where(u => u.BillingPeriodStart >= monthStart)
                .Sum(u => u.TotalCosts);

            var yearToDateCosts = allUsage
                .Where(u => u.BillingPeriodStart >= yearStart)
                .Sum(u => u.TotalCosts);

            var daysInPeriod = (DateTime.UtcNow - currentPeriod.BillingPeriodStart).Days + 1;
            var averageDailyCosts = daysInPeriod > 0 ? currentPeriod.TotalCosts / daysInPeriod : 0;

            var daysRemainingInMonth = DateTime.DaysInMonth(DateTime.UtcNow.Year, DateTime.UtcNow.Month) - DateTime.UtcNow.Day;
            var projectedMonthlyCosts = monthToDateCosts + (averageDailyCosts * daysRemainingInMonth);

            var summary = new UsageSummaryResponse
            {
                UserId = userId,
                CurrentPeriodCosts = currentPeriod.TotalCosts,
                CurrentPeriodMinutes = currentPeriod.TotalMinutes,
                CurrentPeriodSms = currentPeriod.TotalSms,
                CurrentPeriodEmails = currentPeriod.TotalEmails,
                MonthToDateCosts = monthToDateCosts,
                YearToDateCosts = yearToDateCosts,
                AverageDailyCosts = averageDailyCosts,
                ProjectedMonthlyCosts = projectedMonthlyCosts
            };

            return Result<UsageSummaryResponse>.Success(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get usage summary for user {UserId}", userId);
            return Result<UsageSummaryResponse>.Failure($"Failed to get usage summary: {ex.Message}");
        }
    }

    public async Task<Result<List<UsageCostsResponse>>> GetUsageHistoryAsync(string userId, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = await _unitOfWork.Repository<UsageCosts>()
                .FindAsync(u => u.UserId == userId, cancellationToken);

            if (startDate.HasValue)
                query = query.Where(u => u.BillingPeriodStart >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(u => u.BillingPeriodEnd <= endDate.Value);

            var history = query
                .OrderByDescending(u => u.BillingPeriodStart)
                .Select(MapToResponse)
                .ToList();

            return Result<List<UsageCostsResponse>>.Success(history);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get usage history for user {UserId}", userId);
            return Result<List<UsageCostsResponse>>.Failure($"Failed to get usage history: {ex.Message}");
        }
    }

    public async Task<Result<bool>> ResetBillingPeriodAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var newPeriod = new UsageCosts
            {
                UserId = userId,
                BillingPeriodStart = DateTime.UtcNow,
                BillingPeriodEnd = DateTime.UtcNow.AddMonths(1)
            };

            await _unitOfWork.Repository<UsageCosts>().AddAsync(newPeriod, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reset billing period for user {UserId}", userId);
            return Result<bool>.Failure($"Failed to reset billing period: {ex.Message}");
        }
    }

    public async Task RecordCallUsageAsync(string userId, int durationSeconds, decimal cost)
    {
        try
        {
            var usageCosts = await GetOrCreateCurrentPeriodAsync(userId);
            usageCosts.TotalMinutes += (int)Math.Ceiling(durationSeconds / 60.0);
            usageCosts.TotalCalls++;
            usageCosts.TwilioCosts += cost;
            usageCosts.TotalCosts = usageCosts.AiCosts + usageCosts.TwilioCosts + usageCosts.SendGridCosts + usageCosts.StorageCosts;

            _unitOfWork.Repository<UsageCosts>().Update(usageCosts);
            await _unitOfWork.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record call usage for user {UserId}", userId);
        }
    }

    public async Task RecordSmsUsageAsync(string userId, int count, decimal cost)
    {
        try
        {
            var usageCosts = await GetOrCreateCurrentPeriodAsync(userId);
            usageCosts.TotalSms += count;
            usageCosts.TwilioCosts += cost;
            usageCosts.TotalCosts = usageCosts.AiCosts + usageCosts.TwilioCosts + usageCosts.SendGridCosts + usageCosts.StorageCosts;

            _unitOfWork.Repository<UsageCosts>().Update(usageCosts);
            await _unitOfWork.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record SMS usage for user {UserId}", userId);
        }
    }

    public async Task RecordEmailUsageAsync(string userId, int count, decimal cost)
    {
        try
        {
            var usageCosts = await GetOrCreateCurrentPeriodAsync(userId);
            usageCosts.TotalEmails += count;
            usageCosts.SendGridCosts += cost;
            usageCosts.TotalCosts = usageCosts.AiCosts + usageCosts.TwilioCosts + usageCosts.SendGridCosts + usageCosts.StorageCosts;

            _unitOfWork.Repository<UsageCosts>().Update(usageCosts);
            await _unitOfWork.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record email usage for user {UserId}", userId);
        }
    }

    private async Task<UsageCosts> GetOrCreateCurrentPeriodAsync(string userId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var periodStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var periodEnd = periodStart.AddMonths(1);

        var existingUsage = await _unitOfWork.Repository<UsageCosts>()
            .FirstOrDefaultAsync(u => u.UserId == userId && u.BillingPeriodStart == periodStart, cancellationToken);

        if (existingUsage != null)
            return existingUsage;

        var newUsage = new UsageCosts
        {
            UserId = userId,
            BillingPeriodStart = periodStart,
            BillingPeriodEnd = periodEnd
        };

        await _unitOfWork.Repository<UsageCosts>().AddAsync(newUsage, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return newUsage;
    }

    private static UsageCostsResponse MapToResponse(UsageCosts usageCosts)
    {
        return new UsageCostsResponse
        {
            Id = usageCosts.Id,
            UserId = usageCosts.UserId,
            BillingPeriodStart = usageCosts.BillingPeriodStart,
            BillingPeriodEnd = usageCosts.BillingPeriodEnd,
            AiCosts = usageCosts.AiCosts,
            TwilioCosts = usageCosts.TwilioCosts,
            SendGridCosts = usageCosts.SendGridCosts,
            StorageCosts = usageCosts.StorageCosts,
            TotalCosts = usageCosts.TotalCosts,
            TotalCalls = usageCosts.TotalCalls,
            TotalMinutes = usageCosts.TotalMinutes,
            TotalSms = usageCosts.TotalSms,
            TotalEmails = usageCosts.TotalEmails,
            CreatedAt = usageCosts.CreatedAt,
            UpdatedAt = usageCosts.UpdatedAt
        };
    }
}
