namespace SpeakHalo.Application.Interfaces;

public interface ISystemStatusService
{
    Task<SystemStatusDto> GetSystemStatusAsync(CancellationToken cancellationToken = default);
    Task<DatabaseHealthDto> GetDatabaseHealthAsync(CancellationToken cancellationToken = default);
    Task<ExternalServicesHealthDto> GetExternalServicesHealthAsync(CancellationToken cancellationToken = default);
    Task<Dictionary<string, object>> GetSystemMetricsAsync(CancellationToken cancellationToken = default);
}

public class SystemStatusDto
{
    public string Status { get; set; } = "healthy";
    public string Version { get; set; } = "1.0.0";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public TimeSpan Uptime { get; set; }
    public Dictionary<string, bool> Services { get; set; } = new();
}

public class DatabaseHealthDto
{
    public bool IsHealthy { get; set; }
    public long ConnectionCount { get; set; }
    public double ResponseTimeMs { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class ExternalServicesHealthDto
{
    public Dictionary<string, ServiceHealthDto> Services { get; set; } = new();
}

public class ServiceHealthDto
{
    public bool IsHealthy { get; set; }
    public double ResponseTimeMs { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime LastChecked { get; set; } = DateTime.UtcNow;
}
