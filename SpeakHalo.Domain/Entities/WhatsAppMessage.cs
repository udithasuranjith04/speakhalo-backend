using SpeakHalo.Domain.Enums;

namespace SpeakHalo.Domain.Entities;

public class WhatsAppMessage : BaseEntity
{
    public string ConversationId { get; set; } = string.Empty;
    public MessageDirection Direction { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? MediaUrl { get; set; }
    public string? MediaType { get; set; }

    public MessageStatus Status { get; set; } = MessageStatus.Sent;
    public DateTime? DeliveredAt { get; set; }
    public DateTime? ReadAt { get; set; }

    public string? TwilioMessageSid { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }

    public bool IsAiGenerated { get; set; } = false;
    public string? AiContext { get; set; }

    public virtual WhatsAppConversation Conversation { get; set; } = null!;
}
