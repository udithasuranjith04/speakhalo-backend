using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SpeakHalo.Application.DTOs;
using SpeakHalo.Application.Interfaces;
using SpeakHalo.Domain.Entities;
using SpeakHalo.Domain.Enums;
using SpeakHalo.Domain.Interfaces.Repositories;

namespace SpeakHalo.API.Controllers;

/// <summary>
/// Validates that the request contains a valid internal API key.
/// Used to secure internal-only endpoints called by the voice pipeline.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class ValidateInternalApiKeyAttribute : Attribute, IAsyncActionFilter
{
    private const string HeaderName = "X-Internal-Api-Key";

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var configuration = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var expectedKey = configuration["Gemini:InternalApiKey"];

        if (string.IsNullOrEmpty(expectedKey))
        {
            // If no internal key is configured, reject all requests
            context.Result = new UnauthorizedObjectResult(new { error = "Internal API key not configured" });
            return;
        }

        var providedKey = context.HttpContext.Request.Headers[HeaderName].FirstOrDefault();

        if (string.IsNullOrEmpty(providedKey) ||
            !System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(
                System.Text.Encoding.UTF8.GetBytes(providedKey),
                System.Text.Encoding.UTF8.GetBytes(expectedKey)))
        {
            context.Result = new UnauthorizedObjectResult(new { error = "Invalid or missing internal API key" });
            return;
        }

        await next();
    }
}

/// <summary>
/// Tool endpoints called during voice conversations.
/// These are invoked by the voice pipeline when Gemini determines
/// a function call is needed (e.g., check availability, book appointment).
/// Secured with internal API key validation - not publicly accessible.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/voice-webhooks")]
[ValidateInternalApiKey]
public class GeminiVoiceWebhookController : ControllerBase
{
    private readonly IAppointmentService _appointmentService;
    private readonly ICustomerService _customerService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUsageTrackingService _usageTracking;
    private readonly ILogger<GeminiVoiceWebhookController> _logger;

    public GeminiVoiceWebhookController(
        IAppointmentService appointmentService,
        ICustomerService customerService,
        IUnitOfWork unitOfWork,
        IUsageTrackingService usageTracking,
        ILogger<GeminiVoiceWebhookController> logger)
    {
        _appointmentService = appointmentService;
        _customerService = customerService;
        _unitOfWork = unitOfWork;
        _usageTracking = usageTracking;
        _logger = logger;
    }

