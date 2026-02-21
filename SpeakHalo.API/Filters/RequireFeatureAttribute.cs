using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Distributed;
using SpeakHalo.Domain.Entities;
using SpeakHalo.Domain.Enums;
using SpeakHalo.Domain.Interfaces.Repositories;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace SpeakHalo.API.Filters;

/// <summary>
/// Action filter that checks if the user has an active subscription AND
/// if the specific feature is enabled in their EnabledFeatures JSON.
/// Caches subscription state for 5 minutes to reduce DB hits.
/// Returns 403 if the subscription is inactive or the feature is disabled.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class RequireFeatureAttribute : Attribute, IAsyncActionFilter
{
    private readonly PlanFeature _requiredFeature;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    private static readonly Dictionary<PlanFeature, string> FeatureJsonKeys = new()
    {
        { PlanFeature.VoiceAI, "voiceAI" },
        { PlanFeature.WhatsApp, "whatsApp" },
        { PlanFeature.SMS, "sms" },
        { PlanFeature.AIPhotoQuotes, "aiPhotoQuotes" },
        { PlanFeature.AIFollowUps, "aiFollowUps" },
        { PlanFeature.AdvancedAnalytics, "advancedAnalytics" },
        { PlanFeature.SentimentAnalysis, "sentimentAnalysis" },
        { PlanFeature.MultiplePhoneNumbers, "multiplePhoneNumbers" },
        { PlanFeature.PrioritySupport, "prioritySupport" },
        { PlanFeature.MultipleLocations, "multipleLocations" },
        { PlanFeature.BasicCRM, "basicCRM" },
        { PlanFeature.Calendar, "calendar" },
        { PlanFeature.CallTranscription, "callTranscription" },
        { PlanFeature.KnowledgeBase, "knowledgeBase" },
        { PlanFeature.CustomBranding, "customBranding" },
        { PlanFeature.APIAccess, "apiAccess" },
        { PlanFeature.TeamMembers, "teamMembers" },
    };

    public RequireFeatureAttribute(PlanFeature requiredFeature)
    {
        _requiredFeature = requiredFeature;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
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
        var cacheKey = $"user_sub:{userId}";

        bool isActive = false;
        string? enabledFeaturesJson = null;
        bool loadedFromCache = false;

        try
        {
            var cachedBytes = await cache.GetAsync(cacheKey);
            if (cachedBytes != null)
            {
                var cachedStr = Encoding.UTF8.GetString(cachedBytes);
                using var doc = JsonDocument.Parse(cachedStr);
                isActive = doc.RootElement.GetProperty("active").GetBoolean();
                if (doc.RootElement.TryGetProperty("features", out var featProp)
                    && featProp.ValueKind == JsonValueKind.String)
                {
                    enabledFeaturesJson = featProp.GetString();
                }
                loadedFromCache = true;
            }
        }
        catch (Exception ex)
        {
            var logger = context.HttpContext.RequestServices.GetService<ILogger<RequireFeatureAttribute>>();
            logger?.LogError(ex, "Failed to read subscription cache for user {UserId}", userId);
        }

        if (!loadedFromCache)
        {
            var unitOfWork = context.HttpContext.RequestServices.GetRequiredService<IUnitOfWork>();
            var subscriptions = await unitOfWork.Repository<UserSubscription>()
                .FindAsync(s => s.UserId == userId);
            var subscription = subscriptions.FirstOrDefault();

            isActive = subscription != null
                       && subscription.HasActivePayment
                       && !subscription.IsTemporarilySuspended;
            enabledFeaturesJson = subscription?.EnabledFeatures;

            try
            {
                var cacheValue = JsonSerializer.Serialize(new { active = isActive, features = enabledFeaturesJson });
                await cache.SetAsync(cacheKey,
                    Encoding.UTF8.GetBytes(cacheValue),
                    new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = CacheDuration });
            }
            catch (Exception ex)
            {
                var logger = context.HttpContext.RequestServices.GetService<ILogger<RequireFeatureAttribute>>();
                logger?.LogError(ex, "Failed to write subscription cache for user {UserId}", userId);
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
            { StatusCode = 403 };
            return;
        }

        if (!IsFeatureEnabled(enabledFeaturesJson))
        {
            context.Result = new ObjectResult(new
            {
                error = "Feature not available",
                message = $"The {_requiredFeature} feature is not enabled for your account. Please contact your administrator.",
                feature = _requiredFeature.ToString()
            })
            { StatusCode = 403 };
            return;
        }

        await next();
    }

    private bool IsFeatureEnabled(string? enabledFeaturesJson)
    {
        if (string.IsNullOrWhiteSpace(enabledFeaturesJson))
            return true;

        if (!FeatureJsonKeys.TryGetValue(_requiredFeature, out var jsonKey))
            return true;

        try
        {
            using var doc = JsonDocument.Parse(enabledFeaturesJson);
            if (doc.RootElement.TryGetProperty(jsonKey, out var value)
                && value.ValueKind == JsonValueKind.False)
            {
                return false;
            }
            return true;
        }
        catch
        {
            return true;
        }
    }
}
