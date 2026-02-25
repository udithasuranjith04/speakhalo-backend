using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SpeakHalo.Application.DTOs.Payment;
using SpeakHalo.Application.Interfaces;
using SpeakHalo.Domain.Entities;
using SpeakHalo.Domain.Interfaces.Repositories;

namespace SpeakHalo.API.Controllers;

[AllowAnonymous]
[ApiController]
[Route("api/v1/webhooks/paddle")]
public class PaddleWebhookController : ControllerBase
{
    private readonly IPaddleWebhookService _webhookService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAdminNotificationService _adminNotifications;
    private readonly PaddleSettings _paddleSettings;
    private readonly ILogger<PaddleWebhookController> _logger;

    public PaddleWebhookController(
        IPaddleWebhookService webhookService,
        IUnitOfWork unitOfWork,
        IAdminNotificationService adminNotifications,
        IOptions<PaddleSettings> paddleSettings,
        ILogger<PaddleWebhookController> logger)
    {
        _webhookService = webhookService;
        _unitOfWork = unitOfWork;
        _adminNotifications = adminNotifications;
        _paddleSettings = paddleSettings.Value;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> HandleWebhook(CancellationToken cancellationToken)
    {
        // Read raw body for signature verification
        string rawBody;
        using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
        {
            rawBody = await reader.ReadToEndAsync(cancellationToken);
        }

        // Verify signature if webhook secret is configured
        if (!string.IsNullOrEmpty(_paddleSettings.WebhookSecret))
        {
            var signatureHeader = Request.Headers["Paddle-Signature"].FirstOrDefault();
            if (string.IsNullOrEmpty(signatureHeader) || !VerifySignature(rawBody, signatureHeader))
            {
                _logger.LogWarning("Paddle webhook signature verification failed");
                return Ok(); // Return 200 to avoid retries for invalid signatures
            }
        }

        JsonElement payload;
        try
        {
            payload = JsonSerializer.Deserialize<JsonElement>(rawBody);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse Paddle webhook payload");
            return Ok();
        }

        var eventType = payload.TryGetProperty("event_type", out var et) ? et.GetString() : null;
        var eventId = payload.TryGetProperty("event_id", out var eid) ? eid.GetString() : null;

        if (string.IsNullOrEmpty(eventType) || string.IsNullOrEmpty(eventId))
        {
            _logger.LogWarning("Paddle webhook missing event_type or event_id");
            return Ok();
        }

        // Idempotency check
        var existing = await _unitOfWork.Repository<WebhookEvent>()
            .FindAsync(e => e.EventId == eventId && e.Source == "paddle", cancellationToken);
        if (existing.Any())
        {
            _logger.LogInformation("Paddle webhook {EventId} already processed, skipping", eventId);
            return Ok();
        }

        // Record webhook event
        var webhookEvent = new WebhookEvent
        {
            EventId = eventId,
            EventName = eventType,
            Source = "paddle",
            ProcessedAt = DateTime.UtcNow,
            RawPayload = rawBody,
            Success = false
        };

        try
        {
            if (!payload.TryGetProperty("data", out var data))
            {
                _logger.LogWarning("Paddle webhook {EventId} missing data property", eventId);
                webhookEvent.Success = false;
                webhookEvent.ErrorMessage = "Missing data property";
                await SaveWebhookEvent(webhookEvent, cancellationToken);
                return Ok();
            }

            switch (eventType)
            {
                case "subscription.created":
                    await _webhookService.HandleSubscriptionCreatedAsync(data, cancellationToken);
                    break;
                case "subscription.updated":
                    await _webhookService.HandleSubscriptionUpdatedAsync(data, cancellationToken);
                    break;
                case "subscription.canceled":
                    await _webhookService.HandleSubscriptionCanceledAsync(data, cancellationToken);
                    break;
                case "transaction.completed":
                    await _webhookService.HandleTransactionCompletedAsync(data, cancellationToken);
                    break;
                case "transaction.payment_failed":
                    await _webhookService.HandleTransactionPaymentFailedAsync(data, cancellationToken);
                    break;
                default:
                    _logger.LogInformation("Unhandled Paddle webhook event type: {EventType}", eventType);
                    break;
            }

            webhookEvent.Success = true;

            // Send real-time admin notifications for key payment events
            _ = Task.Run(async () =>
            {
                try
                {
                    var customerId = data.TryGetProperty("customer_id", out var cid) ? cid.GetString() : null;
                    var email = customerId ?? "Unknown";

                    // Try to resolve email from custom_data
                    if (data.TryGetProperty("custom_data", out var customData) &&
                        customData.TryGetProperty("user_id", out var uid))
                    {
                        var userId = uid.GetString();
                        if (!string.IsNullOrEmpty(userId))
                        {
                            var u = await _unitOfWork.Users.GetByIdAsync(userId);
                            if (u != null) email = u.Email;
                        }
                    }

                    switch (eventType)
                    {
                        case "subscription.created":
                            await _adminNotifications.NotifySubscriptionChangeAsync("", email, "none", "new subscription");
                            break;
                        case "subscription.canceled":
                            await _adminNotifications.NotifySubscriptionChangeAsync("", email, "active", "canceled");
                            break;
                        case "transaction.payment_failed":
                            var reason = data.TryGetProperty("status", out var st) ? st.GetString() : null;
                            await _adminNotifications.NotifyPaymentFailedAsync("", email, reason);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send admin notification for Paddle webhook {EventType}", eventType);
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Paddle webhook {EventType} {EventId}", eventType, eventId);
            webhookEvent.Success = false;
            webhookEvent.ErrorMessage = ex.Message;
        }

        await SaveWebhookEvent(webhookEvent, cancellationToken);

        return Ok();
    }

    private async Task SaveWebhookEvent(WebhookEvent webhookEvent, CancellationToken cancellationToken)
    {
        try
        {
            await _unitOfWork.Repository<WebhookEvent>().AddAsync(webhookEvent, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save webhook event {EventId}", webhookEvent.EventId);
        }
    }

    private bool VerifySignature(string rawBody, string signatureHeader)
    {
        try
        {
            // Paddle signature format: ts=timestamp;h1=hash
            var parts = signatureHeader.Split(';');
            string? timestamp = null;
            string? hash = null;

            foreach (var part in parts)
            {
                if (part.StartsWith("ts="))
                    timestamp = part[3..];
                else if (part.StartsWith("h1="))
                    hash = part[3..];
            }

            if (string.IsNullOrEmpty(timestamp) || string.IsNullOrEmpty(hash))
                return false;

            var signedPayload = $"{timestamp}:{rawBody}";
            var keyBytes = Encoding.UTF8.GetBytes(_paddleSettings.WebhookSecret);
            var payloadBytes = Encoding.UTF8.GetBytes(signedPayload);

            using var hmac = new HMACSHA256(keyBytes);
            var computedHash = hmac.ComputeHash(payloadBytes);
            var computedHashHex = Convert.ToHexString(computedHash).ToLowerInvariant();

            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(computedHashHex),
                Encoding.UTF8.GetBytes(hash.ToLowerInvariant()));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error verifying Paddle webhook signature");
            return false;
        }
    }
}
