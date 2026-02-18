using SpeakHalo.Application.Services;
using SpeakHalo.Domain.Enums;
using System.Security.Claims;

namespace SpeakHalo.API.Middleware;

public class CookieConsentMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CookieConsentMiddleware> _logger;
    private readonly CookieConsentMiddlewareOptions _options;

    public CookieConsentMiddleware(
        RequestDelegate next,
        ILogger<CookieConsentMiddleware> logger,
        CookieConsentMiddlewareOptions options)
    {
        _next = next;
        _logger = logger;
        _options = options;
    }

    public async Task InvokeAsync(HttpContext context, CookieManagementService cookieService)
    {
        if (!_options.Enabled)
        {
            await _next(context);
            return;
        }

        var path = context.Request.Path.Value?.ToLower() ?? "";
        if (_options.ExcludedPaths.Any(p => path.StartsWith(p.ToLower())))
        {
            await _next(context);
            return;
        }

        var requiredConsent = GetRequiredConsentForPath(path);

        if (requiredConsent == null)
        {
            await _next(context);
            return;
        }

        try
        {
            var userId = context.User.Identity?.IsAuthenticated == true
                ? context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                : null;

            var hasConsent = await cookieService.HasConsentAsync(userId, requiredConsent.Value);

            if (!hasConsent)
            {
                _logger.LogWarning(
                    "Consent required: {Category} for path {Path}. User: {UserId}, IP: {IP}",
                    requiredConsent.Value,
                    path,
                    userId ?? "anonymous",
                    context.Connection.RemoteIpAddress?.ToString());

                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json";

                var response = new
                {
                    error = "Consent Required",
                    message = $"This feature requires consent to {requiredConsent.Value} cookies.",
                    requiredConsent = requiredConsent.Value.ToString(),
                    consentUrl = "/api/v1/cookies/consent"
                };

                await context.Response.WriteAsJsonAsync(response);
                return;
            }

            if (_options.AddConsentHeaders)
            {
                context.Response.Headers.Append("X-Consent-Category", requiredConsent.Value.ToString());
                context.Response.Headers.Append("X-Consent-Status", "granted");
            }

            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking cookie consent for path {Path}", path);

            // Fail open: allow the request through on consent-check errors
            await _next(context);
        }
    }

    private CookieCategory? GetRequiredConsentForPath(string path)
    {
        foreach (var rule in _options.PathConsentRules)
        {
            if (path.StartsWith(rule.Key.ToLower()))
            {
                return rule.Value;
            }
        }

        if (_options.UseDefaultRules)
        {
            if (path.Contains("/analytics") || path.Contains("/metrics"))
                return CookieCategory.Analytics;

            if (path.Contains("/campaigns") || path.Contains("/marketing"))
                return CookieCategory.Marketing;

            if (path.Contains("/performance") || path.Contains("/monitoring"))
                return CookieCategory.Performance;

            if (path.Contains("/social") || path.Contains("/share"))
                return CookieCategory.SocialMedia;
        }

        return null;
    }
}

/// <summary>
/// Configuration options for Cookie Consent Middleware
/// </summary>
public class CookieConsentMiddlewareOptions
{
    /// <summary>
    /// Enable or disable the middleware
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Add consent status headers to responses (for debugging)
    /// </summary>
    public bool AddConsentHeaders { get; set; } = false;

    /// <summary>
    /// Use default path-based consent rules
    /// </summary>
    public bool UseDefaultRules { get; set; } = true;

    /// <summary>
    /// Paths that should be excluded from consent checking
    /// </summary>
    public List<string> ExcludedPaths { get; set; } = new()
    {
        "/api/v1/auth",
        "/api/v1/cookies",
        "/health",
        "/swagger",
        "/hangfire",
        "/_framework"
    };

    /// <summary>
    /// Custom path-to-consent mappings
    /// Key: Path prefix, Value: Required consent category
    /// </summary>
    public Dictionary<string, CookieCategory> PathConsentRules { get; set; } = new();

    /// <summary>
    /// Action to take when consent is not granted (Block or Log)
    /// </summary>
    public ConsentEnforcementMode EnforcementMode { get; set; } = ConsentEnforcementMode.Block;
}

/// <summary>
/// Enforcement mode for consent violations
/// </summary>
public enum ConsentEnforcementMode
{
    /// <summary>
    /// Block the request with 403 Forbidden
    /// </summary>
    Block,

    /// <summary>
    /// Log the violation but allow the request
    /// </summary>
    LogOnly,

    /// <summary>
    /// Warn in response header but allow the request
    /// </summary>
    Warn
}

/// <summary>
/// Extension methods for adding Cookie Consent Middleware
/// </summary>
public static class CookieConsentMiddlewareExtensions
{
    /// <summary>
    /// Add Cookie Consent Middleware to the pipeline
    /// </summary>
    public static IApplicationBuilder UseCookieConsentValidation(
        this IApplicationBuilder builder,
        Action<CookieConsentMiddlewareOptions>? configureOptions = null)
    {
        var options = new CookieConsentMiddlewareOptions();
        configureOptions?.Invoke(options);

        return builder.UseMiddleware<CookieConsentMiddleware>(options);
    }

    /// <summary>
    /// Add Cookie Consent Middleware with default settings
    /// </summary>
    public static IApplicationBuilder UseCookieConsentValidation(this IApplicationBuilder builder)
    {
        return builder.UseCookieConsentValidation(options => { });
    }
}
