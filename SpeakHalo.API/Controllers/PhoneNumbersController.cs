using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
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
public class PhoneNumbersController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITwilioService _twilioService;
    private readonly ILogger<PhoneNumbersController> _logger;
    private readonly PhoneNumberSettings _settings;

    public PhoneNumbersController(
        IUnitOfWork unitOfWork,
        ITwilioService twilioService,
        ILogger<PhoneNumbersController> logger,
        IOptions<PhoneNumberSettings> settings)
    {
        _unitOfWork = unitOfWork;
        _twilioService = twilioService;
        _logger = logger;
        _settings = settings.Value;
    }

    [HttpGet]
    public async Task<IActionResult> ListPhoneNumbers(CancellationToken cancellationToken)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var phoneNumbers = await _unitOfWork.Repository<PhoneNumber>()
            .FindAsync(p => p.UserId == userId, cancellationToken);

        var response = phoneNumbers.Select(p => new PhoneNumberResponse
        {
            Id = p.Id,
            UserId = p.UserId,
            Number = p.Number,
            FriendlyName = p.FriendlyName,
            CountryCode = p.CountryCode,
            IsPrimary = p.IsPrimary,
            WhatsAppEnabled = p.WhatsAppEnabled,
            WhatsAppVerified = p.WhatsAppVerified,
            VoiceEnabled = p.VoiceEnabled,
            SmsEnabled = p.SmsEnabled,
            TwilioSid = p.TwilioSid,
            CreatedAt = p.CreatedAt
        }).OrderByDescending(p => p.IsPrimary).ThenByDescending(p => p.CreatedAt);

        return Ok(response);
    }

    [HttpGet("countries")]
    public IActionResult GetSupportedCountries()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var countries = _settings.SupportedCountries.Select(c => new CountryResponse
        {
            Code = c.Key,
            Name = c.Value.Name,
            Region = c.Value.Region,
            PhoneNumberMonthly = c.Value.PhoneNumberMonthly
        }).OrderBy(c => c.Name);

        return Ok(countries);
    }

    [HttpGet("available")]
    public async Task<IActionResult> SearchAvailableNumbers(
        [FromQuery] string countryCode = "US",
        [FromQuery] string? areaCode = null,
        CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        if (!_settings.SupportedCountries.ContainsKey(countryCode))
            return BadRequest(new { error = $"Country code {countryCode} is not supported" });

        try
        {
            var availableNumbers = await _twilioService.SearchAvailableNumbersAsync(
                countryCode,
                areaCode,
                cancellationToken);

            return Ok(availableNumbers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching available numbers for country {CountryCode}", countryCode);
            return StatusCode(500, new { error = "Failed to search available numbers" });
        }
    }

    [HttpPost("provision")]
    public async Task<IActionResult> ProvisionPhoneNumber(
        [FromBody] ProvisionPhoneNumberRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        if (!_settings.SupportedCountries.ContainsKey(request.CountryCode))
            return BadRequest(new { error = $"Country code {request.CountryCode} is not supported" });

        try
        {
            var user = await _unitOfWork.Repository<User>().GetByIdAsync(userId, cancellationToken);
            if (user == null)
                return NotFound(new { error = "User not found" });

            // Get max phone numbers from UserSubscription (admin sets this per user)
            var subscriptions = await _unitOfWork.Repository<Domain.Entities.UserSubscription>()
                .FindAsync(s => s.UserId == userId, cancellationToken);
            var subscription = subscriptions.FirstOrDefault();
            var maxNumbers = subscription?.MaxPhoneNumbers ?? 1;

            // Use transaction to prevent race condition on concurrent provisioning
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                // Count inside transaction to get accurate count
                var currentCount = await _unitOfWork.Repository<PhoneNumber>()
                    .CountAsync(p => p.UserId == userId, cancellationToken);

                if (currentCount >= maxNumbers)
                    return BadRequest(new { error = $"Phone number limit reached ({maxNumbers}). Contact your administrator to increase the limit." });

                var twilioNumber = await _twilioService.ProvisionPhoneNumberAsync(
                    request.PhoneNumber,
                    request.CountryCode,
                    cancellationToken);

                var phoneNumber = new PhoneNumber
                {
                    UserId = userId,
                    Number = twilioNumber.PhoneNumber,
                    FriendlyName = request.FriendlyName ?? twilioNumber.FriendlyName,
                    CountryCode = request.CountryCode,
                    IsPrimary = currentCount == 0,
                    TwilioSid = twilioNumber.Sid,
                    VoiceEnabled = true,
                    SmsEnabled = true,
                    WhatsAppEnabled = false,
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.Repository<PhoneNumber>().AddAsync(phoneNumber, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                _logger.LogInformation("User {UserId} provisioned phone number {Number}", userId, phoneNumber.Number);

                var response = new PhoneNumberResponse
                {
                    Id = phoneNumber.Id,
                    UserId = phoneNumber.UserId,
                    Number = phoneNumber.Number,
                    FriendlyName = phoneNumber.FriendlyName,
                    CountryCode = phoneNumber.CountryCode,
                    IsPrimary = phoneNumber.IsPrimary,
                    WhatsAppEnabled = phoneNumber.WhatsAppEnabled,
                    WhatsAppVerified = phoneNumber.WhatsAppVerified,
                    VoiceEnabled = phoneNumber.VoiceEnabled,
                    SmsEnabled = phoneNumber.SmsEnabled,
                    TwilioSid = phoneNumber.TwilioSid,
                    CreatedAt = phoneNumber.CreatedAt
                };

                return CreatedAtAction(nameof(GetPhoneNumber), new { id = phoneNumber.Id }, response);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error provisioning phone number for user {UserId}", userId);
            return StatusCode(500, new { error = "Failed to provision phone number" });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetPhoneNumber(string id, CancellationToken cancellationToken)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var phoneNumber = await _unitOfWork.Repository<PhoneNumber>()
            .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId, cancellationToken);

        if (phoneNumber == null)
            return NotFound(new { error = "Phone number not found" });

        var response = new PhoneNumberResponse
        {
            Id = phoneNumber.Id,
            UserId = phoneNumber.UserId,
            Number = phoneNumber.Number,
            FriendlyName = phoneNumber.FriendlyName,
            CountryCode = phoneNumber.CountryCode,
            IsPrimary = phoneNumber.IsPrimary,
            WhatsAppEnabled = phoneNumber.WhatsAppEnabled,
            WhatsAppVerified = phoneNumber.WhatsAppVerified,
            VoiceEnabled = phoneNumber.VoiceEnabled,
            SmsEnabled = phoneNumber.SmsEnabled,
            TwilioSid = phoneNumber.TwilioSid,
            CreatedAt = phoneNumber.CreatedAt
        };

        return Ok(response);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdatePhoneNumber(
        string id,
        [FromBody] UpdatePhoneNumberRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var phoneNumber = await _unitOfWork.Repository<PhoneNumber>()
            .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId, cancellationToken);

        if (phoneNumber == null)
            return NotFound(new { error = "Phone number not found" });

        if (request.FriendlyName != null)
            phoneNumber.FriendlyName = request.FriendlyName;

        if (request.VoiceEnabled.HasValue)
            phoneNumber.VoiceEnabled = request.VoiceEnabled.Value;

        if (request.SmsEnabled.HasValue)
            phoneNumber.SmsEnabled = request.SmsEnabled.Value;

        phoneNumber.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Repository<PhoneNumber>().Update(phoneNumber);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var response = new PhoneNumberResponse
        {
            Id = phoneNumber.Id,
            UserId = phoneNumber.UserId,
            Number = phoneNumber.Number,
            FriendlyName = phoneNumber.FriendlyName,
            CountryCode = phoneNumber.CountryCode,
            IsPrimary = phoneNumber.IsPrimary,
            WhatsAppEnabled = phoneNumber.WhatsAppEnabled,
            WhatsAppVerified = phoneNumber.WhatsAppVerified,
            VoiceEnabled = phoneNumber.VoiceEnabled,
            SmsEnabled = phoneNumber.SmsEnabled,
            TwilioSid = phoneNumber.TwilioSid,
            CreatedAt = phoneNumber.CreatedAt
        };

        return Ok(response);
    }

    [HttpPost("{id}/set-primary")]
    public async Task<IActionResult> SetPrimaryNumber(string id, CancellationToken cancellationToken)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var phoneNumber = await _unitOfWork.Repository<PhoneNumber>()
            .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId, cancellationToken);

        if (phoneNumber == null)
            return NotFound(new { error = "Phone number not found" });

        var currentPrimary = await _unitOfWork.Repository<PhoneNumber>()
            .FirstOrDefaultAsync(p => p.UserId == userId && p.IsPrimary, cancellationToken);

        if (currentPrimary != null && currentPrimary.Id != id)
        {
            currentPrimary.IsPrimary = false;
            currentPrimary.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Repository<PhoneNumber>().Update(currentPrimary);
        }

        phoneNumber.IsPrimary = true;
        phoneNumber.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Repository<PhoneNumber>().Update(phoneNumber);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User {UserId} set phone number {Number} as primary", userId, phoneNumber.Number);

        return Ok(new { message = "Phone number set as primary" });
    }

    [HttpPost("{id}/whatsapp")]
    [RequireFeature(PlanFeature.WhatsApp)]
    public async Task<IActionResult> ConfigureWhatsApp(
        string id,
        [FromBody] ConfigureWhatsAppRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var phoneNumber = await _unitOfWork.Repository<PhoneNumber>()
            .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId, cancellationToken);

        if (phoneNumber == null)
            return NotFound(new { error = "Phone number not found" });

        if (request.Enabled)
        {
            if (string.IsNullOrEmpty(phoneNumber.TwilioSid))
                return BadRequest(new { error = "Phone number is not provisioned with Twilio yet." });

            var result = await _twilioService.EnableWhatsAppForNumberAsync(phoneNumber.TwilioSid, cancellationToken);
            if (!result)
            {
                return BadRequest(new { error = "Failed to enable WhatsApp. Please ensure the number supports WhatsApp." });
            }
        }

        phoneNumber.WhatsAppEnabled = request.Enabled;
        phoneNumber.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Repository<PhoneNumber>().Update(phoneNumber);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User {UserId} {Action} WhatsApp for number {Number}",
            userId, request.Enabled ? "enabled" : "disabled", phoneNumber.Number);

        return Ok(new { message = $"WhatsApp {(request.Enabled ? "enabled" : "disabled")} successfully" });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> ReleasePhoneNumber(string id, CancellationToken cancellationToken)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var phoneNumber = await _unitOfWork.Repository<PhoneNumber>()
            .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId, cancellationToken);

        if (phoneNumber == null)
            return NotFound(new { error = "Phone number not found" });

        if (phoneNumber.IsPrimary)
        {
            var otherNumbers = await _unitOfWork.Repository<PhoneNumber>()
                .CountAsync(p => p.UserId == userId && p.Id != id, cancellationToken);

            if (otherNumbers > 0)
            {
                return BadRequest(new { error = "Cannot release primary number. Please set another number as primary first." });
            }
        }

        if (!string.IsNullOrEmpty(phoneNumber.TwilioSid))
        {
            await _twilioService.ReleasePhoneNumberAsync(phoneNumber.TwilioSid, cancellationToken);
        }

        _unitOfWork.Repository<PhoneNumber>().Remove(phoneNumber);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User {UserId} released phone number {Number}", userId, phoneNumber.Number);

        return Ok(new { message = "Phone number released successfully" });
    }

}

public class PhoneNumberSettings
{
    public Dictionary<string, CountryInfo> SupportedCountries { get; set; } = new();
}

public class CountryInfo
{
    public string Name { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public decimal PhoneNumberMonthly { get; set; } = 1.00m;
}
