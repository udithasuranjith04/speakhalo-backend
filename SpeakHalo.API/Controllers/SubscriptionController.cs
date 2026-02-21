using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpeakHalo.Domain.Entities;
using SpeakHalo.Domain.Interfaces.Repositories;
using System.Security.Claims;
using System.Text.Json;

namespace SpeakHalo.API.Controllers;

/// <summary>
/// Endpoints for the current user's subscription info (plan, features, limits).
/// </summary>
[Authorize]
[ApiController]
[Route("api/v1/subscriptions")]
public class SubscriptionController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public SubscriptionController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Returns the current user's subscription including enabled features, limits, and usage.
    /// </summary>
    [HttpGet("current")]
    public async Task<IActionResult> GetCurrentSubscription(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var subscriptions = await _unitOfWork.Repository<UserSubscription>()
            .FindAsync(s => s.UserId == userId, cancellationToken);
        var sub = subscriptions.FirstOrDefault();

        if (sub == null)
            return NotFound(new { error = "No subscription found" });

        Dictionary<string, bool>? enabledFeatures = null;
        if (!string.IsNullOrWhiteSpace(sub.EnabledFeatures))
        {
            try
            {
                enabledFeatures = JsonSerializer.Deserialize<Dictionary<string, bool>>(sub.EnabledFeatures);
            }
            catch { /* malformed JSON, return null */ }
        }

        return Ok(new
        {
            plan = sub.Plan,
            pricingPlanId = sub.PricingPlanId,
            enabledFeatures,
            hasActivePayment = sub.HasActivePayment,
            isTemporarilySuspended = sub.IsTemporarilySuspended,
            isOnTrial = sub.IsOnTrial,
            trialEndsAt = sub.TrialEndsAt,
            limits = new
            {
                aiMinutesIncluded = sub.AIMinutesIncluded,
                whatsAppMessagesIncluded = sub.WhatsAppMessagesIncluded,
                smsMessagesIncluded = sub.SMSMessagesIncluded,
                imagesIncluded = sub.ImagesIncluded,
                enforceHardLimits = sub.EnforceHardLimits,
            },
            usage = new
            {
                aiMinutesUsed = sub.AIMinutesUsedThisMonth,
                whatsAppMessagesUsed = sub.WhatsAppMessagesUsedThisMonth,
                smsMessagesUsed = sub.SMSMessagesUsedThisMonth,
                imagesUsed = sub.ImagesUsedThisMonth,
            },
            billingCycleStartDate = sub.BillingCycleStartDate,
            nextBillingDate = sub.NextBillingDate,
        });
    }
}
