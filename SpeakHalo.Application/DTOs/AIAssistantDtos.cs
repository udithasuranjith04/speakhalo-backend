namespace SpeakHalo.Application.DTOs;

// Request DTOs
public record AIRequest
{
    public required string ConversationId { get; init; }
    public required string UserId { get; init; }
    public required string Message { get; init; }
    public string ConversationType { get; init; } = "WhatsApp"; // WhatsApp, Voice, SMS
    public string? CustomerId { get; init; }
    public Dictionary<string, string>? Metadata { get; init; }
}

public record UpdateContextRequest
{
    public string? CurrentIntent { get; init; }
    public string? CurrentTopic { get; init; }
    public string? CurrentIssue { get; init; }
    public string? PhotoAnalysis { get; init; }
    public string? AppointmentInfo { get; init; }
}

public record RecordTrainingRequest
{
    public required string UserMessage { get; init; }
    public required string ExpectedResponse { get; init; }
    public string? ActualResponse { get; init; }
    public required string Category { get; init; }
    public required string Intent { get; init; }
    public bool WasSuccessful { get; init; } = true;
    public int? SatisfactionScore { get; init; } // 1-5
    public string? FeedbackNotes { get; init; }
    public string? ConversationContext { get; init; }
    public string? BusinessContext { get; init; }
}

// Response DTOs
public record AIResponse
{
    public required string Message { get; init; }
    public required string ConversationId { get; init; }
    public string Intent { get; init; } = "General";
    public double Confidence { get; init; }
    public List<string> UsedKnowledge { get; init; } = new();
    public List<string> SuggestedActions { get; init; } = new();
    public DateTime Timestamp { get; init; }
}

public record ProductKnowledgeResponse
{
    public required string Id { get; init; }
    public required string Topic { get; init; }
    public required string Category { get; init; }
    public required string Question { get; init; }
    public required string Answer { get; init; }
    public string? ShortAnswer { get; init; }
    public List<string> Keywords { get; init; } = new();
    public List<string> RelatedTopics { get; init; } = new();
    public int Importance { get; init; }
    public bool IsCommonQuestion { get; init; }
    public int TimesReferenced { get; init; }
}

public record AIConversationContextResponse
{
    public required string Id { get; init; }
    public required string ConversationId { get; init; }
    public required string UserId { get; init; }
    public string ConversationType { get; init; } = "WhatsApp";
    public string CurrentIntent { get; init; } = "Unknown";
    public string? CurrentTopic { get; init; }
    public string? LastUserMessage { get; init; }
    public string? LastAIResponse { get; init; }
    public DateTime? LastInteractionAt { get; init; }
    public bool IsActive { get; init; }
    public DateTime ExpiresAt { get; init; }
}

public record AITrainingExampleResponse
{
    public required string Id { get; init; }
    public required string UserMessage { get; init; }
    public required string ExpectedResponse { get; init; }
    public string? ActualResponse { get; init; }
    public required string Category { get; init; }
    public required string Intent { get; init; }
    public bool WasSuccessful { get; init; }
    public int? CustomerSatisfactionScore { get; init; }
    public bool IsVerified { get; init; }
    public DateTime CreatedAt { get; init; }
}
