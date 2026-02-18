using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace SpeakHalo.API.HealthChecks;

/// <summary>
/// Health check for available disk space
/// </summary>
public class DiskSpaceHealthCheck : IHealthCheck
{
    private readonly long _minimumFreeBytesThreshold;

    public DiskSpaceHealthCheck(long minimumFreeBytesThreshold = 1_073_741_824) // 1 GB default
    {
        _minimumFreeBytesThreshold = minimumFreeBytesThreshold;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var drives = DriveInfo.GetDrives().Where(d => d.IsReady && d.DriveType == DriveType.Fixed);

            var data = new Dictionary<string, object>();
            var hasLowSpace = false;
            var hasCriticalSpace = false;

            foreach (var drive in drives)
            {
                var freeSpaceGB = drive.AvailableFreeSpace / (1024.0 * 1024.0 * 1024.0);
                var totalSpaceGB = drive.TotalSize / (1024.0 * 1024.0 * 1024.0);
                var usedPercentage = ((totalSpaceGB - freeSpaceGB) / totalSpaceGB) * 100;

                data[$"drive_{drive.Name.Replace("\\", "").Replace(":", "")}"] = new
                {
                    name = drive.Name,
                    freeSpaceGB = Math.Round(freeSpaceGB, 2),
                    totalSpaceGB = Math.Round(totalSpaceGB, 2),
                    usedPercentage = Math.Round(usedPercentage, 2)
                };

                if (drive.AvailableFreeSpace < _minimumFreeBytesThreshold)
                {
                    hasCriticalSpace = true;
                }
                else if (drive.AvailableFreeSpace < _minimumFreeBytesThreshold * 2)
                {
                    hasLowSpace = true;
                }
            }

            if (hasCriticalSpace)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy("Critical: Low disk space", data: data));
            }

            if (hasLowSpace)
            {
                return Task.FromResult(HealthCheckResult.Degraded("Warning: Disk space running low", data: data));
            }

            return Task.FromResult(HealthCheckResult.Healthy("Disk space is sufficient", data: data));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("Failed to check disk space", ex));
        }
    }
}
