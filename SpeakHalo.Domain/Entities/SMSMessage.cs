using SpeakHalo.Domain.Enums;

namespace SpeakHalo.Domain.Entities;

public class SMSMessage : BaseEntity
{
    public string ConversationId { get; set; } = string.Empty;
    public MessageDirection Direction { get; set; }
    public string Content { get; set; } = string.Empty;
    public MessageStatus Status { get; set; } = MessageStatus.Sent;
    public DateTime? DeliveredAt { get; set; }
    public string? TwilioMessageSid { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }

    public virtual SMSConversation Conversation { get; set; } = null!;
}
