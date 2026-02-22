using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpeakHalo.Application.Interfaces;

namespace SpeakHalo.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class AnalyticsController : ControllerBase
{
    private readonly IAnalyticsService _analyticsService;

    public AnalyticsController(IAnalyticsService analyticsService)
    {
        _analyticsService = analyticsService;
    }

    [HttpGet("calls")]
    public async Task<IActionResult> GetCallAnalytics(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        startDate = EnsureUtc(startDate) ?? DateTime.UtcNow.AddDays(-30);
        endDate = EnsureUtc(endDate) ?? DateTime.UtcNow;

        var result = await _analyticsService.GetCallAnalyticsAsync(userId, startDate, endDate, cancellationToken);
        return Ok(new { isSuccess = true, data = result });
    }

    [HttpGet("revenue")]
    public async Task<IActionResult> GetRevenueAnalytics(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        startDate = EnsureUtc(startDate) ?? DateTime.UtcNow.AddDays(-30);
        endDate = EnsureUtc(endDate) ?? DateTime.UtcNow;

        var result = await _analyticsService.GetRevenueAnalyticsAsync(userId, startDate, endDate, cancellationToken);
        return Ok(new { isSuccess = true, data = result });
    }

    [HttpGet("customers")]
    public async Task<IActionResult> GetCustomerAnalytics(CancellationToken cancellationToken)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var result = await _analyticsService.GetCustomerAnalyticsAsync(userId, cancellationToken);
        return Ok(new { isSuccess = true, data = result });
    }

    [HttpGet("appointments")]
    public async Task<IActionResult> GetAppointmentAnalytics(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        startDate = EnsureUtc(startDate) ?? DateTime.UtcNow.AddDays(-30);
        endDate = EnsureUtc(endDate) ?? DateTime.UtcNow;

        var result = await _analyticsService.GetAppointmentAnalyticsAsync(userId, startDate, endDate, cancellationToken);
        return Ok(new { isSuccess = true, data = result });
    }

    private static DateTime? EnsureUtc(DateTime? dt)
    {
        if (!dt.HasValue) return null;
        return dt.Value.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(dt.Value, DateTimeKind.Utc)
            : dt.Value;
    }

    [HttpGet("usage")]
    public async Task<IActionResult> GetUsageAnalytics(
        [FromQuery] int months = 6,
        CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var result = await _analyticsService.GetUsageAnalyticsAsync(userId, months, cancellationToken);
        return Ok(new { isSuccess = true, data = result });
    }

    [HttpGet("performance")]
    public async Task<IActionResult> GetPerformanceMetrics(CancellationToken cancellationToken)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var result = await _analyticsService.GetPerformanceMetricsAsync(userId, cancellationToken);
        return Ok(new { isSuccess = true, data = result });
    }
}
