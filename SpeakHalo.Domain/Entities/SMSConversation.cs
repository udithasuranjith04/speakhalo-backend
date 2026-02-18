namespace SpeakHalo.Domain.Entities;

public class SMSConversation : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public string? CustomerId { get; set; }
    public string CustomerPhone { get; set; } = string.Empty;
    public string BusinessPhone { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime? LastMessageAt { get; set; }
    public int MessageCount { get; set; } = 0;

    public virtual User User { get; set; } = null!;
    public virtual Customer? Customer { get; set; }
    public virtual ICollection<SMSMessage> Messages { get; set; } = new List<SMSMessage>();
}
