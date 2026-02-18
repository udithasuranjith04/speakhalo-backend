using SpeakHalo.Domain.Enums;

namespace SpeakHalo.Domain.Entities;

/// <summary>
/// Stores detailed industry information for AI-powered assistance.
/// Used to train AI models with industry-specific knowledge.
/// </summary>
public class Industry : BaseEntity
{
    public ServiceType ServiceType { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty; // cleaning
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// JSON array of industry keywords for AI context (e.g., ["cleaning", "housekeeping", "deep clean", "sanitization"])
    /// </summary>
    public string? Keywords { get; set; }

    /// <summary>
    /// JSON array of common services (e.g., ["regular cleaning", "deep cleaning", "move-out cleaning"])
    /// </summary>
    public string? CommonServices { get; set; }

    /// <summary>
    /// JSON array of common equipment brands/models (e.g., ["commercial vacuum", "carpet extractor", "pressure washer"])
    /// </summary>
    public string? CommonEquipment { get; set; }

    /// <summary>
    /// JSON array of typical issues for AI pattern recognition (e.g., ["stubborn stains", "mold in grout", "hard water deposits"])
    /// </summary>
    public string? TypicalIssues { get; set; }

    /// <summary>
    /// JSON array of common technical terms (e.g., ["HEPA filtration", "pH-neutral", "enzyme cleaner", "extraction"])
    /// </summary>
    public string? TechnicalTerms { get; set; }

    /// <summary>
    /// JSON array of seasonal considerations (e.g., ["peak season: summer", "prepare for winter"])
    /// </summary>
    public string? SeasonalConsiderations { get; set; }

    /// <summary>
    /// JSON array of emergency indicators (e.g., ["biohazard cleanup", "flood damage", "sewage backup"])
    /// </summary>
    public string? EmergencyIndicators { get; set; }

    /// <summary>
    /// Average service call duration in minutes
    /// </summary>
    public int? AverageServiceDuration { get; set; }

    /// <summary>
    /// Typical price range for standard service calls
    /// </summary>
    public string? TypicalPriceRange { get; set; }

    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; }

    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
