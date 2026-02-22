using Microsoft.Extensions.Logging;
using SpeakHalo.Application.DTOs.Analytics;
using SpeakHalo.Application.Interfaces;
using SpeakHalo.Domain.Entities;
using SpeakHalo.Domain.Enums;
using SpeakHalo.Domain.Interfaces.Repositories;

namespace SpeakHalo.Application.Services;

public class AnalyticsService : IAnalyticsService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cacheService;
    private readonly ILogger<AnalyticsService> _logger;

    public AnalyticsService(
        IUnitOfWork unitOfWork,
        ICacheService cacheService,
        ILogger<AnalyticsService> logger)
    {
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<CallAnalyticsDto> GetCallAnalyticsAsync(string userId, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"analytics:calls:{userId}:{startDate?.ToString("yyyyMMdd")}:{endDate?.ToString("yyyyMMdd")}";

        return await _cacheService.GetOrSetAsync(cacheKey, async () =>
        {
            var calls = await GetCallsInRangeAsync(userId, startDate, endDate, cancellationToken);
            var callsList = calls.ToList();

            var totalCalls = callsList.Count;
            var missedCalls = callsList.Count(c => c.Outcome == CallOutcome.Missed || c.Outcome == CallOutcome.Voicemail);
            var answeredCalls = totalCalls - missedCalls;
            var callsWithAppointments = callsList.Count(c => c.AppointmentScheduled);
            var callsWithQuotes = callsList.Count(c => c.QuoteRequested);
            var callsWithDuration = callsList.Where(c => c.DurationSeconds > 0).ToList();

            return new CallAnalyticsDto
            {
                TotalCalls = totalCalls,
                AnsweredCalls = answeredCalls,
                MissedCalls = missedCalls,
                AverageCallDuration = callsWithDuration.Any() ? callsWithDuration.Average(c => c.DurationSeconds) : 0,
                TotalMinutes = callsList.Sum(c => c.DurationSeconds) / 60,
                TotalCost = callsList.Sum(c => c.Cost),
                AnswerRate = totalCalls > 0 ? (double)answeredCalls / totalCalls * 100 : 0,
                CallsWithAppointments = callsWithAppointments,
                CallsWithQuotes = callsWithQuotes,
                ConversionRate = answeredCalls > 0 ? (double)callsWithAppointments / answeredCalls * 100 : 0,
                DailyTrends = callsList
                    .GroupBy(c => c.CreatedAt.Date)
                    .Select(g => new CallTrendDto
                    {
                        Date = g.Key,
                        CallCount = g.Count(),
                        Duration = g.Sum(c => c.DurationSeconds),
                        Cost = g.Sum(c => c.Cost)
                    })
                    .OrderBy(t => t.Date)
                    .ToList(),
                CallsByHour = callsList
                    .GroupBy(c => c.CreatedAt.Hour)
                    .ToDictionary(g => g.Key.ToString(), g => g.Count()),
                TopCustomers = callsList
                    .Where(c => !string.IsNullOrEmpty(c.CustomerId))
                    .GroupBy(c => new { c.CustomerId, c.CallerName })
                    .Select(g => new TopCustomerDto
                    {
                        CustomerId = g.Key.CustomerId ?? string.Empty,
                        CustomerName = g.Key.CallerName ?? string.Empty,
                        CallCount = g.Count(),
                        TotalMinutes = g.Sum(c => c.DurationSeconds) / 60
                    })
                    .OrderByDescending(t => t.CallCount)
                    .Take(10)
                    .ToList()
            };
        }, TimeSpan.FromMinutes(5), cancellationToken);
    }

    public async Task<RevenueAnalyticsDto> GetRevenueAnalyticsAsync(string userId, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"analytics:revenue:{userId}:{startDate?.ToString("yyyyMMdd")}:{endDate?.ToString("yyyyMMdd")}";

        return await _cacheService.GetOrSetAsync(cacheKey, async () =>
        {
            var invoices = await GetInvoicesInRangeAsync(userId, startDate, endDate, cancellationToken);
            var invoicesList = invoices.ToList();

            var paidInvoices = invoicesList.Where(i => i.Status == "paid").ToList();
            var totalRevenue = paidInvoices.Sum(i => i.Total);
            var monthlyRecurring = paidInvoices
                .Where(i => i.CreatedAt.Month == DateTime.UtcNow.Month && i.CreatedAt.Year == DateTime.UtcNow.Year)
                .Sum(i => i.Total);

            // Load line items for paid invoices to calculate revenue by category
            var paidInvoiceIds = paidInvoices.Select(i => i.Id).ToList();
            var lineItems = paidInvoiceIds.Count > 0
                ? (await _unitOfWork.Repository<InvoiceLineItem>()
                    .FindAsync(li => paidInvoiceIds.Contains(li.InvoiceId), cancellationToken)).ToList()
                : new List<InvoiceLineItem>();

            var revenueByCategory = lineItems
                .Where(li => !string.IsNullOrEmpty(li.Category))
                .GroupBy(li => li.Category)
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(li => li.Amount));

            return new RevenueAnalyticsDto
            {
                TotalRevenue = totalRevenue,
                MonthlyRecurring = monthlyRecurring,
                UsageRevenue = 0,
                AverageRevenuePerUser = invoicesList.Any()
                    ? totalRevenue / invoicesList.Select(i => i.UserId).Distinct().Count()
                    : 0,
                MonthlyTrends = invoicesList
                    .GroupBy(i => new { i.CreatedAt.Year, i.CreatedAt.Month })
                    .Select(g => new MonthlyRevenueDto
                    {
                        Month = $"{g.Key.Year}-{g.Key.Month:D2}",
                        Revenue = g.Where(i => i.Status == "paid").Sum(i => i.Total),
                        InvoiceCount = g.Count()
                    })
                    .OrderBy(m => m.Month)
                    .ToList(),
                RevenueByCategory = revenueByCategory,
                ProjectedMonthlyRevenue = monthlyRecurring
            };
        }, TimeSpan.FromMinutes(10), cancellationToken);
    }

    public async Task<CustomerAnalyticsDto> GetCustomerAnalyticsAsync(string userId, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"analytics:customers:{userId}";

        return await _cacheService.GetOrSetAsync(cacheKey, async () =>
        {
            var customers = await _unitOfWork.Repository<Customer>()
                .FindAsync(c => c.UserId == userId, cancellationToken);
            var customersList = customers.ToList();

            var thisMonthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
            var newThisMonth = customersList.Count(c => c.CreatedAt >= thisMonthStart);
            var activeCustomers = customersList.Count(c => c.JobCount > 0);

            // Calculate churn rate based on user subscriptions
            var churnRate = await CalculateChurnRateAsync(userId, cancellationToken);

            return new CustomerAnalyticsDto
            {
                TotalCustomers = customersList.Count,
                NewCustomersThisMonth = newThisMonth,
                ActiveCustomers = activeCustomers,
                CustomerRetentionRate = customersList.Count > 0 ? (double)activeCustomers / customersList.Count * 100 : 0,
                ChurnRate = churnRate,
                GrowthTrend = customersList
                    .GroupBy(c => new { c.CreatedAt.Year, c.CreatedAt.Month })
                    .Select(g => new CustomerGrowthDto
                    {
                        Month = $"{g.Key.Year}-{g.Key.Month:D2}",
                        NewCustomers = g.Count(),
                        TotalCustomers = customersList.Count(c => c.CreatedAt <= new DateTime(g.Key.Year, g.Key.Month, DateTime.DaysInMonth(g.Key.Year, g.Key.Month)))
                    })
                    .OrderBy(g => g.Month)
                    .ToList(),
                CustomersBySource = new Dictionary<string, int>
                {
                    { "all", customersList.Count }
                }
            };
        }, TimeSpan.FromMinutes(15), cancellationToken);
    }

    public async Task<AppointmentAnalyticsDto> GetAppointmentAnalyticsAsync(string userId, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"analytics:appointments:{userId}:{startDate?.ToString("yyyyMMdd")}:{endDate?.ToString("yyyyMMdd")}";

        return await _cacheService.GetOrSetAsync(cacheKey, async () =>
        {
            var appointments = await GetAppointmentsInRangeAsync(userId, startDate, endDate, cancellationToken);
            var appointmentsList = appointments.ToList();

            var totalAppointments = appointmentsList.Count;
            var completed = appointmentsList.Count(a => a.Status == AppointmentStatus.Completed);
            var cancelled = appointmentsList.Count(a => a.Status == AppointmentStatus.Cancelled);
            var noShow = appointmentsList.Count(a => a.Status == AppointmentStatus.NoShow);

            return new AppointmentAnalyticsDto
            {
                TotalAppointments = totalAppointments,
                CompletedAppointments = completed,
                CancelledAppointments = cancelled,
                NoShowAppointments = noShow,
                CompletionRate = totalAppointments > 0 ? (double)completed / totalAppointments * 100 : 0,
                NoShowRate = totalAppointments > 0 ? (double)noShow / totalAppointments * 100 : 0,
                DailyTrends = appointmentsList
                    .GroupBy(a => a.ScheduledAt.Date)
                    .Select(g => new AppointmentTrendDto
                    {
                        Date = g.Key,
                        AppointmentCount = g.Count(),
                        CompletedCount = g.Count(a => a.Status == AppointmentStatus.Completed)
                    })
                    .OrderBy(t => t.Date)
                    .ToList(),
                AppointmentsByStatus = appointmentsList
                    .GroupBy(a => a.Status)
                    .ToDictionary(g => g.Key.ToString(), g => g.Count()),
                AppointmentsByServiceType = appointmentsList
                    .GroupBy(a => a.ServiceType)
                    .ToDictionary(g => g.Key.ToString(), g => g.Count())
            };
        }, TimeSpan.FromMinutes(5), cancellationToken);
    }

    public async Task<UsageAnalyticsDto> GetUsageAnalyticsAsync(string userId, int months = 6, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"analytics:usage:{userId}:{months}";

        return await _cacheService.GetOrSetAsync(cacheKey, async () =>
        {
            var user = await _unitOfWork.Repository<User>()
                .GetByIdAsync(userId, cancellationToken);

            if (user == null)
            {
                return new UsageAnalyticsDto();
            }

            var startDate = DateTime.UtcNow.AddMonths(-months);
            var calls = await GetCallsInRangeAsync(userId, startDate, null, cancellationToken);

            // Get WhatsApp conversations for this user
            var whatsappConversations = await _unitOfWork.Repository<WhatsAppConversation>()
                .FindAsync(c => c.UserId == userId, cancellationToken);
            var conversationIds = whatsappConversations.Select(c => c.Id).ToList();

            // Get messages for these conversations
            var whatsappMessages = await _unitOfWork.Repository<WhatsAppMessage>()
                .FindAsync(m => conversationIds.Contains(m.ConversationId) && m.CreatedAt >= startDate, cancellationToken);

            // Get SMS conversations and messages
            var smsConversations = await _unitOfWork.Repository<SMSConversation>()
                .FindAsync(c => c.UserId == userId, cancellationToken);
            var smsConversationIds = smsConversations.Select(c => c.Id).ToList();
            var smsMessages = await _unitOfWork.Repository<SMSMessage>()
                .FindAsync(m => smsConversationIds.Contains(m.ConversationId) && m.CreatedAt >= startDate, cancellationToken);

            var totalVoiceMinutes = calls.Sum(c => c.DurationSeconds) / 60;

            return new UsageAnalyticsDto
            {
                TotalVoiceMinutes = totalVoiceMinutes,
                TotalWhatsAppMessages = whatsappMessages.Count(),
                TotalSmsMessages = smsMessages.Count(),
                AverageDailyUsage = totalVoiceMinutes / (months * 30),
                MonthlyTrends = Enumerable.Range(0, months)
                    .Select(i =>
                    {
                        var monthStart = DateTime.UtcNow.AddMonths(-i).Date;
                        var monthEnd = monthStart.AddMonths(1);

                        return new MonthlyUsageDto
                        {
                            Month = monthStart.ToString("yyyy-MM"),
                            VoiceMinutes = calls.Where(c => c.CreatedAt >= monthStart && c.CreatedAt < monthEnd).Sum(c => c.DurationSeconds) / 60,
                            WhatsAppMessages = whatsappMessages.Count(m => m.CreatedAt >= monthStart && m.CreatedAt < monthEnd),
                            SmsMessages = smsMessages.Count(m => m.CreatedAt >= monthStart && m.CreatedAt < monthEnd)
                        };
                    })
                    .OrderBy(m => m.Month)
                    .ToList(),
                UsageByFeature = new Dictionary<string, int>
                {
                    { "voice_calls", totalVoiceMinutes },
                    { "whatsapp", whatsappMessages.Count() },
                    { "sms", smsMessages.Count() }
                }
            };
        }, TimeSpan.FromMinutes(10), cancellationToken);
    }

    public async Task<PerformanceMetricsDto> GetPerformanceMetricsAsync(string userId, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"analytics:performance:{userId}";

        return await _cacheService.GetOrSetAsync(cacheKey, async () =>
        {
            var calls = await GetCallsInRangeAsync(userId, DateTime.UtcNow.AddDays(-30), null, cancellationToken);
            var callsList = calls.ToList();

            var successfulCalls = callsList.Count(c => c.DurationSeconds > 0);
            var totalCalls = callsList.Count;

            // Calculate average call duration as a proxy for response quality
            var avgDuration = callsList.Where(c => c.DurationSeconds > 0).Select(c => c.DurationSeconds).DefaultIfEmpty(0).Average();
            var failedCalls = callsList.Count(c => c.DurationSeconds == 0);

            return new PerformanceMetricsDto
            {
                AverageResponseTime = Math.Round(avgDuration / 60.0, 1), // Average call duration in minutes
                CallSuccessRate = totalCalls > 0 ? (double)successfulCalls / totalCalls * 100 : 0,
                ApiUptime = totalCalls > 0 ? (double)successfulCalls / totalCalls * 100 : 100,
                ErrorCount = failedCalls,
                CustomerSatisfactionScore = totalCalls > 0 ? Math.Round(Math.Min(5.0, (double)successfulCalls / totalCalls * 5), 1) : 0,
                MetricsByEndpoint = new Dictionary<string, double>
                {
                    { "/api/calls", totalCalls },
                    { "/api/appointments", callsList.Count(c => c.AppointmentScheduled) },
                    { "/api/customers", callsList.Select(c => c.CustomerId).Where(id => id != null).Distinct().Count() }
                }
            };
        }, TimeSpan.FromMinutes(5), cancellationToken);
    }

    public async Task<Dictionary<string, object>> GetBusinessInsightsAsync(string userId, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"analytics:insights:{userId}";

        return await _cacheService.GetOrSetAsync(cacheKey, async () =>
        {
            var calls = await GetCallsInRangeAsync(userId, DateTime.UtcNow.AddDays(-30), null, cancellationToken);
            var appointments = await GetAppointmentsInRangeAsync(userId, DateTime.UtcNow.AddDays(-30), null, cancellationToken);
            var customers = await _unitOfWork.Repository<Customer>()
                .FindAsync(c => c.UserId == userId, cancellationToken);

            var callsList = calls.ToList();
            var appointmentsList = appointments.ToList();
            var customersList = customers.ToList();

            var insights = new Dictionary<string, object>
            {
                { "total_calls_last_30_days", callsList.Count },
                { "total_appointments_last_30_days", appointmentsList.Count },
                { "active_customers", customersList.Count(c => c.JobCount > 0) },
                { "average_call_duration", callsList.Any() ? callsList.Average(c => c.DurationSeconds) : 0 },
                { "appointment_completion_rate", appointmentsList.Any() ? appointmentsList.Count(a => a.Status == AppointmentStatus.Completed) * 100.0 / appointmentsList.Count : 0 },
                { "busiest_hour", callsList.Any() ? callsList.GroupBy(c => c.CreatedAt.Hour).OrderByDescending(g => g.Count()).First().Key : 0 },
                { "most_common_service_type", appointmentsList.Any() ? appointmentsList.GroupBy(a => a.ServiceType).OrderByDescending(g => g.Count()).First().Key.ToString() : "N/A" }
            };

            return insights;
        }, TimeSpan.FromMinutes(15), cancellationToken);
    }

    private async Task<IEnumerable<Call>> GetCallsInRangeAsync(string userId, DateTime? startDate, DateTime? endDate, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var query = await _unitOfWork.Repository<Call>()
            .FindAsync(c => c.UserId == userId && c.CreatedAt <= now, cancellationToken);

        if (startDate.HasValue)
            query = query.Where(c => c.CreatedAt >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(c => c.CreatedAt <= endDate.Value);

        return query;
    }

    private async Task<IEnumerable<Appointment>> GetAppointmentsInRangeAsync(string userId, DateTime? startDate, DateTime? endDate, CancellationToken cancellationToken)
    {
        var query = await _unitOfWork.Repository<Appointment>()
            .FindAsync(a => a.UserId == userId, cancellationToken);

        if (startDate.HasValue)
            query = query.Where(a => a.ScheduledAt >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(a => a.ScheduledAt <= endDate.Value);

        return query;
    }

    private async Task<IEnumerable<Invoice>> GetInvoicesInRangeAsync(string userId, DateTime? startDate, DateTime? endDate, CancellationToken cancellationToken)
    {
        var query = await _unitOfWork.Repository<Invoice>()
            .FindAsync(i => i.UserId == userId, cancellationToken);

        if (startDate.HasValue)
            query = query.Where(i => i.CreatedAt >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(i => i.CreatedAt <= endDate.Value);

        return query;
    }

    /// <summary>
    /// Calculate churn rate as percentage of deactivated users out of total.
    /// Since admin controls all accounts, "churn" = admin deactivating users.
    /// </summary>
    private async Task<double> CalculateChurnRateAsync(string userId, CancellationToken cancellationToken)
    {
        try
        {
            var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);

            // All subscriptions that existed before the period
            var allSubscriptions = await _unitOfWork.Repository<UserSubscription>()
                .FindAsync(s => s.Plan != null && s.CreatedAt < thirtyDaysAgo, cancellationToken);
            var totalAtStart = allSubscriptions.Count();

            if (totalAtStart == 0)
                return 0;

            // Count users deactivated (HasActivePayment=false or suspended)
            var deactivatedCount = allSubscriptions
                .Count(s => !s.HasActivePayment || s.IsTemporarilySuspended);

            var churnRate = (double)deactivatedCount / totalAtStart * 100;

            _logger.LogInformation(
                "Churn rate calculated: {ChurnRate}% ({Deactivated}/{Total})",
                churnRate.ToString("F2"),
                deactivatedCount,
                totalAtStart);

            return Math.Round(churnRate, 2);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating churn rate for user {UserId}", userId);
            return 0;
        }
    }

    /// <summary>
    /// Calculate monthly churn rate — simplified for admin-controlled accounts.
    /// Counts users deactivated each month vs total active at start of month.
    /// </summary>
    public async Task<Dictionary<string, double>> CalculateMonthlyChurnRateAsync(int months = 12, CancellationToken cancellationToken = default)
    {
        var churnRates = new Dictionary<string, double>();

        try
        {
            var today = DateTime.UtcNow.Date;

            // Get all subscriptions once for efficiency
            var allSubscriptions = await _unitOfWork.Repository<UserSubscription>()
                .FindAsync(s => s.Plan != null, cancellationToken);
            var subsList = allSubscriptions.ToList();

            for (int i = 0; i < months; i++)
            {
                var monthStart = today.AddMonths(-i);
                var monthStartFirstDay = new DateTime(monthStart.Year, monthStart.Month, 1);

                // Users who existed at start of month
                var activeAtStart = subsList.Count(s => s.CreatedAt < monthStartFirstDay);

                if (activeAtStart == 0)
                {
                    churnRates[$"{monthStart:yyyy-MM}"] = 0;
                    continue;
                }

                // Users deactivated who were created before month start
                var deactivatedThisMonth = subsList.Count(s =>
                    s.CreatedAt < monthStartFirstDay &&
                    (!s.HasActivePayment || s.IsTemporarilySuspended));

                var monthlyChurnRate = (double)deactivatedThisMonth / activeAtStart * 100;
                churnRates[$"{monthStart:yyyy-MM}"] = Math.Round(monthlyChurnRate, 2);
            }

            return churnRates.OrderBy(kv => kv.Key).ToDictionary(kv => kv.Key, kv => kv.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating monthly churn rates");
            return churnRates;
        }
    }

    /// <summary>
    /// Get churn analytics — simplified for admin-controlled accounts.
    /// "Churn" = admin deactivating/suspending users. No self-service subscriptions.
    /// </summary>
    public async Task<ChurnAnalyticsDto> GetChurnAnalyticsAsync(int months = 12, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"analytics:churn:{months}";

        return await _cacheService.GetOrSetAsync(cacheKey, async () =>
        {
            var now = DateTime.UtcNow;
            var previousMonthStart = new DateTime(now.Year, now.Month, 1).AddMonths(-1);

            // Get monthly churn rates
            var monthlyChurnRates = await CalculateMonthlyChurnRateAsync(months, cancellationToken);

            var currentMonthKey = $"{now:yyyy-MM}";
            var previousMonthKey = $"{previousMonthStart:yyyy-MM}";

            var currentChurn = monthlyChurnRates.GetValueOrDefault(currentMonthKey, 0);
            var previousChurn = monthlyChurnRates.GetValueOrDefault(previousMonthKey, 0);

            // Count active vs inactive subscriptions
            var allSubscriptions = await _unitOfWork.Repository<UserSubscription>()
                .FindAsync(s => s.Plan != null, cancellationToken);
            var subsList = allSubscriptions.ToList();
            var activeSubscriptions = subsList.Count(s => s.HasActivePayment && !s.IsTemporarilySuspended);
            var suspendedCount = subsList.Count(s => s.IsTemporarilySuspended);
            var deactivatedCount = subsList.Count(s => !s.HasActivePayment);

            return new ChurnAnalyticsDto
            {
                CurrentMonthChurnRate = currentChurn,
                PreviousMonthChurnRate = previousChurn,
                ChurnRateChange = Math.Round(currentChurn - previousChurn, 2),
                TotalActiveSubscriptions = activeSubscriptions,
                ChurnedThisMonth = deactivatedCount + suspendedCount,
                MonthlyChurnRates = monthlyChurnRates,
                Breakdown = new ChurnBreakdownDto
                {
                    VoluntaryCancellations = 0, // No self-service cancellations
                    SubscriptionExpirations = deactivatedCount, // Admin deactivated
                    PaymentFailures = suspendedCount // Admin suspended
                },
                CalculatedAt = DateTime.UtcNow
            };
        }, TimeSpan.FromMinutes(15), cancellationToken);
    }

}
