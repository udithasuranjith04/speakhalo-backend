using SpeakHalo.Application.DTOs.Analytics;

namespace SpeakHalo.Application.Interfaces;

public interface IAnalyticsService
{
    Task<CallAnalyticsDto> GetCallAnalyticsAsync(string userId, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default);
    Task<RevenueAnalyticsDto> GetRevenueAnalyticsAsync(string userId, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default);
    Task<CustomerAnalyticsDto> GetCustomerAnalyticsAsync(string userId, CancellationToken cancellationToken = default);
    Task<AppointmentAnalyticsDto> GetAppointmentAnalyticsAsync(string userId, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default);
    Task<UsageAnalyticsDto> GetUsageAnalyticsAsync(string userId, int months = 6, CancellationToken cancellationToken = default);
    Task<PerformanceMetricsDto> GetPerformanceMetricsAsync(string userId, CancellationToken cancellationToken = default);
    Task<Dictionary<string, object>> GetBusinessInsightsAsync(string userId, CancellationToken cancellationToken = default);
    Task<Dictionary<string, double>> CalculateMonthlyChurnRateAsync(int months = 12, CancellationToken cancellationToken = default);
    Task<ChurnAnalyticsDto> GetChurnAnalyticsAsync(int months = 12, CancellationToken cancellationToken = default);
}
