namespace SpeakHalo.Domain.Entities;

/// <summary>
/// Tracks image analysis events for usage monitoring and soft limits
/// </summary>
public class ImageAnalysis : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public string? CustomerId { get; set; } // Associated customer (if applicable)
    public string? AppointmentId { get; set; } // Associated appointment (if applicable)

    // Image Details
    public string ImageUrl { get; set; } = string.Empty;
    public string? ImageSource { get; set; } // "whatsapp", "sms", "upload", etc.
    public long ImageSizeBytes { get; set; }
    public string? ImageType { get; set; } // "image/jpeg", "image/png", etc.

    // Analysis Details
    public string AnalysisType { get; set; } = "equipment_diagnosis"; // Type of analysis performed
    public string? AnalysisResult { get; set; } // JSON result from Gemini Vision
    public string? ExtractedText { get; set; } // Any text extracted from image
    public string? DiagnosisCategory { get; set; } // e.g., "residential", "commercial", "carpet"
    public string? IssueIdentified { get; set; } // Brief description of identified issue
    public decimal? EstimatedCost { get; set; } // AI-suggested cost estimate

    // Cost Tracking
    public decimal ProcessingCost { get; set; } = 0.012m; // Cost per high-detail image analysis
    public string Model { get; set; } = "gemini-2.0-flash"; // Model used for analysis
    public int TokensUsed { get; set; } // Tokens consumed in analysis

    // Billing Month Tracking
    public DateTime BillingMonth { get; set; } = DateTime.UtcNow; // Month this usage counts toward
    public bool CountsTowardQuota { get; set; } = true; // Whether this counts toward monthly limit

    // Status
    public string Status { get; set; } = "completed"; // completed, failed, pending
    public string? ErrorMessage { get; set; }
    public TimeSpan? ProcessingTime { get; set; }

    // Navigation Properties
    public virtual User User { get; set; } = null!;
    public virtual Customer? Customer { get; set; }
    public virtual Appointment? Appointment { get; set; }
}
