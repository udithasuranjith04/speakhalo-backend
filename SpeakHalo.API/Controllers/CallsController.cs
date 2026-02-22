using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpeakHalo.API.Filters;
using SpeakHalo.Application.DTOs;
using SpeakHalo.Application.Interfaces;
using SpeakHalo.Domain.Entities;
using SpeakHalo.Domain.Enums;
using SpeakHalo.Domain.Interfaces.Repositories;

namespace SpeakHalo.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
[RequireFeature(PlanFeature.VoiceAI)]
[RequireApiScope("calls")]
public class CallsController : ControllerBase
{
    private readonly ICallService _callService;
    private readonly IUsageTrackingService _usageTracking;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAnalyticsService _analyticsService;

    public CallsController(ICallService callService, IUsageTrackingService usageTracking, IUnitOfWork unitOfWork, IAnalyticsService analyticsService)
    {
        _callService = callService;
        _usageTracking = usageTracking;
        _unitOfWork = unitOfWork;
        _analyticsService = analyticsService;
    }

    private async Task<bool> UserOwnsCallAsync(string callId, string userId)
    {
        var calls = await _unitOfWork.Repository<Call>().FindAsync(c => c.Id == callId && c.UserId == userId);
        return calls.Any();
    }

    [HttpPost]
    public async Task<IActionResult> CreateCall([FromBody] CreateCallRequest request, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        // Override the UserId from the request body with the authenticated user's ID
        var securedRequest = request with { UserId = userId };

        var result = await _callService.CreateCallAsync(securedRequest, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(result);

        return CreatedAtAction(nameof(GetCallById), new { id = result.Data!.Id }, result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetCallById(string id, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        if (!await UserOwnsCallAsync(id, userId))
            return NotFound(new { error = "Call not found" });

        var result = await _callService.GetCallByIdAsync(id, cancellationToken);

        if (!result.IsSuccess)
            return NotFound(result);

        return Ok(result);
    }

    [HttpGet("user/calls")]
    public async Task<IActionResult> GetUserCalls([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null, CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _callService.GetUserCallsAsync(userId, pageNumber, pageSize, search, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateCall(string id, [FromBody] UpdateCallRequest request, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        if (!await UserOwnsCallAsync(id, userId))
            return NotFound(new { error = "Call not found" });

        var result = await _callService.UpdateCallAsync(id, request, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPost("{id:guid}/end")]
    public async Task<IActionResult> EndCall(string id, [FromBody] int durationSeconds, CancellationToken cancellationToken)
    {
        // Validate duration bounds (0-480 minutes = 8 hours max)
        if (durationSeconds < 0 || durationSeconds > 28800)
            return BadRequest(new { error = "Duration must be between 0 and 28800 seconds" });

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        if (!await UserOwnsCallAsync(id, userId))
            return NotFound(new { error = "Call not found" });

        var result = await _callService.EndCallAsync(id, durationSeconds, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(result);

        var minutesUsed = Math.Max(1, (int)Math.Ceiling(durationSeconds / 60.0));
        await _usageTracking.TrackAIMinutesAsync(userId, minutesUsed);

        return Ok(result);
    }

    [HttpPost("{id:guid}/transcript")]
    public async Task<IActionResult> AddTranscriptEntry(string id, [FromBody] AddTranscriptRequest request, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        if (!await UserOwnsCallAsync(id, userId))
            return NotFound(new { error = "Call not found" });

        var result = await _callService.AddTranscriptEntryAsync(id, request.Speaker, request.Text, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpGet("{id:guid}/transcript")]
    public async Task<IActionResult> GetCallTranscript(string id, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        if (!await UserOwnsCallAsync(id, userId))
            return NotFound(new { error = "Call not found" });

        var result = await _callService.GetCallTranscriptAsync(id, cancellationToken);

        if (!result.IsSuccess)
            return NotFound(result);

        return Ok(result);
    }

    [HttpGet("analytics")]
    [RequireFeature(PlanFeature.AdvancedAnalytics)]
    public async Task<IActionResult> GetCallAnalytics([FromQuery] DateTime startDate, [FromQuery] DateTime endDate, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        // Ensure UTC kind for PostgreSQL compatibility
        if (startDate.Kind == DateTimeKind.Unspecified) startDate = DateTime.SpecifyKind(startDate, DateTimeKind.Utc);
        if (endDate.Kind == DateTimeKind.Unspecified) endDate = DateTime.SpecifyKind(endDate, DateTimeKind.Utc);
        var result = await _callService.GetCallAnalyticsAsync(userId, startDate, endDate, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpGet("dashboard-stats")]
    public async Task<IActionResult> GetDashboardStats(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var now = DateTime.UtcNow;
        var today = now.Date;
        var tomorrow = today.AddDays(1);

        var totalCalls = await _unitOfWork.Repository<Call>().CountAsync(c => c.UserId == userId && c.CreatedAt <= now, cancellationToken);
        var callsToday = await _unitOfWork.Repository<Call>().CountAsync(c => c.UserId == userId && c.CreatedAt >= today && c.CreatedAt <= now, cancellationToken);
        var totalAppointments = await _unitOfWork.Repository<Appointment>().CountAsync(a => a.UserId == userId, cancellationToken);
        var appointmentsToday = await _unitOfWork.Repository<Appointment>().CountAsync(a => a.UserId == userId && a.ScheduledAt >= today && a.ScheduledAt < tomorrow, cancellationToken);
        var totalCustomers = await _unitOfWork.Repository<Customer>().CountAsync(c => c.UserId == userId, cancellationToken);
        var missedCalls = await _unitOfWork.Repository<Call>().CountAsync(c => c.UserId == userId && c.CreatedAt <= now && c.Outcome == CallOutcome.Missed, cancellationToken);

        var allCalls = await _unitOfWork.Repository<Call>().FindAsync(c => c.UserId == userId && c.CreatedAt <= now && c.DurationSeconds > 0, cancellationToken);
        var avgDuration = allCalls.Any() ? allCalls.Average(c => c.DurationSeconds) : 0;

        return Ok(new
        {
            isSuccess = true,
            data = new
            {
                totalCalls,
                callsToday,
                totalAppointments,
                appointmentsToday,
                totalCustomers,
                averageCallDuration = Math.Round(avgDuration),
                missedCalls
            }
        });
    }

    [HttpGet("trends")]
    public async Task<IActionResult> GetCallTrends([FromQuery] int days = 7, CancellationToken cancellationToken = default)
    {
        if (days < 1 || days > 90) days = 7;

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var endDate = DateTime.UtcNow;
        var startDate = endDate.AddDays(-days);

        var callAnalytics = await _analyticsService.GetCallAnalyticsAsync(userId, startDate, endDate, cancellationToken);
        var appointmentAnalytics = await _analyticsService.GetAppointmentAnalyticsAsync(userId, startDate, endDate, cancellationToken);

        // Build appointment lookup
        var appointmentMap = appointmentAnalytics.DailyTrends
            .ToDictionary(d => d.Date.ToString("yyyy-MM-dd"), d => d.AppointmentCount);

        // Build call lookup
        var callMap = callAnalytics.DailyTrends
            .ToDictionary(d => d.Date.ToString("yyyy-MM-dd"), d => new { d.CallCount, d.Duration });

        // Merge into a flat list — every day always has all fields
        var trends = new List<object>();
        for (var d = startDate.Date; d <= endDate.Date; d = d.AddDays(1))
        {
            var key = d.ToString("yyyy-MM-dd");
            callMap.TryGetValue(key, out var call);
            appointmentMap.TryGetValue(key, out var appts);
            trends.Add(new { date = key, calls = call?.CallCount ?? 0, duration = call?.Duration ?? 0, appointments = appts });
        }

        return Ok(new { isSuccess = true, data = trends });
    }
}

public record AddTranscriptRequest(string Speaker, string Text);
