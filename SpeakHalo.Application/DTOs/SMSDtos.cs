using SpeakHalo.Domain.Enums;

namespace SpeakHalo.Application.DTOs;

public record SendSMSRequest
{
    public required string UserId { get; init; }
    public required string ToNumber { get; init; }
    public required string Content { get; init; }
    public string? CustomerId { get; init; }
}

public record SMSMessageResponse
{
    public required string Id { get; init; }
    public required string ConversationId { get; init; }
    public MessageDirection Direction { get; init; }
    public required string Content { get; init; }
    public MessageStatus Status { get; init; }
    public DateTime? DeliveredAt { get; init; }
    public string? TwilioMessageSid { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record SMSConversationResponse
{
    public required string Id { get; init; }
    public required string UserId { get; init; }
    public string? CustomerId { get; init; }
    public required string CustomerPhone { get; init; }
    public required string BusinessPhone { get; init; }
    public bool IsActive { get; init; }
    public DateTime? LastMessageAt { get; init; }
    public int MessageCount { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record SMSConversationDetailResponse
{
    public required string Id { get; init; }
    public required string UserId { get; init; }
    public string? CustomerId { get; init; }
    public required string CustomerPhone { get; init; }
    public required string BusinessPhone { get; init; }
    public bool IsActive { get; init; }
    public DateTime? LastMessageAt { get; init; }
    public int MessageCount { get; init; }
    public List<SMSMessageResponse> Messages { get; init; } = new();
    public DateTime CreatedAt { get; init; }
}
