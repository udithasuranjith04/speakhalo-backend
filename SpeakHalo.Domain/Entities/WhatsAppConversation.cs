namespace SpeakHalo.Domain.Entities;

public class WhatsAppConversation : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public string? CustomerId { get; set; }
    public string CustomerPhone { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public string BusinessPhone { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
    public DateTime? LastMessageAt { get; set; }
    public int MessageCount { get; set; } = 0;
    public int UnreadCount { get; set; } = 0;

    public string AiMode { get; set; } = "auto";
    public string? Summary { get; set; }
    public string? Language { get; set; }

    public virtual User User { get; set; } = null!;
    public virtual Customer? Customer { get; set; }
    public virtual ICollection<WhatsAppMessage> Messages { get; set; } = new List<WhatsAppMessage>();
}
