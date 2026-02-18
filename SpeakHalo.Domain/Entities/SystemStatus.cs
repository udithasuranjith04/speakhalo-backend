namespace SpeakHalo.Domain.Entities;

public class SystemStatus : BaseEntity
{
    public string ServiceName { get; set; } = string.Empty;
    public string Status { get; set; } = "healthy";
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
    public int ResponseTimeMs { get; set; }
    public string? ErrorMessage { get; set; }
    public string? Metadata { get; set; }
}
