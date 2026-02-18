using System.Security.Cryptography;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpeakHalo.API.Filters;
using SpeakHalo.Application.Interfaces;
using SpeakHalo.Domain.Entities;
using SpeakHalo.Domain.Interfaces.Repositories;

namespace SpeakHalo.API.Controllers;

/// <summary>
/// Handles incoming Twilio voice webhooks.
/// Routes calls to Gemini-powered voice agent via Twilio Media Streams.
/// Configured as the voice webhook URL on Twilio phone numbers.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/webhooks/twilio")]
[AllowAnonymous]
[ValidateTwilioSignature]
public class TwilioVoiceWebhookController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICallerRecognitionService _callerRecognition;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TwilioVoiceWebhookController> _logger;

    public TwilioVoiceWebhookController(
        IUnitOfWork unitOfWork,
        ICallerRecognitionService callerRecognition,
        IConfiguration configuration,
        ILogger<TwilioVoiceWebhookController> logger)
    {
        _unitOfWork = unitOfWork;
        _callerRecognition = callerRecognition;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Handle incoming voice call from Twilio.
    /// Returns TwiML that starts a Media Stream WebSocket connection to our Gemini voice agent.
    /// </summary>
    [HttpPost("voice")]
    [Consumes("application/x-www-form-urlencoded")]
    public async Task<IActionResult> HandleIncomingCall(
        [FromForm] TwilioVoiceRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Incoming call from {From} to {To}, CallSid: {CallSid}",
            request.From, request.To, request.CallSid);

        try
        {
            // Find the phone number and associated user
            var phoneNumbers = await _unitOfWork.Repository<PhoneNumber>()
                .FindAsync(p => p.Number == request.To || p.Number == request.Called, cancellationToken);
            var phoneNumber = phoneNumbers.FirstOrDefault();

            string? userId = phoneNumber?.UserId;
            string agentId = "default";

            if (userId != null)
            {
                // Look up the user's active VoiceAgent to pass its real ID to the voice service
                var voiceAgents = await _unitOfWork.Repository<VoiceAgent>()
                    .FindAsync(a => a.UserId == userId && !a.IsDeleted, cancellationToken);
                var voiceAgent = voiceAgents.FirstOrDefault();
                if (voiceAgent != null)
                {
                    agentId = voiceAgent.Id;
                }

            }

            // Recognize returning customer (opt-in, multi-tenant)
            string? customerId = null;
            if (userId != null && !string.IsNullOrEmpty(request.From))
            {
                var recognized = await _callerRecognition.RecognizeCallerAsync(userId, request.From, "voice", cancellationToken);
                if (recognized != null && recognized.CustomerId != null)
                    customerId = recognized.CustomerId;
            }

            // Generate a one-time stream token for WebSocket authentication
            var streamToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();
            var streamTokenHash = Convert.ToHexString(
                SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(streamToken))).ToLowerInvariant();

            // Create a Call record for tracking (store hash, not plaintext token)
            var call = new Call
            {
                UserId = userId ?? "",
                PhoneNumberId = phoneNumber?.Id,
                CustomerId = customerId,
                CallerPhone = request.From ?? "",
                CallerName = request.CallerName ?? "",
                RecipientPhone = request.To ?? "",
                Direction = "inbound",
                TwilioCallSid = request.CallSid,
                Notes = $"Gemini Voice Agent: {agentId}\nStreamToken: {streamTokenHash}",
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Repository<Call>().AddAsync(call, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Build the WebSocket URL for Media Streams
            var baseUrl = _configuration["Application:BaseUrl"] ?? "https://api.speakhalo.com";
            var wsHost = new Uri(baseUrl).Host;
            var callerPhone = Uri.EscapeDataString(request.From ?? "");
            var mediaStreamUrl = $"wss://{wsHost}/api/v1/voice-stream/{agentId}?callerPhone={callerPhone}&token={streamToken}";

            // Return TwiML that connects Twilio to our WebSocket voice agent
            // The action URL is called when the media stream ends (AI disconnect or transfer)
            var streamEndedUrl = $"{baseUrl}/api/v1/webhooks/twilio/stream-ended";
            var twiml = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<Response>
    <Connect action=""{streamEndedUrl}"">
        <Stream url=""{mediaStreamUrl}"" />
    </Connect>
</Response>";

            _logger.LogInformation("Connecting call {CallSid} to voice stream at {Url}",
                request.CallSid, mediaStreamUrl);

            return Content(twiml, "application/xml");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling incoming voice call from {From}", request.From);

            var errorTwiml = """
                <?xml version="1.0" encoding="UTF-8"?>
                <Response>
                    <Say voice="Polly.Joanna">We apologize, but we're experiencing technical difficulties. Please try again later or leave a message after the beep.</Say>
                    <Record maxLength="120" transcribe="true" />
                </Response>
                """;

            return Content(errorTwiml, "application/xml");
        }
    }

    /// <summary>
    /// Handle call status updates from Twilio
    /// </summary>
    [HttpPost("status")]
    [Consumes("application/x-www-form-urlencoded")]
    public async Task<IActionResult> HandleStatusCallback(
        [FromForm] TwilioStatusRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Call status update: {CallSid} -> {CallStatus}, Duration: {Duration}s",
            request.CallSid, request.CallStatus, request.CallDuration);

        try
        {
            if (request.CallStatus is "completed" or "failed" or "busy" or "no-answer" or "canceled")
            {
                var calls = await _unitOfWork.Repository<Call>()
                    .FindAsync(c => c.TwilioCallSid == request.CallSid, cancellationToken);
                var call = calls.FirstOrDefault();

                if (call != null)
                {
                    call.DurationSeconds = int.TryParse(request.CallDuration, out var dur) ? dur : 0;
                    call.EndedAt = DateTime.UtcNow;

                    _unitOfWork.Repository<Call>().Update(call);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);

                    _logger.LogInformation("Updated call {CallId} status to {Status}, duration {Duration}s",
                        call.Id, request.CallStatus, call.DurationSeconds);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling status callback for {CallSid}", request.CallSid);
        }

        return Ok();
    }

    /// <summary>
    /// Called by Twilio when the media stream WebSocket closes.
    /// If the AI triggered a transfer, returns Dial TwiML to connect the caller to the human.
    /// Otherwise returns empty Response to end the call normally.
    /// </summary>
    [HttpPost("stream-ended")]
    [Consumes("application/x-www-form-urlencoded")]
    public async Task<IActionResult> HandleStreamEnded(
        [FromForm] TwilioVoiceRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stream ended for call {CallSid}", request.CallSid);

        try
        {
            if (string.IsNullOrEmpty(request.CallSid))
            {
                _logger.LogWarning("Stream-ended callback received without CallSid");
                return Content("<?xml version=\"1.0\" encoding=\"UTF-8\"?><Response/>", "application/xml");
            }

            var calls = await _unitOfWork.Repository<Call>()
                .FindAsync(c => c.TwilioCallSid == request.CallSid && !c.IsDeleted, cancellationToken);
            var call = calls.FirstOrDefault();

            if (call is { WasTransferred: true } && !string.IsNullOrWhiteSpace(call.TransferredToNumber))
            {
                _logger.LogInformation(
                    "Transferring call {CallSid} to {Number}",
                    request.CallSid, call.TransferredToNumber);

                // Use the business phone number as callerId so the human sees a familiar number
                var callerIdNumber = call.RecipientPhone;
                var baseUrl = _configuration["Application:BaseUrl"] ?? "https://api.speakhalo.com";
                var dialCompleteUrl = $"{baseUrl}/api/v1/webhooks/twilio/dial-complete";

                var twiml = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<Response>
    <Say voice=""Polly.Joanna"">Please hold while I connect you.</Say>
    <Dial timeout=""30"" callerId=""{callerIdNumber}"" action=""{dialCompleteUrl}"">
        <Number>{call.TransferredToNumber}</Number>
    </Dial>
</Response>";

                return Content(twiml, "application/xml");
            }

            // No transfer — end call normally
            _logger.LogInformation("No transfer pending for call {CallSid}, ending normally", request.CallSid);
            return Content("<?xml version=\"1.0\" encoding=\"UTF-8\"?><Response/>", "application/xml");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling stream-ended for call {CallSid}", request.CallSid);
            return Content("<?xml version=\"1.0\" encoding=\"UTF-8\"?><Response/>", "application/xml");
        }
    }

    /// <summary>
    /// Called by Twilio when the Dial leg completes (human answered and hung up, or didn't answer).
    /// Records transfer outcome. If the human didn't answer, offers voicemail.
    /// </summary>
    [HttpPost("dial-complete")]
    [Consumes("application/x-www-form-urlencoded")]
    public async Task<IActionResult> HandleDialComplete(
        [FromForm] TwilioDialStatusRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Dial complete for call {CallSid}: DialCallStatus={DialStatus}, Duration={Duration}s",
            request.CallSid, request.DialCallStatus, request.DialCallDuration);

        try
        {
            if (string.IsNullOrEmpty(request.CallSid))
            {
                return Content("<?xml version=\"1.0\" encoding=\"UTF-8\"?><Response/>", "application/xml");
            }

            var calls = await _unitOfWork.Repository<Call>()
                .FindAsync(c => c.TwilioCallSid == request.CallSid && !c.IsDeleted, cancellationToken);
            var call = calls.FirstOrDefault();

            if (call != null)
            {
                if (request.DialCallStatus == "completed")
                {
                    // Human answered and call completed successfully
                    call.TransferDurationSeconds = int.TryParse(request.DialCallDuration, out var dur) ? dur : null;
                    call.Notes = $"{call.Notes}\n[TRANSFER COMPLETED] Duration: {request.DialCallDuration}s";
                    _logger.LogInformation("Transfer completed for call {CallSid}, duration {Duration}s",
                        request.CallSid, request.DialCallDuration);
                }
                else
                {
                    // Human didn't answer, was busy, or call failed
                    call.WasTransferred = false;
                    call.Notes = $"{call.Notes}\n[TRANSFER FAILED] Status: {request.DialCallStatus}";
                    _logger.LogWarning("Transfer failed for call {CallSid}: {Status}",
                        request.CallSid, request.DialCallStatus);
                }

                _unitOfWork.Repository<Call>().Update(call);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            // If the human didn't answer, offer voicemail to the caller
            if (request.DialCallStatus is "no-answer" or "busy" or "failed")
            {
                var voicemailTwiml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<Response>
    <Say voice=""Polly.Joanna"">Sorry, no one is available right now. Please leave a message after the beep and we'll get back to you as soon as possible.</Say>
    <Record maxLength=""120"" transcribe=""true"" />
</Response>";

                return Content(voicemailTwiml, "application/xml");
            }

            // Transfer completed normally — end call
            return Content("<?xml version=\"1.0\" encoding=\"UTF-8\"?><Response/>", "application/xml");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling dial-complete for call {CallSid}", request.CallSid);
            return Content("<?xml version=\"1.0\" encoding=\"UTF-8\"?><Response/>", "application/xml");
        }
    }
}

public class TwilioVoiceRequest
{
    public string? CallSid { get; set; }
    public string? From { get; set; }
    public string? To { get; set; }
    public string? Called { get; set; }
    public string? CallerName { get; set; }
    public string? FromCity { get; set; }
    public string? FromState { get; set; }
    public string? FromCountry { get; set; }
    public string? CallStatus { get; set; }
    public string? Direction { get; set; }
}

public class TwilioStatusRequest
{
    public string? CallSid { get; set; }
    public string? CallStatus { get; set; }
    public string? CallDuration { get; set; }
    public string? RecordingUrl { get; set; }
    public string? RecordingSid { get; set; }
    public string? RecordingDuration { get; set; }
}

public class TwilioDialStatusRequest
{
    public string? CallSid { get; set; }
    public string? DialCallStatus { get; set; }
    public string? DialCallDuration { get; set; }
    public string? DialCallSid { get; set; }
    public string? RecordingUrl { get; set; }
}
