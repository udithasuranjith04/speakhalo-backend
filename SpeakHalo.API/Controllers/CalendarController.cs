using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using SpeakHalo.API.Filters;
using SpeakHalo.Application.Interfaces;
using SpeakHalo.Domain.Enums;

namespace SpeakHalo.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
[RequireFeature(PlanFeature.Calendar)]
public class CalendarController : ControllerBase
{
    private readonly ICalendarService _calendarService;
    private readonly IConfiguration _configuration;

    public CalendarController(ICalendarService calendarService, IConfiguration configuration)
    {
        _calendarService = calendarService;
        _configuration = configuration;
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetStatus(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _calendarService.IsConnectedAsync(userId, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(result);

        return Ok(new { isSuccess = true, data = new { connected = result.Data } });
    }

    [HttpGet("auth-url")]
    public async Task<IActionResult> GetAuthUrl(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _calendarService.GetAuthorizationUrlAsync(userId, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(result);

        return Ok(new { isSuccess = true, data = new { url = result.Data } });
    }

    [HttpGet("oauth/callback")]
    [AllowAnonymous]
    public async Task<IActionResult> OAuthCallback([FromQuery] string? code, [FromQuery] string? state, [FromQuery] string? error, CancellationToken cancellationToken)
    {
        var frontendUrl = _configuration["Frontend:BaseUrl"];
        if (string.IsNullOrEmpty(frontendUrl))
            frontendUrl = _configuration["Application:FrontendUrl"];
        if (string.IsNullOrEmpty(frontendUrl))
            frontendUrl = "https://speakhalo.com";
        frontendUrl = frontendUrl.TrimEnd('/');

        if (!string.IsNullOrEmpty(error))
        {
            return Redirect($"{frontendUrl}/dashboard/settings?tab=integrations&calendar=error");
        }

        if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state))
        {
            return Redirect($"{frontendUrl}/dashboard/settings?tab=integrations&calendar=error");
        }

        var result = await _calendarService.HandleOAuthCallbackAsync(code, state, cancellationToken);

        var status = result.IsSuccess ? "success" : "error";
        return Redirect($"{frontendUrl}/dashboard/settings?tab=integrations&calendar={status}");
    }

    [HttpPost("disconnect")]
    public async Task<IActionResult> Disconnect(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _calendarService.DisconnectAsync(userId, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(result);

        return Ok(new { isSuccess = true, message = "Google Calendar disconnected successfully." });
    }
}
