using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace SpeakHalo.API.HealthChecks;

/// <summary>
/// Health check for Redis cache connectivity
/// </summary>
public class RedisHealthCheck : IHealthCheck
{
    private readonly IConfiguration _configuration;

    public RedisHealthCheck(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var connectionString = _configuration.GetConnectionString("Redis") ?? _configuration["Redis:ConnectionString"];
            if (string.IsNullOrEmpty(connectionString))
            {
                return HealthCheckResult.Degraded("Redis connection string not configured");
            }

            var options = ConfigurationOptions.Parse(connectionString);
            options.ConnectTimeout = 5000;
            options.SyncTimeout = 5000;

            using var connection = await ConnectionMultiplexer.ConnectAsync(options);
            var db = connection.GetDatabase();

            // Perform a simple ping operation
            var pingTime = await db.PingAsync();

            if (pingTime.TotalMilliseconds < 1000)
            {
                return HealthCheckResult.Healthy($"Redis is accessible (ping: {pingTime.TotalMilliseconds:F2}ms)");
            }

            return HealthCheckResult.Degraded($"Redis is slow (ping: {pingTime.TotalMilliseconds:F2}ms)");
        }
        catch (RedisConnectionException ex)
        {
            return HealthCheckResult.Unhealthy("Cannot connect to Redis", ex);
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Redis health check failed", ex);
        }
    }
}
