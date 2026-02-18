using System.Security.Cryptography;

namespace SpeakHalo.API.Middleware;

public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Generate a unique nonce for this request
        var nonce = GenerateNonce();
        context.Items["csp-nonce"] = nonce;

        // Security headers
        context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
        context.Response.Headers.Append("X-Frame-Options", "DENY");
        context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
        context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");

        // Path-aware Content Security Policy
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;
        var csp = BuildContentSecurityPolicy(path, nonce);

        context.Response.Headers.Append("Content-Security-Policy", csp);

        // Additional security headers
        context.Response.Headers.Append("Permissions-Policy",
            "geolocation=(), microphone=(), camera=(), payment=(), usb=(), magnetometer=(), gyroscope=()");
        context.Response.Headers.Append("Strict-Transport-Security",
            "max-age=31536000; includeSubDomains; preload");
        context.Response.Headers.Append("X-Permitted-Cross-Domain-Policies", "none");

        // Remove server identification headers
        context.Response.Headers.Remove("Server");
        context.Response.Headers.Remove("X-Powered-By");
        context.Response.Headers.Remove("X-AspNet-Version");

        // Cloudflare-compatible cache headers per endpoint type
        SetCacheControlHeaders(context, path);

        await _next(context);
    }

    private static void SetCacheControlHeaders(HttpContext context, string path)
    {
        // WebSocket, webhook, and auth endpoints — never cache
        if (path.StartsWith("/api/v1/voice-stream") ||
            path.StartsWith("/api/v1/webhooks") ||
            path.StartsWith("/api/v1/whatsapp/webhook") ||
            path.StartsWith("/api/v1/auth") ||
            path.StartsWith("/hubs/"))
        {
            context.Response.Headers.Append("Cache-Control", "no-store, no-cache, must-revalidate");
            context.Response.Headers.Append("CDN-Cache-Control", "no-store");
            return;
        }

        // Health check endpoints — short edge cache (Cloudflare caches, reduces origin load)
        if (path.StartsWith("/health"))
        {
            context.Response.Headers.Append("Cache-Control", "public, max-age=10, s-maxage=10");
            context.Response.Headers.Append("CDN-Cache-Control", "max-age=10");
            return;
        }

        // All other API endpoints — private (browser can cache, Cloudflare respects per-user)
        context.Response.Headers.Append("Cache-Control", "private, no-cache");
    }

    private static string BuildContentSecurityPolicy(string path, string nonce)
    {
        // For Hangfire dashboard, allow inline scripts/styles for its UI
        // This is acceptable as Hangfire is admin-only and requires authentication
        if (path.StartsWith("/hangfire"))
        {
            return "default-src 'self'; " +
                   "script-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net; " +
                   "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net; " +
                   "img-src 'self' data: https:; " +
                   "font-src 'self' data: https://cdn.jsdelivr.net; " +
                   "connect-src 'self'; " +
                   "frame-ancestors 'none'; " +
                   "base-uri 'self'; " +
                   "form-action 'self'";
        }

        // Strict CSP for all other endpoints - no 'unsafe-inline'
        return $"default-src 'self'; " +
               $"script-src 'self' 'nonce-{nonce}' https://cdn.jsdelivr.net; " +
               $"style-src 'self' 'nonce-{nonce}' https://cdn.jsdelivr.net; " +
               $"img-src 'self' data: https:; " +
               $"font-src 'self' data: https://cdn.jsdelivr.net; " +
               $"connect-src 'self' https: wss: ws:; " +
               $"frame-ancestors 'none'; " +
               $"base-uri 'self'; " +
               $"form-action 'self'; " +
               $"upgrade-insecure-requests; " +
               $"block-all-mixed-content";
    }

    private static string GenerateNonce()
    {
        var randomBytes = new byte[16];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }
        return Convert.ToBase64String(randomBytes);
    }
}
