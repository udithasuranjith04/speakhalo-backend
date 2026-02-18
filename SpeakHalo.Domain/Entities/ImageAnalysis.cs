namespace SpeakHalo.Domain.Entities;

/// <summary>
/// Tracks image analysis events for usage monitoring and soft limits.
/// </summary>
public class ImageAnalysis : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public string? CustomerId { get; set; }
    public string? AppointmentId { get; set; }

    public string ImageUrl { get; set; } = string.Empty;
    public string? ImageSource { get; set; }
    public long ImageSizeBytes { get; set; }
    public string? ImageType { get; set; }

    public string AnalysisType { get; set; } = "equipment_diagnosis";
    public string? AnalysisResult { get; set; }
    public string? ExtractedText { get; set; }
    public string? DiagnosisCategory { get; set; }
    public string? IssueIdentified { get; set; }
    public decimal? EstimatedCost { get; set; }

    public decimal ProcessingCost { get; set; } = 0.012m;
    public string Model { get; set; } = "gemini-2.0-flash";
    public int TokensUsed { get; set; }

    public DateTime BillingMonth { get; set; } = DateTime.UtcNow;
    public bool CountsTowardQuota { get; set; } = true;

    public string Status { get; set; } = "completed";
    public string? ErrorMessage { get; set; }
    public TimeSpan? ProcessingTime { get; set; }

    public virtual User User { get; set; } = null!;
    public virtual Customer? Customer { get; set; }
    public virtual Appointment? Appointment { get; set; }
}
