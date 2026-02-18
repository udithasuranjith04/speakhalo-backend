using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace SpeakHalo.API.HealthChecks;

/// <summary>
/// Health check for memory usage
/// </summary>
public class MemoryHealthCheck : IHealthCheck
{
    private readonly long _degradedThresholdBytes;
    private readonly long _unhealthyThresholdBytes;

    public MemoryHealthCheck(long degradedThresholdBytes = 1_610_612_736, long unhealthyThresholdBytes = 2_147_483_648) // 1.5 GB / 2 GB defaults
    {
        _degradedThresholdBytes = degradedThresholdBytes;
        _unhealthyThresholdBytes = unhealthyThresholdBytes;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var process = System.Diagnostics.Process.GetCurrentProcess();
            var allocatedBytes = GC.GetTotalMemory(forceFullCollection: false);
            var workingSetBytes = process.WorkingSet64;

            var allocatedMB = allocatedBytes / (1024.0 * 1024.0);
            var workingSetMB = workingSetBytes / (1024.0 * 1024.0);

            var data = new Dictionary<string, object>
            {
                { "allocatedMB", Math.Round(allocatedMB, 2) },
                { "workingSetMB", Math.Round(workingSetMB, 2) },
                { "gen0Collections", GC.CollectionCount(0) },
                { "gen1Collections", GC.CollectionCount(1) },
                { "gen2Collections", GC.CollectionCount(2) }
            };

            if (allocatedBytes >= _unhealthyThresholdBytes)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy($"Memory usage is critical ({allocatedMB:F2} MB)", data: data));
            }

            if (allocatedBytes >= _degradedThresholdBytes)
            {
                return Task.FromResult(HealthCheckResult.Degraded($"Memory usage is high ({allocatedMB:F2} MB)", data: data));
            }

            return Task.FromResult(HealthCheckResult.Healthy($"Memory usage is normal ({allocatedMB:F2} MB)", data: data));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("Failed to check memory usage", ex));
        }
    }
}
