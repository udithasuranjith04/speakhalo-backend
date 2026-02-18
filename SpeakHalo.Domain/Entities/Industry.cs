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
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    /// <summary>JSON array of industry keywords for AI context</summary>
    public string? Keywords { get; set; }

    /// <summary>JSON array of common services</summary>
    public string? CommonServices { get; set; }

    /// <summary>JSON array of common equipment brands/models</summary>
    public string? CommonEquipment { get; set; }

    /// <summary>JSON array of typical issues for AI pattern recognition</summary>
    public string? TypicalIssues { get; set; }

    /// <summary>JSON array of common technical terms</summary>
    public string? TechnicalTerms { get; set; }

    /// <summary>JSON array of seasonal considerations</summary>
    public string? SeasonalConsiderations { get; set; }

    /// <summary>JSON array of emergency indicators</summary>
    public string? EmergencyIndicators { get; set; }

    public int? AverageServiceDuration { get; set; }
    public string? TypicalPriceRange { get; set; }

    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; }

    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
