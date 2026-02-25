using System.Text.Json;
using Microsoft.Extensions.Logging;
using SpeakHalo.Application.DTOs;
using SpeakHalo.Application.Interfaces;
using SpeakHalo.Domain.Entities;
using SpeakHalo.Domain.Interfaces.Repositories;

namespace SpeakHalo.Application.Services;

public class PaddleWebhookService : IPaddleWebhookService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationService _notificationService;
    private readonly ILogger<PaddleWebhookService> _logger;

    public PaddleWebhookService(
        IUnitOfWork unitOfWork,
        INotificationService notificationService,
        ILogger<PaddleWebhookService> logger)
    {
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task HandleSubscriptionCreatedAsync(JsonElement data, CancellationToken cancellationToken = default)
    {
        var subscriptionId = data.GetProperty("id").GetString()!;
        var customerId = data.GetProperty("customer_id").GetString()!;
        var status = data.GetProperty("status").GetString() ?? "active";

        string? priceId = null;
        if (data.TryGetProperty("items", out var items) && items.GetArrayLength() > 0)
        {
            var firstItem = items[0];
            if (firstItem.TryGetProperty("price", out var price))
                priceId = price.GetProperty("id").GetString();
        }

        // Find the user subscription by PaddleCustomerId
        var subs = await _unitOfWork.Repository<UserSubscription>()
            .FindAsync(s => s.PaddleCustomerId == customerId, cancellationToken);
        var sub = subs.FirstOrDefault();

        if (sub == null)
        {
            _logger.LogWarning("No subscription found for Paddle customer {CustomerId}", customerId);
            return;
        }

        sub.PaddleSubscriptionId = subscriptionId;
        sub.PaddleSubscriptionStatus = status;
        sub.HasActivePayment = true;
        sub.RequiresPayment = false;
        sub.IsOnTrial = false;

        if (data.TryGetProperty("created_at", out var createdAt))
            sub.PaddleSubscriptionCreatedAt = createdAt.GetDateTime();

        if (data.TryGetProperty("current_billing_period", out var period))
        {
            if (period.TryGetProperty("ends_at", out var endsAt))
                sub.PaddleCurrentPeriodEnd = endsAt.GetDateTime();
        }

        if (data.TryGetProperty("management_urls", out var urls))
        {
            if (urls.TryGetProperty("update_payment_method", out var updateUrl))
                sub.PaddleUpdatePaymentMethodUrl = updateUrl.GetString();
            if (urls.TryGetProperty("cancel", out var cancelUrl))
                sub.PaddleCancelUrl = cancelUrl.GetString();
        }

        if (!string.IsNullOrEmpty(priceId))
        {
            sub.PaddlePriceId = priceId;
            // Sync plan limits from the matching PricingPlan
            var plans = await _unitOfWork.Repository<PricingPlan>()
                .FindAsync(p => p.PaddlePriceId == priceId && p.IsActive && !p.IsDeleted, cancellationToken);
            var plan = plans.FirstOrDefault();
            if (plan != null)
            {
                sub.PricingPlanId = plan.Id;
                sub.Plan = plan.TierSlug ?? plan.Name.ToLower();
                sub.AIMinutesIncluded = plan.AIMinutesIncluded;
                sub.SMSMessagesIncluded = plan.SMSMessagesIncluded;
                sub.WhatsAppMessagesIncluded = plan.WhatsAppMessagesIncluded;
                sub.ImagesIncluded = plan.ImagesIncluded;
                sub.MaxPhoneNumbers = plan.MaxPhoneNumbers;
                sub.AIOverageRate = plan.AIOverageRate;
                sub.SMSOverageRate = plan.SMSOverageRate;
                sub.WhatsAppOverageRate = plan.WhatsAppOverageRate;
                sub.ImageOverageRate = plan.ImageOverageRate;
                sub.EnabledFeatures = plan.EnabledFeatures;
                sub.EnforceHardLimits = plan.EnforceHardLimits;
            }
        }

        _unitOfWork.Repository<UserSubscription>().Update(sub);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Subscription created for user {UserId}, Paddle subscription {SubscriptionId}",
            sub.UserId, subscriptionId);
    }

    public async Task HandleSubscriptionUpdatedAsync(JsonElement data, CancellationToken cancellationToken = default)
    {
        var subscriptionId = data.GetProperty("id").GetString()!;
        var status = data.GetProperty("status").GetString() ?? "active";

        var subs = await _unitOfWork.Repository<UserSubscription>()
            .FindAsync(s => s.PaddleSubscriptionId == subscriptionId, cancellationToken);
        var sub = subs.FirstOrDefault();

        if (sub == null)
        {
            _logger.LogWarning("No subscription found for Paddle subscription {SubscriptionId}", subscriptionId);
            return;
        }

        sub.PaddleSubscriptionStatus = status;

        if (data.TryGetProperty("current_billing_period", out var period))
        {
            if (period.TryGetProperty("ends_at", out var endsAt))
                sub.PaddleCurrentPeriodEnd = endsAt.GetDateTime();
            if (period.TryGetProperty("starts_at", out var startsAt))
                sub.BillingCycleStartDate = startsAt.GetDateTime();
        }

        if (data.TryGetProperty("management_urls", out var urls))
        {
            if (urls.TryGetProperty("update_payment_method", out var updateUrl))
                sub.PaddleUpdatePaymentMethodUrl = updateUrl.GetString();
            if (urls.TryGetProperty("cancel", out var cancelUrl))
                sub.PaddleCancelUrl = cancelUrl.GetString();
        }

        // Handle plan changes
        if (data.TryGetProperty("items", out var items) && items.GetArrayLength() > 0)
        {
            var firstItem = items[0];
            if (firstItem.TryGetProperty("price", out var price))
            {
                var newPriceId = price.GetProperty("id").GetString();
                if (!string.IsNullOrEmpty(newPriceId) && newPriceId != sub.PaddlePriceId)
                {
                    sub.PaddlePriceId = newPriceId;
                    var plans = await _unitOfWork.Repository<PricingPlan>()
                        .FindAsync(p => p.PaddlePriceId == newPriceId && !p.IsDeleted, cancellationToken);
                    var plan = plans.FirstOrDefault();
                    if (plan != null)
                    {
                        sub.PricingPlanId = plan.Id;
                        sub.Plan = plan.TierSlug ?? plan.Name.ToLower();
                        sub.AIMinutesIncluded = plan.AIMinutesIncluded;
                        sub.SMSMessagesIncluded = plan.SMSMessagesIncluded;
                        sub.WhatsAppMessagesIncluded = plan.WhatsAppMessagesIncluded;
                        sub.ImagesIncluded = plan.ImagesIncluded;
                        sub.MaxPhoneNumbers = plan.MaxPhoneNumbers;
                        sub.AIOverageRate = plan.AIOverageRate;
                        sub.SMSOverageRate = plan.SMSOverageRate;
                        sub.WhatsAppOverageRate = plan.WhatsAppOverageRate;
                        sub.ImageOverageRate = plan.ImageOverageRate;
                        sub.EnabledFeatures = plan.EnabledFeatures;
                    }
                }
            }
        }

        _unitOfWork.Repository<UserSubscription>().Update(sub);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Subscription updated for user {UserId}, status={Status}", sub.UserId, status);
    }

    public async Task HandleSubscriptionCanceledAsync(JsonElement data, CancellationToken cancellationToken = default)
    {
        var subscriptionId = data.GetProperty("id").GetString()!;

        var subs = await _unitOfWork.Repository<UserSubscription>()
            .FindAsync(s => s.PaddleSubscriptionId == subscriptionId, cancellationToken);
        var sub = subs.FirstOrDefault();

        if (sub == null)
        {
            _logger.LogWarning("No subscription found for Paddle subscription {SubscriptionId}", subscriptionId);
            return;
        }

        sub.PaddleSubscriptionStatus = "canceled";
        sub.PaddleCanceledAt = DateTime.UtcNow;

        if (data.TryGetProperty("canceled_at", out var canceledAt))
            sub.PaddleCanceledAt = canceledAt.GetDateTime();

        // If the subscription ends at period end, keep active until then
        if (data.TryGetProperty("current_billing_period", out var period) &&
            period.TryGetProperty("ends_at", out var endsAt))
        {
            sub.PaddleCurrentPeriodEnd = endsAt.GetDateTime();
            sub.NextBillingDate = endsAt.GetDateTime();
        }
        else
        {
            // Immediate cancellation
            sub.HasActivePayment = false;
        }

        _unitOfWork.Repository<UserSubscription>().Update(sub);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Send notification
        try
        {
            await _notificationService.CreateNotificationAsync(new CreateNotificationRequest
                {
                    UserId = sub.UserId,
                    Title = "Subscription Canceled",
                    Message = "Your subscription has been canceled. Access will continue until the end of your billing period.",
                    Type = "billing"
                }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send cancellation notification to user {UserId}", sub.UserId);
        }

        _logger.LogInformation("Subscription canceled for user {UserId}", sub.UserId);
    }

    public async Task HandleTransactionCompletedAsync(JsonElement data, CancellationToken cancellationToken = default)
    {
        string? customerId = null;
        if (data.TryGetProperty("customer_id", out var cidEl))
            customerId = cidEl.GetString();

        string? subscriptionId = null;
        if (data.TryGetProperty("subscription_id", out var subIdEl))
            subscriptionId = subIdEl.GetString();

        if (string.IsNullOrEmpty(customerId) && string.IsNullOrEmpty(subscriptionId))
        {
            _logger.LogWarning("Transaction completed but no customer_id or subscription_id found");
            return;
        }

        IEnumerable<UserSubscription> subs;
        if (!string.IsNullOrEmpty(subscriptionId))
        {
            subs = await _unitOfWork.Repository<UserSubscription>()
                .FindAsync(s => s.PaddleSubscriptionId == subscriptionId, cancellationToken);
        }
        else
        {
            subs = await _unitOfWork.Repository<UserSubscription>()
                .FindAsync(s => s.PaddleCustomerId == customerId, cancellationToken);
        }

        var sub = subs.FirstOrDefault();
        if (sub == null)
        {
            _logger.LogWarning("No subscription found for transaction completed event");
            return;
        }

        sub.HasActivePayment = true;
        sub.RequiresPayment = false;

        // Set first payment date if not already set
        if (!sub.FirstPaymentAt.HasValue)
            sub.FirstPaymentAt = DateTime.UtcNow;

        _unitOfWork.Repository<UserSubscription>().Update(sub);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Transaction completed for user {UserId}", sub.UserId);
    }

    public async Task HandleTransactionPaymentFailedAsync(JsonElement data, CancellationToken cancellationToken = default)
    {
        string? subscriptionId = null;
        if (data.TryGetProperty("subscription_id", out var subIdEl))
            subscriptionId = subIdEl.GetString();

        string? customerId = null;
        if (data.TryGetProperty("customer_id", out var cidEl))
            customerId = cidEl.GetString();

        IEnumerable<UserSubscription> subs;
        if (!string.IsNullOrEmpty(subscriptionId))
        {
            subs = await _unitOfWork.Repository<UserSubscription>()
                .FindAsync(s => s.PaddleSubscriptionId == subscriptionId, cancellationToken);
        }
        else if (!string.IsNullOrEmpty(customerId))
        {
            subs = await _unitOfWork.Repository<UserSubscription>()
                .FindAsync(s => s.PaddleCustomerId == customerId, cancellationToken);
        }
        else
        {
            return;
        }

        var sub = subs.FirstOrDefault();
        if (sub == null) return;

        // Send notification to user about payment failure
        try
        {
            await _notificationService.CreateNotificationAsync(new CreateNotificationRequest
                {
                    UserId = sub.UserId,
                    Title = "Payment Failed",
                    Message = "Your payment could not be processed. Please update your payment method to avoid service interruption.",
                    Type = "billing"
                }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send payment failure notification to user {UserId}", sub.UserId);
        }

        _logger.LogWarning("Payment failed for user {UserId}", sub.UserId);
    }
}
