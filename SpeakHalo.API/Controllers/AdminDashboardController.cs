using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SpeakHalo.Domain.Entities;
using SpeakHalo.Domain.Interfaces.Repositories;

namespace SpeakHalo.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/admin/dashboard")]
[Authorize(Roles = "Admin")]
public class AdminDashboardController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AdminDashboardController> _logger;

    public AdminDashboardController(
        IUnitOfWork unitOfWork,
        ILogger<AdminDashboardController> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    [HttpGet("overview")]
    public async Task<IActionResult> GetDashboardOverview(CancellationToken cancellationToken)
    {
        try
        {
            var now = DateTime.UtcNow;
            var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            var invoiceRepo = _unitOfWork.Repository<Invoice>();
            var totalRevenue = await invoiceRepo.SumAsync(i => (decimal?)i.Total, cancellationToken) ?? 0;
            var paidRevenue = await invoiceRepo.SumAsync(i => i.Status == "paid" ? (decimal?)i.Total : null, cancellationToken) ?? 0;
            var invoiceCount = await invoiceRepo.CountAsync(null, cancellationToken);

            var monthRevenue = await invoiceRepo.SumAsync(i => i.CreatedAt >= monthStart ? (decimal?)i.Total : null, cancellationToken) ?? 0;
            var monthPaid = await invoiceRepo.SumAsync(i => i.CreatedAt >= monthStart && i.Status == "paid" ? (decimal?)i.Total : null, cancellationToken) ?? 0;

            var totalCustomers = await _unitOfWork.Users.CountAsync(null, cancellationToken);
            var activeCustomers = await _unitOfWork.Users.CountAsync(u => u.IsActive, cancellationToken);
            var trialCustomers = await _unitOfWork.Repository<Domain.Entities.UserTrial>()
                .CountAsync(t => t.IsTrial, cancellationToken);

            var lastMonth = now.AddDays(-30);
            var cancelledLastMonth = await _unitOfWork.Repository<Domain.Entities.UserSubscription>()
                .CountAsync(s => s.Plan == null && s.UpdatedAt >= lastMonth && !s.HasActivePayment, cancellationToken);
            var churnRate = activeCustomers > 0
                ? Math.Round((double)cancelledLastMonth / activeCustomers * 100, 1)
                : 0.0;

            var totalCalls = await _unitOfWork.Calls.CountAsync(null, cancellationToken);
            var totalSeconds = await _unitOfWork.Calls.SumAsync(c => (long?)c.DurationSeconds, cancellationToken) ?? 0;
            var totalMinutes = totalSeconds / 60;

            var monthCallCount = await _unitOfWork.Calls.CountAsync(c => c.CreatedAt >= monthStart, cancellationToken);
            var monthSeconds = await _unitOfWork.Calls.SumAsync(c => c.CreatedAt >= monthStart ? (long?)c.DurationSeconds : null, cancellationToken) ?? 0;
            var monthMinutes = monthSeconds / 60;

            var monthAppointments = await _unitOfWork.Appointments
                .CountAsync(a => a.CreatedAt >= monthStart, cancellationToken);

            var overview = new
            {
                revenue = new
                {
                    all_time = new
                    {
                        total = Math.Round(totalRevenue, 2),
                        paid = Math.Round(paidRevenue, 2),
                        outstanding = Math.Round(totalRevenue - paidRevenue, 2),
                        invoice_count = invoiceCount
                    },
                    current_month = new
                    {
                        total = Math.Round(monthRevenue, 2),
                        paid = Math.Round(monthPaid, 2),
                        outstanding = Math.Round(monthRevenue - monthPaid, 2)
                    }
                },
                customers = new
                {
                    total = totalCustomers,
                    active = activeCustomers,
                    trial = trialCustomers,
                    churn_rate = churnRate
                },
                usage = new
                {
                    all_time = new
                    {
                        calls = totalCalls,
                        minutes = totalMinutes
                    },
                    current_month = new
                    {
                        calls = monthCallCount,
                        minutes = monthMinutes,
                        appointments = monthAppointments
                    }
                }
            };

            return Ok(overview);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching admin dashboard overview");
            return StatusCode(500, new { error = "Failed to load dashboard overview" });
        }
    }

    [HttpGet("revenue/trends")]
    public async Task<IActionResult> GetRevenueTrends(
        [FromQuery] string period = "month",
        [FromQuery] int months = 12,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var cutoffDate = DateTime.UtcNow.AddMonths(-months);

            var trends = await _unitOfWork.Repository<Invoice>()
                .FindAsync(i => i.CreatedAt >= cutoffDate, cancellationToken);

            var groupedTrends = trends
                .GroupBy(i => new { i.CreatedAt.Year, i.CreatedAt.Month })
                .Select(g => new
                {
                    year = g.Key.Year,
                    month = g.Key.Month,
                    revenue = Math.Round(g.Sum(i => i.Total), 2),
                    paid = Math.Round(g.Where(i => i.Status == "paid").Sum(i => i.Total), 2),
                    invoice_count = g.Count()
                })
                .OrderBy(t => t.year).ThenBy(t => t.month)
                .ToList();

            return Ok(new { period, data = groupedTrends });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching revenue trends");
            return StatusCode(500, new { error = "Failed to load revenue trends" });
        }
    }

    [HttpGet("customers/analytics")]
    public async Task<IActionResult> GetCustomerAnalytics(CancellationToken cancellationToken)
    {
        try
        {
            var now = DateTime.UtcNow;
            var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            var totalCustomers = await _unitOfWork.Users.CountAsync(null, cancellationToken);
            var activeCustomers = await _unitOfWork.Users.CountAsync(u => u.IsActive, cancellationToken);
            var inactiveCustomers = await _unitOfWork.Users.CountAsync(u => !u.IsActive, cancellationToken);
            var trialCustomers = await _unitOfWork.Repository<Domain.Entities.UserTrial>()
                .CountAsync(t => t.IsTrial, cancellationToken);
            var payingCustomers = activeCustomers - trialCustomers;
            var newThisMonth = await _unitOfWork.Users.CountAsync(u => u.CreatedAt >= monthStart, cancellationToken);

            var allSubscriptions = await _unitOfWork.Repository<Domain.Entities.UserSubscription>()
                .GetAllAsync(cancellationToken);
            var subscriptionDistribution = allSubscriptions
                .GroupBy(s => s.Plan ?? "free")
                .Select(g => new { plan = g.Key, count = g.Count() })
                .OrderByDescending(x => x.count)
                .ToList();

            var analytics = new
            {
                total_customers = totalCustomers,
                active_customers = activeCustomers,
                inactive_customers = inactiveCustomers,
                trial_customers = trialCustomers,
                paying_customers = payingCustomers,
                new_this_month = newThisMonth,
                subscription_distribution = subscriptionDistribution
            };

            return Ok(analytics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching customer analytics");
            return StatusCode(500, new { error = "Failed to load customer analytics" });
        }
    }

    [HttpGet("usage/analytics")]
    public async Task<IActionResult> GetUsageAnalytics(
        [FromQuery] int days = 30,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-days);

            var totalCalls = await _unitOfWork.Calls.CountAsync(c => c.CreatedAt >= cutoffDate, cancellationToken);
            var totalDurationSeconds = await _unitOfWork.Calls.SumAsync(
                c => c.CreatedAt >= cutoffDate ? (long?)c.DurationSeconds : null,
                cancellationToken) ?? 0;
            var averageDurationSeconds = totalCalls > 0
                ? await _unitOfWork.Calls.AverageAsync(c => c.CreatedAt >= cutoffDate ? (int?)c.DurationSeconds : null, cancellationToken) ?? 0
                : 0;

            var appointments = await _unitOfWork.Appointments.FindAsync(a => a.CreatedAt >= cutoffDate, cancellationToken);

            var usageCostsRepo = _unitOfWork.Repository<UsageCosts>();
            var totalAi = await usageCostsRepo.SumAsync(
                u => u.CreatedAt >= cutoffDate ? (decimal?)u.AiCosts : null,
                cancellationToken) ?? 0;
            var totalTwilio = await usageCostsRepo.SumAsync(
                u => u.CreatedAt >= cutoffDate ? (decimal?)u.TwilioCosts : null,
                cancellationToken) ?? 0;
            var totalSendGrid = await usageCostsRepo.SumAsync(
                u => u.CreatedAt >= cutoffDate ? (decimal?)u.SendGridCosts : null,
                cancellationToken) ?? 0;
            var totalStorage = await usageCostsRepo.SumAsync(
                u => u.CreatedAt >= cutoffDate ? (decimal?)u.StorageCosts : null,
                cancellationToken) ?? 0;
            var grandTotal = await usageCostsRepo.SumAsync(
                u => u.CreatedAt >= cutoffDate ? (decimal?)u.TotalCosts : null,
                cancellationToken) ?? 0;

            var analytics = new
            {
                period_days = days,
                calls = new
                {
                    total = totalCalls,
                    total_duration_minutes = totalDurationSeconds / 60,
                    average_duration_seconds = averageDurationSeconds
                },
                appointments = new
                {
                    total = appointments.Count(),
                    by_status = appointments
                        .GroupBy(a => a.Status.ToString())
                        .Select(g => new { status = g.Key, count = g.Count() })
                        .ToList()
                },
                costs = new
                {
                    total_ai = Math.Round(totalAi, 2),
                    total_twilio = Math.Round(totalTwilio, 2),
                    total_sendgrid = Math.Round(totalSendGrid, 2),
                    total_storage = Math.Round(totalStorage, 2),
                    grand_total = Math.Round(grandTotal, 2)
                }
            };

            return Ok(analytics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching usage analytics");
            return StatusCode(500, new { error = "Failed to load usage analytics" });
        }
    }

    [HttpGet("invoices/summary")]
    public async Task<IActionResult> GetInvoicesSummary(CancellationToken cancellationToken)
    {
        try
        {
            var now = DateTime.UtcNow;
            var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            var invoiceRepo = _unitOfWork.Repository<Invoice>();

            var totalInvoices = await invoiceRepo.CountAsync(null, cancellationToken);
            var paidInvoices = await invoiceRepo.CountAsync(i => i.Status == "paid", cancellationToken);
            var pendingInvoices = await invoiceRepo.CountAsync(i => i.Status == "pending", cancellationToken);
            var overdueInvoices = await invoiceRepo.CountAsync(i => i.Status == "overdue", cancellationToken);

            var totalRevenue = await invoiceRepo.SumAsync(i => (decimal?)i.Total, cancellationToken) ?? 0;
            var paidRevenue = await invoiceRepo.SumAsync(i => i.Status == "paid" ? (decimal?)i.Total : null, cancellationToken) ?? 0;
            var outstandingRevenue = await invoiceRepo.SumAsync(i => i.Status != "paid" ? (decimal?)i.Total : null, cancellationToken) ?? 0;

            var monthInvoices = await invoiceRepo.CountAsync(i => i.CreatedAt >= monthStart, cancellationToken);
            var monthRevenue = await invoiceRepo.SumAsync(i => i.CreatedAt >= monthStart ? (decimal?)i.Total : null, cancellationToken) ?? 0;

            var averageInvoiceValue = totalInvoices > 0
                ? await invoiceRepo.AverageAsync(i => (decimal?)i.Total, cancellationToken) ?? 0
                : 0;

            var summary = new
            {
                total_invoices = totalInvoices,
                paid_invoices = paidInvoices,
                pending_invoices = pendingInvoices,
                overdue_invoices = overdueInvoices,
                total_revenue = Math.Round(totalRevenue, 2),
                paid_revenue = Math.Round(paidRevenue, 2),
                outstanding_revenue = Math.Round(outstandingRevenue, 2),
                current_month = new
                {
                    invoices = monthInvoices,
                    revenue = Math.Round(monthRevenue, 2)
                },
                average_invoice_value = Math.Round((double)averageInvoiceValue, 2)
            };

            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching invoices summary");
            return StatusCode(500, new { error = "Failed to load invoices summary" });
        }
    }

    [HttpGet("quick-stats")]
    public async Task<IActionResult> GetQuickStats(CancellationToken cancellationToken)
    {
        try
        {
            var now = DateTime.UtcNow;
            var todayStart = now.Date;
            var yesterdayStart = todayStart.AddDays(-1);

            var todayCalls = await _unitOfWork.Calls
                .CountAsync(c => c.CreatedAt >= todayStart, cancellationToken);

            var todayAppointments = await _unitOfWork.Appointments
                .CountAsync(a => a.CreatedAt >= todayStart, cancellationToken);

            var todayRevenue = (await _unitOfWork.Repository<Invoice>()
                .FindAsync(i => i.CreatedAt >= todayStart && i.Status == "paid", cancellationToken))
                .Sum(i => i.Total);

            var yesterdayCalls = await _unitOfWork.Calls
                .CountAsync(c => c.CreatedAt >= yesterdayStart && c.CreatedAt < todayStart, cancellationToken);

            var yesterdayAppointments = await _unitOfWork.Appointments
                .CountAsync(a => a.CreatedAt >= yesterdayStart && a.CreatedAt < todayStart, cancellationToken);

            var stats = new
            {
                today = new
                {
                    calls = todayCalls,
                    appointments = todayAppointments,
                    revenue = Math.Round(todayRevenue, 2)
                },
                yesterday = new
                {
                    calls = yesterdayCalls,
                    appointments = yesterdayAppointments
                },
                changes = new
                {
                    calls = todayCalls - yesterdayCalls,
                    appointments = todayAppointments - yesterdayAppointments
                }
            };

            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching quick stats");
            return StatusCode(500, new { error = "Failed to load quick stats" });
        }
    }

    [HttpGet("alerts")]
    public async Task<IActionResult> GetSystemAlerts(CancellationToken cancellationToken)
    {
        try
        {
            var alerts = new List<object>();

            var expiringTrials = await _unitOfWork.Repository<Domain.Entities.UserTrial>().FindAsync(
                t => t.IsTrial && t.TrialEndsAt.HasValue &&
                     t.TrialEndsAt.Value <= DateTime.UtcNow.AddDays(7) &&
                     t.TrialEndsAt.Value > DateTime.UtcNow,
                cancellationToken);

            if (expiringTrials.Any())
            {
                alerts.Add(new
                {
                    type = "warning",
                    title = "Expiring Trials",
                    message = $"{expiringTrials.Count()} trial accounts will expire within 7 days",
                    count = expiringTrials.Count()
                });
            }

            var overdueInvoices = await _unitOfWork.Repository<Invoice>()
                .FindAsync(i => i.Status == "overdue", cancellationToken);

            if (overdueInvoices.Any())
            {
                alerts.Add(new
                {
                    type = "error",
                    title = "Overdue Invoices",
                    message = $"{overdueInvoices.Count()} invoices are overdue",
                    count = overdueInvoices.Count(),
                    total_amount = Math.Round(overdueInvoices.Sum(i => i.Total), 2)
                });
            }

            // UpdatedAt is used as a proxy for activity since there's no dedicated last-activity field
            var lowActivityUsers = await _unitOfWork.Users.FindAsync(
                u => u.UpdatedAt.HasValue &&
                     u.UpdatedAt.Value < DateTime.UtcNow.AddDays(-30) &&
                     u.IsActive,
                cancellationToken);

            if (lowActivityUsers.Any())
            {
                alerts.Add(new
                {
                    type = "info",
                    title = "Low Activity Users",
                    message = $"{lowActivityUsers.Count()} active users have low recent activity (30+ days)",
                    count = lowActivityUsers.Count()
                });
            }

            return Ok(new { alerts, count = alerts.Count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching system alerts");
            return StatusCode(500, new { error = "Failed to load system alerts" });
        }
    }
}
