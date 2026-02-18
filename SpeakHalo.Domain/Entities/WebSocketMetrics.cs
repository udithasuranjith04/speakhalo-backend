namespace SpeakHalo.Domain.Entities;

public class WebSocketMetrics : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public string CallId { get; set; } = string.Empty;
    public DateTime ConnectedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DisconnectedAt { get; set; }
    public int DurationSeconds { get; set; }
    public long BytesSent { get; set; }
    public long BytesReceived { get; set; }
    public int MessagesIn { get; set; }
    public int MessagesOut { get; set; }
    public int ErrorCount { get; set; }
    public double AverageLatencyMs { get; set; }

    public virtual User User { get; set; } = null!;
    public virtual Call Call { get; set; } = null!;
}
