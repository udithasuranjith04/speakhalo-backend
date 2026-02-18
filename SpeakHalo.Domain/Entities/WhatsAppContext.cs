namespace SpeakHalo.Domain.Entities;

public class WhatsAppContext : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string ConversationContext { get; set; } = string.Empty;
    public DateTime LastInteraction { get; set; } = DateTime.UtcNow;
    public int MessageCount { get; set; } = 0;
    public string? CurrentIntent { get; set; }
    public string? SessionData { get; set; }

    public virtual User User { get; set; } = null!;
}
