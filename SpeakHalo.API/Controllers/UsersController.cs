using System.Text.Json;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CustomerReminderPreferences = SpeakHalo.Application.BackgroundJobs.CustomerReminderPreferences;
using SpeakHalo.Application.DTOs;
using SpeakHalo.Application.Interfaces;
using SpeakHalo.Domain.Entities;
using SpeakHalo.Domain.Interfaces.Repositories;
using System.Security.Claims;

namespace SpeakHalo.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IGeocodingService _geocodingService;

    public UsersController(IUserService userService, IUnitOfWork unitOfWork, IGeocodingService geocodingService)
    {
        _userService = userService;
        _unitOfWork = unitOfWork;
        _geocodingService = geocodingService;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetUserById(string id, CancellationToken cancellationToken)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isAdmin = User.IsInRole("Admin");
        if (id != currentUserId && !isAdmin)
            return Forbid();

        var result = await _userService.GetUserByIdAsync(id, cancellationToken);

        if (!result.IsSuccess)
            return NotFound(result);

        return Ok(result);
    }

    [HttpGet("email/{email}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetUserByEmail(string email, CancellationToken cancellationToken)
    {
        var result = await _userService.GetUserByEmailAsync(email, cancellationToken);

        if (!result.IsSuccess)
            return NotFound(result);

        return Ok(result);
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAllUsers([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var result = await _userService.GetAllUsersAsync(pageNumber, pageSize, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(string id, [FromBody] UpdateUserRequest request, CancellationToken cancellationToken)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        if (id != currentUserId)
            return Forbid();

        var result = await _userService.UpdateUserAsync(id, request, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPost("{id}/password")]
    public async Task<IActionResult> UpdatePassword(string id, [FromBody] UpdatePasswordRequest request, CancellationToken cancellationToken)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        if (id != currentUserId)
            return Forbid();

        var result = await _userService.UpdatePasswordAsync(id, request.CurrentPassword, request.NewPassword, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeactivateUser(string id, CancellationToken cancellationToken)
    {
        var result = await _userService.DeactivateUserAsync(id, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpGet("caller-recognition")]
    public async Task<IActionResult> GetCallerRecognitionSetting(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var profiles = await _unitOfWork.Repository<UserProfile>()
            .FindAsync(p => p.UserId == userId, cancellationToken);
        var profile = profiles.FirstOrDefault();

        return Ok(new { enabled = profile?.CallerRecognitionEnabled ?? false });
    }

    [HttpPut("caller-recognition")]
    public async Task<IActionResult> SetCallerRecognitionSetting(
        [FromBody] CallerRecognitionRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var profiles = await _unitOfWork.Repository<UserProfile>()
            .FindAsync(p => p.UserId == userId, cancellationToken);
        var profile = profiles.FirstOrDefault();

        if (profile == null)
            return NotFound(new { error = "User profile not found" });

        profile.CallerRecognitionEnabled = request.Enabled;
        profile.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Repository<UserProfile>().Update(profile);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(new { enabled = profile.CallerRecognitionEnabled });
    }

    [HttpGet("reminder-settings")]
    public async Task<IActionResult> GetReminderSettings(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var profiles = await _unitOfWork.Repository<UserProfile>()
            .FindAsync(p => p.UserId == userId, cancellationToken);
        var profile = profiles.FirstOrDefault();

        if (profile?.NotificationSettings != null)
        {
            try
            {
                var settings = JsonSerializer.Deserialize<CustomerReminderPreferences>(profile.NotificationSettings,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (settings != null)
                    return Ok(settings);
            }
            catch
            {
                // Fall through to defaults
            }
        }

        return Ok(CustomerReminderPreferences.Default);
    }

    [HttpPut("reminder-settings")]
    public async Task<IActionResult> UpdateReminderSettings(
        [FromBody] ReminderSettingsRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var profiles = await _unitOfWork.Repository<UserProfile>()
            .FindAsync(p => p.UserId == userId, cancellationToken);
        var profile = profiles.FirstOrDefault();

        if (profile == null)
            return NotFound(new { error = "User profile not found" });

        // Validate channels
        var validChannels = new HashSet<string> { "sms", "whatsapp" };
        var channels = request.ReminderChannels?
            .Where(c => validChannels.Contains(c.ToLowerInvariant()))
            .Select(c => c.ToLowerInvariant())
            .Distinct()
            .ToList() ?? new List<string> { "sms" };

        var settings = new CustomerReminderPreferences
        {
            Reminder24hEnabled = request.Reminder24hEnabled,
            Reminder2hEnabled = request.Reminder2hEnabled,
            ReminderChannels = channels,
            CustomMessage24h = request.CustomMessage24h?.Trim(),
            CustomMessage2h = request.CustomMessage2h?.Trim()
        };

        profile.NotificationSettings = JsonSerializer.Serialize(settings,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        profile.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Repository<UserProfile>().Update(profile);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(settings);
    }

    [HttpGet("business-location")]
    public async Task<IActionResult> GetBusinessLocation(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var profiles = await _unitOfWork.Repository<UserProfile>()
            .FindAsync(p => p.UserId == userId, cancellationToken);
        var profile = profiles.FirstOrDefault();

        if (profile == null || string.IsNullOrWhiteSpace(profile.BusinessAddress))
            return Ok(new { hasLocation = false });

        var coords = await _geocodingService.GeocodeAddressAsync(profile.BusinessAddress, null, null, null, cancellationToken);
        if (coords == null)
            return Ok(new { hasLocation = false, address = profile.BusinessAddress });

        return Ok(new
        {
            hasLocation = true,
            latitude = coords.Value.Latitude,
            longitude = coords.Value.Longitude,
            address = profile.BusinessAddress,
            businessName = profile.BusinessName
        });
    }
}

public record UpdatePasswordRequest(string CurrentPassword, string NewPassword);
public record CallerRecognitionRequest(bool Enabled);
public record ReminderSettingsRequest(
    bool Reminder24hEnabled,
    bool Reminder2hEnabled,
    List<string>? ReminderChannels,
    string? CustomMessage24h,
    string? CustomMessage2h
);
