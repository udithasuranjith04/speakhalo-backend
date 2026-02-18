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

    public CallsController(ICallService callService, IUsageTrackingService usageTracking, IUnitOfWork unitOfWork)
    {
        _callService = callService;
        _usageTracking = usageTracking;
        _unitOfWork = unitOfWork;
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

    [HttpGet("{id}")]
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
    public async Task<IActionResult> GetUserCalls([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _callService.GetUserCallsAsync(userId, pageNumber, pageSize, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPut("{id}")]
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

    [HttpPost("{id}/end")]
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

    [HttpPost("{id}/transcript")]
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

    [HttpGet("{id}/transcript")]
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
        var result = await _callService.GetCallAnalyticsAsync(userId, startDate, endDate, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(result);

        return Ok(result);
    }
}

public record AddTranscriptRequest(string Speaker, string Text);