    /// <summary>
    /// Check appointment availability for a given date/time range
    /// </summary>
    [HttpPost("tools/check-availability")]
    public async Task<IActionResult> CheckAvailability(
        [FromBody] CheckAvailabilityRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Checking availability for {Date}", request.Date);

        try
        {
            if (string.IsNullOrEmpty(request.UserId))
                return Ok(new { available = true, message = "Availability checking requires a user ID." });

            var targetDate = DateTime.TryParse(request.Date, out var parsed) ? parsed : DateTime.UtcNow.AddDays(1);
            var result = await _appointmentService.GetAppointmentsByDateRangeAsync(
                request.UserId, targetDate.Date, targetDate.Date.AddDays(1), cancellationToken);

            if (!result.IsSuccess)
                return Ok(new { available = true, date = request.Date, message = "Schedule is open." });

            var bookedTimes = (result.Data ?? new List<AppointmentResponse>())
                .Where(a => a.Status != AppointmentStatus.Cancelled)
                .Select(a => a.ScheduledAt.ToString("h:mm tt"))
                .ToList();

            return Ok(new
            {
                available = true,
                date = request.Date,
                bookedSlots = bookedTimes,
                message = bookedTimes.Count == 0
                    ? $"Wide open on {targetDate:dddd, MMMM d}."
                    : $"Some slots taken on {targetDate:dddd, MMMM d} but still have openings."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking availability");
            return Ok(new { error = "Unable to check availability at this time" });
        }
    }

    /// <summary>
    /// Book an appointment from a voice call
    /// </summary>
    [HttpPost("tools/book-appointment")]
    public async Task<IActionResult> BookAppointment(
        [FromBody] BookAppointmentRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Booking appointment for {CustomerName} at {DateTime}",
            request.CustomerName, request.DateTime);

        try
        {
            if (string.IsNullOrEmpty(request.UserId))
                return Ok(new { success = false, message = "Booking requires a user ID." });

            // Look up or create customer
            string? customerId = null;
            if (!string.IsNullOrEmpty(request.CustomerPhone))
            {
                var customers = await _unitOfWork.Repository<Customer>()
                    .FindAsync(c => c.UserId == request.UserId && c.Phone == request.CustomerPhone && !c.IsDeleted, cancellationToken);
                customerId = customers.FirstOrDefault()?.Id;
            }

            if (string.IsNullOrEmpty(customerId))
                return Ok(new { success = false, message = "Customer not found. Save customer details first." });

            var scheduledAt = System.DateTime.TryParse(request.DateTime, out var parsed)
                ? parsed
                : System.DateTime.UtcNow.AddDays(1).Date.AddHours(9);

            var appointmentRequest = new CreateAppointmentRequest
            {
                UserId = request.UserId,
                CustomerId = customerId,
                ScheduledAt = scheduledAt,
                DurationMinutes = 60,
                Notes = $"Booked via voice webhook. Customer: {request.CustomerName}, Issue: {request.Description}"
            };

            var result = await _appointmentService.CreateAppointmentAsync(appointmentRequest, cancellationToken);

            if (result.IsSuccess)
            {
                return Ok(new
                {
                    success = true,
                    appointmentId = result.Data?.Id,
                    customerName = request.CustomerName,
                    scheduledAt = result.Data?.ScheduledAt.ToString("dddd, MMMM d 'at' h:mm tt"),
                    message = $"Appointment confirmed for {request.CustomerName}"
                });
            }

            return Ok(new { success = false, message = "Unable to book that time slot." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error booking appointment");
            return Ok(new { success = false, error = "Unable to book appointment at this time" });
        }
    }

    /// <summary>
    /// Get business information for the caller
    /// </summary>
    [HttpPost("tools/business-info")]
    public async Task<IActionResult> GetBusinessInfo(
        [FromBody] BusinessInfoRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting business info for user {UserId}", request.UserId);

        try
        {
            if (!string.IsNullOrEmpty(request.UserId))
            {
                var user = await _unitOfWork.Repository<User>().GetByIdAsync(request.UserId, cancellationToken);
                if (user != null)
                {
                    // Read from UserProfile (source of truth) with fallback to deprecated User properties
                    var profiles = await _unitOfWork.Repository<UserProfile>()
                        .FindAsync(p => p.UserId == user.Id, cancellationToken);
                    var profile = profiles.FirstOrDefault();

                    return Ok(new
                    {
#pragma warning disable CS0618 // Fallback to deprecated User properties for data that hasn't been migrated yet
                        businessName = profile?.BusinessName ?? user.BusinessName ?? "SpeakHalo Business",
                        industry = profile != null ? profile.Industry.ToString() : user.Industry.ToString()
#pragma warning restore CS0618
                    });
                }
            }

            return Ok(new
            {
                businessName = "SpeakHalo Business",
                message = "Business information not available"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting business info");
            return Ok(new { error = "Unable to retrieve business information" });
        }
    }

    /// <summary>
    /// Transfer the call to a human agent
    /// </summary>
    [HttpPost("tools/transfer-call")]
    public IActionResult TransferCall(
        [FromBody] TransferCallRequest request)
    {
        _logger.LogInformation("Transferring call {CallSid} to {TransferNumber}",
            request.CallSid, request.TransferNumber);

        return Ok(new
        {
            success = true,
            transferNumber = request.TransferNumber,
            message = "Transferring to a human agent"
        });
    }

    /// <summary>
    /// Post-call processing: save transcript, duration, cost
    /// </summary>
    [HttpPost("post-call")]
    public async Task<IActionResult> PostCallProcessing(
        [FromBody] PostCallRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Post-call processing for call {CallSid}", request.CallSid);

        try
        {
            var calls = await _unitOfWork.Repository<Call>()
                .FindAsync(c => c.TwilioCallSid == request.CallSid, cancellationToken);
            var call = calls.FirstOrDefault();

            if (call != null)
            {
                call.DurationSeconds = request.DurationSeconds;
                call.Notes = $"{call.Notes}\nTranscript: {request.Transcript}";
                call.EndedAt = DateTime.UtcNow;

                _unitOfWork.Repository<Call>().Update(call);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // Track AI minutes usage (round up to nearest minute)
                var minutesUsed = Math.Max(1, (int)Math.Ceiling(request.DurationSeconds / 60.0));
                if (!string.IsNullOrEmpty(call.UserId))
                {
                    await _usageTracking.TrackAIMinutesAsync(call.UserId, minutesUsed);
                    _logger.LogInformation("Tracked {Minutes} AI minutes for user {UserId} from call {CallId}",
                        minutesUsed, call.UserId, call.Id);
                }

                _logger.LogInformation("Updated call {CallId} with transcript and duration", call.Id);
            }

            return Ok(new { success = true, callSid = request.CallSid });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing post-call data for {CallSid}", request.CallSid);
            return Ok(new { success = false, error = "Failed to process post-call data" });
        }
    }
}

public class CheckAvailabilityRequest
{
    public string? Date { get; set; }
    public string? TimeRange { get; set; }
    public string? ServiceType { get; set; }
    public string? UserId { get; set; }
}

public class BookAppointmentRequest
{
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string? CustomerEmail { get; set; }
    public string? CustomerAddress { get; set; }
    public string DateTime { get; set; } = string.Empty;
    public string ServiceType { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? UserId { get; set; }
}

public class BusinessInfoRequest
{
    public string? UserId { get; set; }
    public string? InfoType { get; set; }
}

public class TransferCallRequest
{
    public string? CallSid { get; set; }
    public string? TransferNumber { get; set; }
    public string? Reason { get; set; }
}

public class PostCallRequest
{
    public string? CallSid { get; set; }
    public string? Transcript { get; set; }
    public int DurationSeconds { get; set; }
    public decimal? Cost { get; set; }
    public string? Summary { get; set; }
}
