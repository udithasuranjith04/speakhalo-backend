namespace SpeakHalo.Domain.Entities;

/// <summary>
/// Product knowledge for AI assistant training
/// Contains comprehensive information about SpeakHalo features, capabilities, and workflows
/// </summary>
public class ProductKnowledge : BaseEntity
{
    public required string Topic { get; set; } // "Features", "Pricing", "How-To", "Troubleshooting", etc.
    public required string Category { get; set; } // "Voice AI", "WhatsApp AI", "Photo Analysis", "Scheduling", etc.
    public required string Question { get; set; } // User question or topic
    public required string Answer { get; set; } // Detailed answer
    public string? ShortAnswer { get; set; } // Quick summary (for voice responses)

    // Related information
    public List<string> Keywords { get; set; } = new(); // For search and matching
    public List<string> RelatedTopics { get; set; } = new(); // Cross-references
    public List<string> AlternativeQuestions { get; set; } = new(); // Different ways users might ask

    // Context
    public string? UserType { get; set; } // "Business Owner", "Technician", "Customer", "Admin"
    public string? Workflow { get; set; } // Step-by-step process if applicable
    public string? Prerequisites { get; set; } // What user needs before using this feature

    // Examples and demos
    public string? ExampleScenario { get; set; }
    public string? CodeExample { get; set; } // API examples if applicable
    public string? VideoUrl { get; set; }
    public string? DocumentationUrl { get; set; }

    // AI Training metadata
    public int Importance { get; set; } = 50; // 1-100, how important this knowledge is
    public bool IsCommonQuestion { get; set; } = false;
    public int TimesReferenced { get; set; } = 0; // How often AI uses this
    public double ConfidenceThreshold { get; set; } = 0.7; // When to use this answer

    // Versioning
    public string? Version { get; set; } // Product version this applies to
    public bool IsCurrentVersion { get; set; } = true;
    public DateTime? DeprecatedAt { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime? LastUpdated { get; set; }
}

/// <summary>
/// Conversation context for AI assistant
/// Stores conversation state and history for better responses
/// </summary>
public class AIConversationContext : BaseEntity
{
    public required string ConversationId { get; set; } // WhatsApp conversation or call ID
    public required string UserId { get; set; }
    public string? CustomerId { get; set; }

    public string ConversationType { get; set; } = "WhatsApp"; // "WhatsApp", "Voice", "SMS"
    public string CurrentIntent { get; set; } = "Unknown"; // Current user intent
    public string? CurrentTopic { get; set; } // Current discussion topic

    // Conversation state
    public string? LastUserMessage { get; set; }
    public string? LastAIResponse { get; set; }
    public DateTime? LastInteractionAt { get; set; }

    // Context data (JSON)
    public string? CustomerInfo { get; set; } // Customer details
    public string? AppointmentInfo { get; set; } // Pending/active appointments
    public string? ServiceHistory { get; set; } // Past services
    public string? CurrentIssue { get; set; } // Problem being discussed
    public string? PhotoAnalysis { get; set; } // Latest vision analysis

    // AI behavior settings
    public string Tone { get; set; } = "Professional"; // "Professional", "Friendly", "Casual"
    public string Language { get; set; } = "en";
    public bool UseEmojis { get; set; } = false;

    // Session management
    public bool IsActive { get; set; } = true;
    public DateTime ExpiresAt { get; set; }
}

/// <summary>
/// AI training examples for fine-tuning
/// Real conversation examples for continuous improvement
/// </summary>
public class AITrainingExample : BaseEntity
{
    public required string UserMessage { get; set; }
    public required string ExpectedResponse { get; set; }
    public string? ActualResponse { get; set; }

    public required string Category { get; set; } // "Scheduling", "Pricing", "Technical", etc.
    public required string Intent { get; set; } // "BookAppointment", "GetQuote", "AskQuestion"

    // Quality metrics
    public bool WasSuccessful { get; set; } = true;
    public int? CustomerSatisfactionScore { get; set; } // 1-5
    public string? FeedbackNotes { get; set; }

    // Context
    public string? ConversationContext { get; set; } // Previous messages
    public string? BusinessContext { get; set; } // Industry, services offered

    // Training metadata
    public bool IsVerified { get; set; } = false; // Reviewed by human
    public string? VerifiedBy { get; set; }
    public DateTime? VerifiedAt { get; set; }

    public bool IncludeInTraining { get; set; } = true;
    public int ImportanceWeight { get; set; } = 1; // For weighted training
}
