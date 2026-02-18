using SpeakHalo.Application.DTOs;
using SpeakHalo.Shared.Models;

namespace SpeakHalo.Application.Interfaces;

public interface IUsageMeteringService
{
    Task<Result<bool>> RecordUsageAsync(RecordUsageRequest request, CancellationToken cancellationToken = default);
    Task<Result<UsageCostsResponse>> GetCurrentPeriodUsageAsync(string userId, CancellationToken cancellationToken = default);
    Task<Result<UsageSummaryResponse>> GetUsageSummaryAsync(string userId, CancellationToken cancellationToken = default);
    Task<Result<List<UsageCostsResponse>>> GetUsageHistoryAsync(string userId, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default);
    Task<Result<bool>> ResetBillingPeriodAsync(string userId, CancellationToken cancellationToken = default);
    Task RecordCallUsageAsync(string userId, int durationSeconds, decimal cost);
    Task RecordSmsUsageAsync(string userId, int count, decimal cost);
    Task RecordEmailUsageAsync(string userId, int count, decimal cost);
}
