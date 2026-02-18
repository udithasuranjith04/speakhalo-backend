using SpeakHalo.Application.Interfaces;

namespace SpeakHalo.API.Middleware;

public class ApiKeyAuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiKeyAuthenticationMiddleware> _logger;
    private const string ApiKeyHeaderName = "X-Api-Key";

    public ApiKeyAuthenticationMiddleware(RequestDelegate next, ILogger<ApiKeyAuthenticationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IApiKeyService apiKeyService)
    {
        var apiKey = context.Request.Headers[ApiKeyHeaderName].FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            var validationResult = await apiKeyService.ValidateApiKeyWithScopesAsync(apiKey);

            if (validationResult.IsSuccess && validationResult.Data != null)
            {
                context.Items["UserId"] = validationResult.Data.UserId;
                context.Items["AuthMethod"] = "ApiKey";
                context.Items["ApiKeyScopes"] = validationResult.Data.Scopes;

                var ipAddress = context.Connection.RemoteIpAddress?.ToString();
                if (!string.IsNullOrEmpty(ipAddress))
                {
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await apiKeyService.UpdateLastUsedAsync(validationResult.Data.UserId, ipAddress);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to update API key last used");
                        }
                    });
                }
            }
            else
            {
                _logger.LogWarning("Invalid API key attempt from {IpAddress}", context.Connection.RemoteIpAddress);
            }
        }

        await _next(context);
    }
}
