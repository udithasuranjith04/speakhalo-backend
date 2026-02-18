using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace SpeakHalo.API.HealthChecks;

/// <summary>
/// Health check for Twilio API connectivity
/// </summary>
public class TwilioHealthCheck : IHealthCheck
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public TwilioHealthCheck(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var accountSid = _configuration["Twilio:AccountSid"];
            var authToken = _configuration["Twilio:AuthToken"];

            if (string.IsNullOrEmpty(accountSid) || string.IsNullOrEmpty(authToken))
            {
                return HealthCheckResult.Degraded("Twilio credentials not configured");
            }

            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(5);

            // Create basic auth header
            var credentials = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{accountSid}:{authToken}"));
            client.DefaultRequestHeaders.Add("Authorization", $"Basic {credentials}");

            // Check account status
            var response = await client.GetAsync($"https://api.twilio.com/2010-04-01/Accounts/{accountSid}.json", cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return HealthCheckResult.Healthy("Twilio API is accessible");
            }

            return HealthCheckResult.Degraded($"Twilio API returned status code: {response.StatusCode}");
        }
        catch (TaskCanceledException)
        {
            return HealthCheckResult.Degraded("Twilio API request timed out");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Twilio API is not accessible", ex);
        }
    }
}
