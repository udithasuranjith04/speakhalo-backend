using SpeakHalo.Domain.Enums;

namespace SpeakHalo.Application.DTOs;

public record SendWhatsAppMessageRequest
{
    public required string UserId { get; init; }
    public required string ToNumber { get; init; }
    public required string Content { get; init; }
    public string? CustomerId { get; init; }
    public string? MediaUrl { get; init; }
    public string? MediaType { get; init; }
}

public record WhatsAppMessageResponse
{
    public required string Id { get; init; }
    public required string ConversationId { get; init; }
    public MessageDirection Direction { get; init; }
    public required string Content { get; init; }
    public string? MediaUrl { get; init; }
    public string? MediaType { get; init; }
    public MessageStatus Status { get; init; }
    public DateTime? DeliveredAt { get; init; }
    public DateTime? ReadAt { get; init; }
    public string? TwilioMessageSid { get; init; }
    public bool IsAiGenerated { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record WhatsAppConversationResponse
{
    public required string Id { get; init; }
    public required string UserId { get; init; }
    public string? CustomerId { get; init; }
    public required string CustomerPhone { get; init; }
    public string? CustomerName { get; init; }
    public required string BusinessPhone { get; init; }
    public bool IsActive { get; init; }
    public DateTime? LastMessageAt { get; init; }
    public int MessageCount { get; init; }
    public int UnreadCount { get; init; }
    public string AiMode { get; init; } = string.Empty;
    public string? Summary { get; init; }
    public string? Language { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record WhatsAppConversationDetailResponse
{
    public required string Id { get; init; }
    public required string UserId { get; init; }
    public string? CustomerId { get; init; }
    public required string CustomerPhone { get; init; }
    public string? CustomerName { get; init; }
    public required string BusinessPhone { get; init; }
    public bool IsActive { get; init; }
    public DateTime? LastMessageAt { get; init; }
    public int MessageCount { get; init; }
    public int UnreadCount { get; init; }
    public string AiMode { get; init; } = string.Empty;
    public string? Summary { get; init; }
    public string? Language { get; init; }
    public List<WhatsAppMessageResponse> Messages { get; init; } = new();
    public DateTime CreatedAt { get; init; }
}

public record UpdateWhatsAppConversationRequest
{
    public bool? IsActive { get; init; }
    public string? AiMode { get; init; }
    public string? Language { get; init; }
}

// Photo-to-Quote DTOs
public record PhotoQuoteRequest
{
    public required string ConversationId { get; init; }
    public required string UserId { get; init; }
    public List<string> ImageUrls { get; init; } = new();
    public List<byte[]> ImageData { get; init; } = new();
    public string? CustomerDescription { get; init; }
}

public record QuoteLineItem
{
    public required string Description { get; init; }
    public double Quantity { get; init; }
    public required string Unit { get; init; } // "ea", "hr", "ft", etc.
    public decimal UnitPrice { get; init; }
    public decimal Total { get; init; }
    public required string Category { get; init; } // "Parts", "Labor", "Materials"
    public string? Notes { get; init; }
}

public record ServiceQuote
{
    public required string QuoteId { get; init; }
    public decimal TotalAmount { get; init; }
    public List<QuoteLineItem> LineItems { get; init; } = new();
    public double EstimatedDurationHours { get; init; }
    public required string Urgency { get; init; } // "emergency", "urgent", "routine"
    public int ValidityDays { get; init; }
    public required string Terms { get; init; }
    public string Notes { get; set; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}

public record PhotoQuoteResult
{
    public bool Success { get; init; }
    public double ConfidenceScore { get; init; } // 0-100
    public required string Workflow { get; init; } // "auto_quote", "review_needed", "request_more_photos"
    public VisionAnalysisResult? VisionAnalysis { get; init; }
    public ServiceQuote? Quote { get; init; }
    public required string CustomerMessage { get; init; }
    public string? InternalNotes { get; init; }
    public List<string> SuggestedFollowUp { get; init; } = new();
    public int ImagesAnalyzed { get; init; }
    public List<string> MissingPhotoRequests { get; init; } = new();
}
