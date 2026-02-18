using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpeakHalo.API.Filters;
using SpeakHalo.Application.Interfaces;

namespace SpeakHalo.API.Controllers;

[ApiController]
[Route("api/v1/webhooks/twilio/sms")]
[AllowAnonymous]
[ValidateTwilioSignature]
public class SMSWebhookController : ControllerBase
{
    private readonly ISMSConversationService _smsService;
    private readonly IUsageTrackingService _usageTracking;
    private readonly ILogger<SMSWebhookController> _logger;

    public SMSWebhookController(
        ISMSConversationService smsService,
        IUsageTrackingService usageTracking,
        ILogger<SMSWebhookController> logger)
    {
        _smsService = smsService;
        _usageTracking = usageTracking;
        _logger = logger;
    }

    /// <summary>
    /// Handle incoming SMS from Twilio
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> HandleIncomingSMS([FromForm] TwilioSmsWebhook webhook, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Incoming SMS from {From} to {To}, MessageSid: {MessageSid}", webhook.From, webhook.To, webhook.MessageSid);

        var result = await _smsService.HandleIncomingSMSAsync(
            webhook.From,
            webhook.To,
            webhook.Body ?? string.Empty,
            webhook.MessageSid,
            cancellationToken
        );

        if (!result.IsSuccess)
        {
            _logger.LogWarning("Failed to handle incoming SMS from {From}: {Errors}", webhook.From, string.Join(", ", result.Errors));
            return BadRequest();
        }

        return Ok();
    }

    /// <summary>
    /// Handle SMS delivery status callbacks from Twilio
    /// </summary>
    [HttpPost("status")]
    public async Task<IActionResult> HandleStatusCallback([FromForm] TwilioSmsStatusWebhook webhook, CancellationToken cancellationToken)
    {
        _logger.LogInformation("SMS status update: {MessageSid} -> {Status}", webhook.MessageSid, webhook.MessageStatus);

        var result = await _smsService.UpdateMessageStatusAsync(webhook.MessageSid, webhook.MessageStatus, cancellationToken);

        if (!result.IsSuccess)
        {
            _logger.LogWarning("Failed to update SMS status for {MessageSid}: {Errors}", webhook.MessageSid, string.Join(", ", result.Errors));
            return BadRequest();
        }

        return Ok();
    }
}

public record TwilioSmsWebhook
{
    public string From { get; init; } = string.Empty;
    public string To { get; init; } = string.Empty;
    public string? Body { get; init; }
    public string MessageSid { get; init; } = string.Empty;
    public int? NumMedia { get; init; }
}

public record TwilioSmsStatusWebhook
{
    public string MessageSid { get; init; } = string.Empty;
    public string MessageStatus { get; init; } = string.Empty;
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }
}
