using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SpeakHalo.Application.Interfaces;
using SpeakHalo.Infrastructure.Data;

namespace SpeakHalo.API.Controllers;

[ApiController]
[Route("api/v1/admin/analytics")]
[Authorize(Roles = "Admin")]
public class AdminAnalyticsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IAnalyticsService _analyticsService;
    private readonly ILogger<AdminAnalyticsController> _logger;

    public AdminAnalyticsController(
        ApplicationDbContext context,
        IAnalyticsService analyticsService,
        ILogger<AdminAnalyticsController> logger)
    {
        _context = context;
        _analyticsService = analyticsService;
        _logger = logger;
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboardOverview([FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var now = DateTime.UtcNow;
            var start = startDate ?? now.AddDays(-30);
            var end = endDate ?? now;

            var totalUsers = await _context.Users.CountAsync(u => u.IsActive && !u.IsDeleted);

            var subscriptionsByPlan = await _context.UserSubscriptions
                .Where(s => !string.IsNullOrEmpty(s.Plan))
                .GroupBy(s => s.Plan)
                .Select(g => new
                {
                    plan = g.Key,
                    count = g.Count()
                })
                .ToListAsync();

            var activeTrials = await _context.UserTrials
                .CountAsync(t => t.IsTrial && t.TrialEndsAt > now);

            var expiredTrials = await _context.UserTrials
                .CountAsync(t => t.IsTrial && t.TrialEndsAt <= now);

            var suspendedCount = await _context.UserSubscriptions
                .CountAsync(s => s.IsTemporarilySuspended);

            var usageStats = await _context.UserSubscriptions
                .Select(s => new
                {
                    s.AIMinutesUsedThisMonth,
                    s.WhatsAppMessagesUsedThisMonth,
                    s.SMSMessagesUsedThisMonth,
                    s.ImagesUsedThisMonth
                })
                .ToListAsync();

            var totalAIMinutes = usageStats.Sum(s => s.AIMinutesUsedThisMonth);
            var totalWhatsApp = usageStats.Sum(s => s.WhatsAppMessagesUsedThisMonth);
            var totalSMS = usageStats.Sum(s => s.SMSMessagesUsedThisMonth);
            var totalImages = usageStats.Sum(s => s.ImagesUsedThisMonth);

            return Ok(new
            {
                overview = new
                {
                    totalUsers,
                    activeTrials,
                    expiredTrials,
                    suspendedAccounts = suspendedCount,
                    period = new { start, end }
                },
                subscriptions = new
                {
                    byPlan = subscriptionsByPlan,
                    total = subscriptionsByPlan.Sum(p => p.count)
                },
                usage = new
                {
                    currentMonth = new
                    {
                        aiMinutes = totalAIMinutes,
                        whatsAppMessages = totalWhatsApp,
                        smsMessages = totalSMS,
                        images = totalImages
                    }
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving dashboard overview");
            return StatusCode(500, new { message = "Error retrieving dashboard overview", error = "An unexpected error occurred. Please try again." });
        }
    }

    /// <summary>
    /// Get detailed usage statistics
    /// </summary>
    [HttpGet("usage")]
    public async Task<IActionResult> GetUsageStatistics([FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var now = DateTime.UtcNow;
            var start = startDate ?? now.AddDays(-30);
            var end = endDate ?? now;

            var subscriptions = await _context.UserSubscriptions
                .Include(s => s.User)
                .Where(s => s.User.IsActive)
                .Select(s => new
                {
                    s.UserId,
                    s.User.Email,
                    s.Plan,
                    s.AIMinutesIncluded,
                    s.AIMinutesUsedThisMonth,
                    s.WhatsAppMessagesIncluded,
                    s.WhatsAppMessagesUsedThisMonth,
                    s.SMSMessagesIncluded,
                    s.SMSMessagesUsedThisMonth,
                    s.ImagesIncluded,
                    s.ImagesUsedThisMonth
                })
                .ToListAsync();

            var usageBreakdown = subscriptions.Select(s => new
            {
                s.UserId,
                s.Email,
                s.Plan,
                aiUsage = new
                {
                    included = s.AIMinutesIncluded,
                    used = s.AIMinutesUsedThisMonth,
                    percentage = s.AIMinutesIncluded > 0 ? (s.AIMinutesUsedThisMonth * 100.0 / s.AIMinutesIncluded) : 0,
                    overage = Math.Max(0, s.AIMinutesUsedThisMonth - s.AIMinutesIncluded)
                },
                whatsAppUsage = new
                {
                    included = s.WhatsAppMessagesIncluded,
                    used = s.WhatsAppMessagesUsedThisMonth,
                    percentage = s.WhatsAppMessagesIncluded > 0 ? (s.WhatsAppMessagesUsedThisMonth * 100.0 / s.WhatsAppMessagesIncluded) : 0,
                    overage = Math.Max(0, s.WhatsAppMessagesUsedThisMonth - s.WhatsAppMessagesIncluded)
                },
                smsUsage = new
                {
                    included = s.SMSMessagesIncluded,
                    used = s.SMSMessagesUsedThisMonth,
                    percentage = s.SMSMessagesIncluded > 0 ? (s.SMSMessagesUsedThisMonth * 100.0 / s.SMSMessagesIncluded) : 0,
                    overage = Math.Max(0, s.SMSMessagesUsedThisMonth - s.SMSMessagesIncluded)
                },
                imageUsage = new
                {
                    included = s.ImagesIncluded,
                    used = s.ImagesUsedThisMonth,
                    percentage = s.ImagesIncluded > 0 ? (s.ImagesUsedThisMonth * 100.0 / s.ImagesIncluded) : 0,
                    overage = Math.Max(0, s.ImagesUsedThisMonth - s.ImagesIncluded)
                }
            }).ToList();

            var topAIUsers = usageBreakdown.OrderByDescending(u => u.aiUsage.used).Take(10);
            var topWhatsAppUsers = usageBreakdown.OrderByDescending(u => u.whatsAppUsage.used).Take(10);

            var totalUsage = new
            {
                aiMinutes = usageBreakdown.Sum(u => u.aiUsage.used),
                whatsAppMessages = usageBreakdown.Sum(u => u.whatsAppUsage.used),
                smsMessages = usageBreakdown.Sum(u => u.smsUsage.used),
                images = usageBreakdown.Sum(u => u.imageUsage.used)
            };

            return Ok(new
            {
                period = new { start, end },
                totalUsage,
                topUsers = new
                {
                    byAIMinutes = topAIUsers,
                    byWhatsApp = topWhatsAppUsers
                },
                allUsers = usageBreakdown
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving usage statistics");
            return StatusCode(500, new { message = "Error retrieving usage statistics", error = "An unexpected error occurred. Please try again." });
        }
    }

    /// <summary>
    /// Get revenue analytics
    /// </summary>
    [HttpGet("revenue")]
    public async Task<IActionResult> GetRevenueAnalytics([FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var now = DateTime.UtcNow;
            var start = startDate ?? now.AddDays(-30);
            var end = endDate ?? now;

            var usersByPlan = await _context.UserSubscriptions
                .Where(s => !string.IsNullOrEmpty(s.Plan) && s.Plan != "trial")
                .GroupBy(s => s.Plan)
                .Select(g => new
                {
                    plan = g.Key,
                    count = g.Count()
                })
                .ToListAsync();

            return Ok(new
            {
                period = new { start, end },
                breakdown = new
                {
                    byPlan = usersByPlan
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving revenue analytics");
            return StatusCode(500, new { message = "Error retrieving revenue analytics", error = "An unexpected error occurred. Please try again." });
        }
    }

    /// <summary>
    /// Get growth metrics
    /// </summary>
    [HttpGet("growth")]
    public async Task<IActionResult> GetGrowthMetrics([FromQuery] int days = 30)
    {
        try
        {
            var now = DateTime.UtcNow;
            var startDate = now.AddDays(-days);

            var newUsersByDay = await _context.Users
                .Where(u => u.CreatedAt >= startDate)
                .GroupBy(u => u.CreatedAt.Date)
                .Select(g => new
                {
                    date = g.Key,
                    count = g.Count()
                })
                .OrderBy(d => d.date)
                .ToListAsync();

            var trialsByDay = await _context.UserTrials
                .Where(t => t.TrialStartedAt.HasValue && t.TrialStartedAt.Value >= startDate)
                .GroupBy(t => t.TrialStartedAt!.Value.Date)
                .Select(g => new
                {
                    date = g.Key,
                    count = g.Count()
                })
                .OrderBy(d => d.date)
                .ToListAsync();

            var trialConversions = await _context.UserTrials
                .Where(t => t.IsTrial == false && t.TrialStartedAt.HasValue && t.TrialStartedAt.Value >= startDate)
                .CountAsync();

            var totalTrials = await _context.UserTrials
                .Where(t => t.TrialStartedAt.HasValue && t.TrialStartedAt.Value >= startDate)
                .CountAsync();

            var conversionRate = totalTrials > 0 ? (trialConversions * 100.0 / totalTrials) : 0;

            var churnedUsers = await _context.Users
                .Where(u => !u.IsActive || u.IsDeleted)
                .Where(u => u.UpdatedAt.HasValue && u.UpdatedAt.Value >= startDate)
                .CountAsync();

            var totalUsersAtStart = await _context.Users
                .Where(u => u.CreatedAt < startDate)
                .CountAsync();

            var churnRate = totalUsersAtStart > 0 ? (churnedUsers * 100.0 / totalUsersAtStart) : 0;

            return Ok(new
            {
                period = new { days, startDate, endDate = now },
                newUsers = new
                {
                    total = newUsersByDay.Sum(d => d.count),
                    byDay = newUsersByDay
                },
                trials = new
                {
                    total = totalTrials,
                    conversions = trialConversions,
                    conversionRate,
                    byDay = trialsByDay
                },
                churn = new
                {
                    count = churnedUsers,
                    rate = churnRate
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving growth metrics");
            return StatusCode(500, new { message = "Error retrieving growth metrics", error = "An unexpected error occurred. Please try again." });
        }
    }

    /// <summary>
    /// Get geographic distribution of users by phone number country code
    /// </summary>
    [HttpGet("geographic")]
    public async Task<IActionResult> GetGeographicDistribution(CancellationToken cancellationToken = default)
    {
        try
        {
            var countryNames = new Dictionary<string, string>
            {
                ["US"] = "United States", ["GB"] = "United Kingdom", ["CA"] = "Canada",
                ["AU"] = "Australia", ["DE"] = "Germany", ["FR"] = "France",
                ["ES"] = "Spain", ["IT"] = "Italy", ["NL"] = "Netherlands",
                ["BR"] = "Brazil", ["MX"] = "Mexico", ["IN"] = "India",
                ["JP"] = "Japan", ["KR"] = "South Korea", ["SG"] = "Singapore",
                ["HK"] = "Hong Kong", ["IL"] = "Israel", ["SE"] = "Sweden",
                ["NO"] = "Norway", ["DK"] = "Denmark", ["FI"] = "Finland",
                ["IE"] = "Ireland", ["NZ"] = "New Zealand", ["ZA"] = "South Africa",
                ["AE"] = "United Arab Emirates", ["CH"] = "Switzerland",
                ["AT"] = "Austria", ["BE"] = "Belgium", ["PT"] = "Portugal",
                ["PL"] = "Poland",
            };

            var usersByCountry = await _context.PhoneNumbers
                .Where(p => p.IsActive)
                .GroupBy(p => p.CountryCode)
                .Select(g => new
                {
                    CountryCode = g.Key,
                    UserCount = g.Select(p => p.UserId).Distinct().Count()
                })
                .ToListAsync(cancellationToken);

            var callsByCountry = await _context.Calls
                .Where(c => c.PhoneNumberId != null)
                .Join(
                    _context.PhoneNumbers,
                    c => c.PhoneNumberId,
                    p => p.Id,
                    (c, p) => p.CountryCode
                )
                .GroupBy(cc => cc)
                .Select(g => new
                {
                    CountryCode = g.Key,
                    CallCount = g.Count()
                })
                .ToListAsync(cancellationToken);

            var callsLookup = callsByCountry.ToDictionary(c => c.CountryCode, c => c.CallCount);

            var result = usersByCountry
                .Select(u => new
                {
                    countryCode = u.CountryCode,
                    countryName = countryNames.GetValueOrDefault(u.CountryCode, u.CountryCode),
                    userCount = u.UserCount,
                    callCount = callsLookup.GetValueOrDefault(u.CountryCode, 0)
                })
                .OrderByDescending(x => x.userCount)
                .ToList();

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving geographic distribution");
            return StatusCode(500, new { message = "Error retrieving geographic distribution", error = "An unexpected error occurred. Please try again." });
        }
    }

    /// <summary>
    /// Get comprehensive churn analytics with monthly trends and breakdown
    /// </summary>
    [HttpGet("churn")]
    public async Task<IActionResult> GetChurnAnalytics([FromQuery] int months = 12, CancellationToken cancellationToken = default)
    {
        try
        {
            var churnAnalytics = await _analyticsService.GetChurnAnalyticsAsync(months, cancellationToken);
            return Ok(churnAnalytics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving churn analytics");
            return StatusCode(500, new { message = "Error retrieving churn analytics", error = "An unexpected error occurred. Please try again." });
        }
    }

}
