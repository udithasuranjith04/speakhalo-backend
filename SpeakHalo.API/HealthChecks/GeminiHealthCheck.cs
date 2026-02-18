using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace SpeakHalo.API.HealthChecks;

/// <summary>
/// Health check for Google Gemini API connectivity
/// </summary>
public class GeminiHealthCheck : IHealthCheck
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public GeminiHealthCheck(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var apiKey = _configuration["Gemini:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
            {
                return HealthCheckResult.Degraded("Gemini API key not configured");
            }

            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(5);

            var response = await client.GetAsync(
                $"https://generativelanguage.googleapis.com/v1beta/models?key={apiKey}",
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return HealthCheckResult.Healthy("Gemini API is accessible");
            }

            return HealthCheckResult.Degraded(
                $"Gemini API returned status code: {response.StatusCode}");
        }
        catch (TaskCanceledException)
        {
            return HealthCheckResult.Degraded("Gemini API request timed out");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Gemini API is not accessible", ex);
        }
    }
}
