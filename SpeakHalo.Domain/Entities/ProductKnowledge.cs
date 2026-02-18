namespace SpeakHalo.Domain.Entities;

public class ProductKnowledge : BaseEntity
{
    public required string Topic { get; set; }
    public required string Category { get; set; }
    public required string Question { get; set; }
    public required string Answer { get; set; }
    public string? ShortAnswer { get; set; }

    public List<string> Keywords { get; set; } = new();
    public List<string> RelatedTopics { get; set; } = new();
    public List<string> AlternativeQuestions { get; set; } = new();

    public string? UserType { get; set; }
    public string? Workflow { get; set; }
    public string? Prerequisites { get; set; }

    public string? ExampleScenario { get; set; }
    public string? CodeExample { get; set; }
    public string? VideoUrl { get; set; }
    public string? DocumentationUrl { get; set; }

    public int Importance { get; set; } = 50;
    public bool IsCommonQuestion { get; set; } = false;
    public int TimesReferenced { get; set; } = 0;
    public double ConfidenceThreshold { get; set; } = 0.7;

    public string? Version { get; set; }
    public bool IsCurrentVersion { get; set; } = true;
    public DateTime? DeprecatedAt { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime? LastUpdated { get; set; }
}

public class AIConversationContext : BaseEntity
{
    public required string ConversationId { get; set; }
    public required string UserId { get; set; }
    public string? CustomerId { get; set; }

    public string ConversationType { get; set; } = "WhatsApp";
    public string CurrentIntent { get; set; } = "Unknown";
    public string? CurrentTopic { get; set; }

    public string? LastUserMessage { get; set; }
    public string? LastAIResponse { get; set; }
    public DateTime? LastInteractionAt { get; set; }

    public string? CustomerInfo { get; set; }
    public string? AppointmentInfo { get; set; }
    public string? ServiceHistory { get; set; }
    public string? CurrentIssue { get; set; }
    public string? PhotoAnalysis { get; set; }

    public string Tone { get; set; } = "Professional";
    public string Language { get; set; } = "en";
    public bool UseEmojis { get; set; } = false;

    public bool IsActive { get; set; } = true;
    public DateTime ExpiresAt { get; set; }
}

public class AITrainingExample : BaseEntity
{
    public required string UserMessage { get; set; }
    public required string ExpectedResponse { get; set; }
    public string? ActualResponse { get; set; }

    public required string Category { get; set; }
    public required string Intent { get; set; }

    public bool WasSuccessful { get; set; } = true;
    public int? CustomerSatisfactionScore { get; set; }
    public string? FeedbackNotes { get; set; }

    public string? ConversationContext { get; set; }
    public string? BusinessContext { get; set; }

    public bool IsVerified { get; set; } = false;
    public string? VerifiedBy { get; set; }
    public DateTime? VerifiedAt { get; set; }

    public bool IncludeInTraining { get; set; } = true;
    public int ImportanceWeight { get; set; } = 1;
}
