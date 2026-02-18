namespace SpeakHalo.Domain.Entities;

public class WhatsAppScheduledMessage : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime ScheduledFor { get; set; }
    public bool IsSent { get; set; } = false;
    public DateTime? SentAt { get; set; }
    public string? MessageSid { get; set; }
    public string? ErrorMessage { get; set; }

    public virtual User User { get; set; } = null!;
}
