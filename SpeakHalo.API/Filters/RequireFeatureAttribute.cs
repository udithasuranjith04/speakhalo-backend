using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Distributed;
using SpeakHalo.Application.Interfaces;
using SpeakHalo.Domain.Entities;
using SpeakHalo.Domain.Enums;
using SpeakHalo.Domain.Interfaces.Repositories;
using System.Security.Claims;
using System.Text;

namespace SpeakHalo.API.Filters;

/// <summary>
/// Action filter that checks if the user has an active subscription.
/// All features are available to all active users — admin controls access
/// by activating/deactivating users and setting usage limits.
/// Caches the subscription status for 5 minutes to avoid DB hits on every request.
/// Returns 403 if the user's subscription is not active or is suspended.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class RequireFeatureAttribute : Attribute, IAsyncActionFilter
{
    private readonly PlanFeature _requiredFeature;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public RequireFeatureAttribute(PlanFeature requiredFeature)
    {
        _requiredFeature = requiredFeature;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // Skip feature check for [AllowAnonymous] endpoints (e.g., webhooks)
        var endpoint = context.HttpContext.GetEndpoint();
        if (endpoint?.Metadata.GetMetadata<Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute>() != null)
        {
            await next();
            return;
        }

        var userId = context.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var cache = context.HttpContext.RequestServices.GetRequiredService<IDistributedCache>();

        // Try to get cached subscription status
        var cacheKey = $"user_active:{userId}";
        string? cachedStatus = null;

        try
        {
            var cached = await cache.GetAsync(cacheKey);
            if (cached != null)
            {
                cachedStatus = Encoding.UTF8.GetString(cached);
            }
        }
        catch (Exception ex)
        {
            var logger = context.HttpContext.RequestServices.GetService<ILogger<RequireFeatureAttribute>>();
            logger?.LogError(ex, "Failed to read subscription status cache for user {UserId}", userId);
        }

        bool isActive;

        if (cachedStatus != null)
        {
            isActive = cachedStatus == "active";
        }
        else
        {
            var unitOfWork = context.HttpContext.RequestServices.GetRequiredService<IUnitOfWork>();
            var subscriptions = await unitOfWork.Repository<UserSubscription>()
                .FindAsync(s => s.UserId == userId);
            var subscription = subscriptions.FirstOrDefault();

            isActive = subscription != null
                       && subscription.HasActivePayment
                       && !subscription.IsTemporarilySuspended;

            // Cache the status
            try
            {
                var statusValue = isActive ? "active" : "inactive";
                await cache.SetAsync(cacheKey,
                    Encoding.UTF8.GetBytes(statusValue),
                    new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = CacheDuration });
            }
            catch (Exception ex)
            {
                var logger = context.HttpContext.RequestServices.GetService<ILogger<RequireFeatureAttribute>>();
                logger?.LogError(ex, "Failed to write subscription status cache for user {UserId}", userId);
            }
        }

        if (!isActive)
        {
            context.Result = new ObjectResult(new
            {
                error = "Account not active",
                message = "Your account is not currently active. Please contact your administrator.",
                feature = _requiredFeature.ToString()
            })
            {
                StatusCode = 403
            };
            return;
        }

        await next();
    }
}
