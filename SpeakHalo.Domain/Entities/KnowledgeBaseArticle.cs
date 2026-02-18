using SpeakHalo.Domain.Enums;

namespace SpeakHalo.Domain.Entities;

/// <summary>
/// Knowledge base article for AI training and reference.
/// Contains industry-specific technical information, troubleshooting guides, and best practices.
/// </summary>
public class KnowledgeBaseArticle : BaseEntity
{
    public required string Title { get; set; }
    public required string Content { get; set; }
    public required ServiceType Industry { get; set; }
    public required string Category { get; set; }
    public List<string> Tags { get; set; } = new();

    public string? EquipmentType { get; set; }
    public string? EquipmentBrand { get; set; }
    public string? EquipmentModel { get; set; }
    public string? IssueType { get; set; }

    public string? PartNumbers { get; set; }
    public string? Symptoms { get; set; }
    public string? DiagnosisSteps { get; set; }
    public string? RepairSteps { get; set; }
    public string? SafetyWarnings { get; set; }
    public string? RequiredTools { get; set; }

    public string? Complexity { get; set; }
    public int? EstimatedTimeMinutes { get; set; }
    public string? SkillLevel { get; set; }

    public decimal? TypicalPartsCost { get; set; }
    public decimal? TypicalLaborCost { get; set; }

    public int UseCount { get; set; } = 0;
    public double Relevance { get; set; } = 1.0;
    public bool IsVerified { get; set; } = false;
    public string? VerifiedBy { get; set; }
    public DateTime? VerifiedAt { get; set; }

    public string? Author { get; set; }
    public string? Source { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? PublishedAt { get; set; }
}
