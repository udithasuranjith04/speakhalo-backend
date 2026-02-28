using System.Collections.Concurrent;
using System.Net;
using System.Security.Claims;
using Microsoft.Extensions.Caching.Distributed;

namespace SpeakHalo.API.Middleware;

public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    private readonly IDistributedCache _cache;
    private static readonly ConcurrentDictionary<string, RateLimitBucket> _requests = new();
    private static readonly Timer _cleanupTimer;

    // Rate limit configuration
    private const int AnonymousRequestsPerMinute = 20;
    private const int AuthenticatedRequestsPerMinute = 100;
    private const int WebhookRequestsPerMinute = 300;
    private const int AdminRequestsPerMinute = 500;

    // Endpoint-specific limits (per minute per client)
    private static readonly Dictionary<string, int> EndpointLimits = new()
    {
        { "/api/v1/auth/login", 5 },                       // Login attempts
        { "/api/v1/auth/force-change-password", 3 },       // Password change (token-gated, but limit brute force)
        { "/api/v1/auth/forgot-password", 3 },             // Forgot password (anti-spam)
        { "/api/v1/auth/reset-password", 5 },              // Reset password with token
        { "/api/v1/auth/refresh", 10 },                    // Token refresh
        { "/api/v1/auth/revoke-token", 5 },                // Token revocation
        { "/api/v1/admin/trials/request", 2 },             // Trial requests
        { "/api/v1/subscriptions/trial-invite/validate", 10 }, // Trial invite validation (public endpoint)
    };

    static RateLimitingMiddleware()
    {
        // Cleanup old entries every 5 minutes
        _cleanupTimer = new Timer(CleanupOldEntries, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
    }

    private readonly IHostEnvironment _environment;

    public RateLimitingMiddleware(
        RequestDelegate next,
        ILogger<RateLimitingMiddleware> logger,
        IDistributedCache cache,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _cache = cache;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip rate limiting entirely in Testing environment
        if (_environment.EnvironmentName == "Testing")
        {
            await _next(context);
            return;
        }

        // Skip rate limiting for health checks, Hangfire dashboard, and CORS preflight
        if (context.Request.Path.StartsWithSegments("/health") ||
            context.Request.Path.StartsWithSegments("/hangfire") ||
            context.Request.Method == "OPTIONS")
        {
            await _next(context);
            return;
        }

        var clientId = GetClientIdentifier(context);
        var endpoint = GetEndpointKey(context);
        var limit = GetRateLimit(context, endpoint);
        var windowSeconds = 60;

        var (isExceeded, remaining, resetTime) = await CheckRateLimitAsync(clientId, endpoint, limit, windowSeconds);

        // Add rate limit headers
        context.Response.Headers.Append("X-RateLimit-Limit", limit.ToString());
        context.Response.Headers.Append("X-RateLimit-Remaining", Math.Max(0, remaining).ToString());
        context.Response.Headers.Append("X-RateLimit-Reset", resetTime.ToString("o"));

        if (isExceeded)
        {
            _logger.LogWarning(
                "Rate limit exceeded for {ClientId} on {Endpoint}. Limit: {Limit}/min",
                clientId, endpoint, limit);

            context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
            context.Response.Headers.Append("Retry-After", "60");

            await context.Response.WriteAsJsonAsync(new
            {
                error = "Rate limit exceeded",
                message = $"Too many requests. Please try again in {(resetTime - DateTime.UtcNow).TotalSeconds:F0} seconds.",
                limit = limit,
                remaining = 0,
                resetAt = resetTime
            });
            return;
        }

        await _next(context);
    }

    private static string GetClientIdentifier(HttpContext context)
    {
        // Priority: 1. User ID (authenticated), 2. API Key, 3. IP Address
        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(userId))
            return $"user:{userId}";

        var apiKey = context.Request.Headers["X-API-Key"].FirstOrDefault();
        if (!string.IsNullOrEmpty(apiKey))
            return $"apikey:{apiKey}";

        var ipAddress = GetTrustedClientIpAddress(context);
        return $"ip:{ipAddress}";
    }

    private static string GetTrustedClientIpAddress(HttpContext context)
    {
        // Cloudflare: CF-Connecting-IP is the most reliable real client IP
        // This header is set by Cloudflare and cannot be spoofed by the client
        var cfConnectingIp = context.Request.Headers["CF-Connecting-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(cfConnectingIp) && IPAddress.TryParse(cfConnectingIp.Trim(), out _))
        {
            return cfConnectingIp.Trim();
        }

        var remoteIp = context.Connection.RemoteIpAddress;

        // Only trust X-Forwarded-For if we're behind a known proxy/load balancer
        // Check if the connection is from a trusted proxy (localhost, private network, or known cloud provider)
        if (remoteIp != null && IsTrustedProxy(remoteIp))
        {
            var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                // Get the first (original client) IP from the chain
                var ipChain = forwardedFor.Split(',');
                foreach (var ip in ipChain)
                {
                    var trimmedIp = ip.Trim();
                    if (IPAddress.TryParse(trimmedIp, out var parsedIp))
                    {
                        // Return the first valid, non-private IP address
                        if (!IsPrivateOrLocalNetwork(parsedIp))
                        {
                            return parsedIp.ToString();
                        }
                    }
                }
            }
        }

        // Fall back to connection remote IP address
        return remoteIp?.ToString() ?? "unknown";
    }

    private static bool IsTrustedProxy(IPAddress ipAddress)
    {
        // Trust localhost
        if (IPAddress.IsLoopback(ipAddress))
            return true;

        // Trust private network ranges (where load balancers typically are)
        if (IsPrivateOrLocalNetwork(ipAddress))
            return true;

        // In production, you should configure specific trusted proxy IPs
        // For now, we'll be conservative and only trust private networks
        return false;
    }

    private static bool IsPrivateOrLocalNetwork(IPAddress ipAddress)
    {
        if (IPAddress.IsLoopback(ipAddress))
            return true;

        var bytes = ipAddress.GetAddressBytes();

        // Check IPv4 private ranges
        if (ipAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
        {
            // 10.0.0.0/8
            if (bytes[0] == 10)
                return true;

            // 172.16.0.0/12
            if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
                return true;

            // 192.168.0.0/16
            if (bytes[0] == 192 && bytes[1] == 168)
                return true;

            // 127.0.0.0/8 (localhost)
            if (bytes[0] == 127)
                return true;
        }

        // Check IPv6 private ranges
        if (ipAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
        {
            // fc00::/7 (Unique Local Addresses)
            if (bytes[0] >= 0xfc && bytes[0] <= 0xfd)
                return true;

            // fe80::/10 (Link-local)
            if (bytes[0] == 0xfe && (bytes[1] & 0xc0) == 0x80)
                return true;
        }

        return false;
    }

    private static string GetEndpointKey(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "";

        // Check if there's a specific limit for this endpoint
        foreach (var endpoint in EndpointLimits.Keys)
        {
            if (path.StartsWith(endpoint, StringComparison.OrdinalIgnoreCase))
                return endpoint;
        }

        // Return generic endpoint group
        if (path.StartsWith("/api/v1/webhooks") || path.Contains("/webhook"))
            return "/api/v1/webhooks/*";
        if (path.StartsWith("/api/v1/admin"))
            return "/api/v1/admin/*";
        if (path.StartsWith("/api/v1/auth"))
            return "/api/v1/auth/*";

        return "/api/v1/*";
    }

    private static int GetRateLimit(HttpContext context, string endpoint)
    {
        // Check for endpoint-specific limit
        if (EndpointLimits.TryGetValue(endpoint, out var specificLimit))
            return specificLimit;

        // Check user role
        if (context.User.IsInRole("Admin"))
            return AdminRequestsPerMinute;

        // Check if it's a webhook
        if (endpoint.Contains("/webhooks"))
            return WebhookRequestsPerMinute;

        // Check if user is authenticated
        if (context.User.Identity?.IsAuthenticated == true)
            return AuthenticatedRequestsPerMinute;

        // Anonymous users
        return AnonymousRequestsPerMinute;
    }

    private async Task<(bool isExceeded, int remaining, DateTime resetTime)> CheckRateLimitAsync(
        string clientId,
        string endpoint,
        int limit,
        int windowSeconds)
    {
        var key = $"ratelimit:{clientId}:{endpoint}";
        var now = DateTime.UtcNow;
        var windowStart = now.AddSeconds(-windowSeconds);
        var resetTime = now.AddSeconds(windowSeconds);

        // Try to use distributed cache (Redis) with sliding window counter
        try
        {
            // Use sliding window with two buckets for smooth rate limiting
            var currentWindowKey = $"{key}:{now.Ticks / TimeSpan.TicksPerSecond / windowSeconds}";
            var previousWindowKey = $"{key}:{(now.Ticks / TimeSpan.TicksPerSecond / windowSeconds) - 1}";

            // Get current and previous window counts
            var currentCountStr = await _cache.GetStringAsync(currentWindowKey);
            var previousCountStr = await _cache.GetStringAsync(previousWindowKey);

            var currentCount = string.IsNullOrEmpty(currentCountStr) ? 0 : int.Parse(currentCountStr);
            var previousCount = string.IsNullOrEmpty(previousCountStr) ? 0 : int.Parse(previousCountStr);

            // Calculate weighted count using sliding window algorithm
            var percentageIntoCurrentWindow = (double)(now.Ticks % (TimeSpan.TicksPerSecond * windowSeconds)) / (TimeSpan.TicksPerSecond * windowSeconds);
            var weightedCount = (int)Math.Ceiling((previousCount * (1 - percentageIntoCurrentWindow)) + currentCount);

            if (weightedCount >= limit)
            {
                _logger.LogDebug(
                    "Rate limit reached for {Key}. Current: {CurrentCount}, Previous: {PreviousCount}, Weighted: {WeightedCount}, Limit: {Limit}",
                    key, currentCount, previousCount, weightedCount, limit);
                return (true, 0, resetTime);
            }

            // Increment current window counter
            var newCurrentCount = currentCount + 1;
            await _cache.SetStringAsync(currentWindowKey, newCurrentCount.ToString(), new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(windowSeconds * 2) // Keep for 2 windows
            });

            var remaining = limit - weightedCount - 1;
            return (false, Math.Max(0, remaining), resetTime);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to use distributed cache for rate limiting, falling back to in-memory");
            // Fallback to in-memory tracking
            return CheckRateLimitInMemory(key, limit, windowStart, now);
        }
    }

    private static (bool isExceeded, int remaining, DateTime resetTime) CheckRateLimitInMemory(
        string key,
        int limit,
        DateTime windowStart,
        DateTime now)
    {
        var bucket = _requests.GetOrAdd(key, _ => new RateLimitBucket());

        lock (bucket.Lock)
        {
            bucket.Timestamps.RemoveAll(r => r <= windowStart);

            if (bucket.Timestamps.Count >= limit)
            {
                var oldestRequest = bucket.Timestamps.Min();
                var resetTime = oldestRequest.AddMinutes(1);
                return (true, 0, resetTime);
            }

            bucket.Timestamps.Add(now);
            return (false, limit - bucket.Timestamps.Count, windowStart.AddMinutes(1));
        }
    }

    private static void CleanupOldEntries(object? state)
    {
        var cutoff = DateTime.UtcNow.AddMinutes(-5);
        var keysToRemove = new List<string>();

        foreach (var kvp in _requests)
        {
            lock (kvp.Value.Lock)
            {
                if (kvp.Value.Timestamps.All(r => r < cutoff))
                    keysToRemove.Add(kvp.Key);
            }
        }

        foreach (var key in keysToRemove)
        {
            _requests.TryRemove(key, out _);
        }
    }

    /// <summary>
    /// Thread-safe rate limit bucket. The lock protects the Timestamps list
    /// from concurrent access when Redis fallback is active.
    /// </summary>
    private sealed class RateLimitBucket
    {
        public readonly object Lock = new();
        public readonly List<DateTime> Timestamps = new();
    }
}
