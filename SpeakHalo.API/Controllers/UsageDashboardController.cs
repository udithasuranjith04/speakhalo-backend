using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpeakHalo.Application.Interfaces;
using System.Security.Claims;

namespace SpeakHalo.API.Controllers;

/// <summary>
/// Usage dashboard - provides usage overview for admin.
/// Pending rework: billing/overage system removed, admin sets limits per user directly.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class UsageDashboardController : ControllerBase
{
    private readonly IUsageTrackingService _usageTracking;
    private readonly ILogger<UsageDashboardController> _logger;

    public UsageDashboardController(
        IUsageTrackingService usageTracking,
        ILogger<UsageDashboardController> logger)
    {
        _usageTracking = usageTracking;
        _logger = logger;
    }

    /// <summary>
    /// Get current usage overview for the authenticated user
    /// </summary>
    [HttpGet("overview")]
    public async Task<IActionResult> GetUsageOverview()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(new { message = "User not authenticated" });

        try
        {
            var usage = await _usageTracking.GetUsageReportAsync(userId);
            return Ok(usage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting usage overview for user {UserId}", userId);
            return StatusCode(500, new { message = "Failed to retrieve usage overview" });
        }
    }

    #region Admin Endpoints

    /// <summary>
    /// Get usage overview for a specific user (Admin only)
    /// </summary>
    [HttpGet("admin/user/{userId}/overview")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetUserOverview(string userId)
    {
        if (string.IsNullOrEmpty(userId))
            return BadRequest(new { message = "User ID is required" });

        try
        {
            var usage = await _usageTracking.GetUsageReportAsync(userId);
            return Ok(usage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user overview for admin, userId: {UserId}", userId);
            return StatusCode(500, new { message = "Failed to retrieve user overview" });
        }
    }

    #endregion
}
