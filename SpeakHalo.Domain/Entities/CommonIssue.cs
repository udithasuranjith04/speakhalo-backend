using SpeakHalo.Domain.Enums;

namespace SpeakHalo.Domain.Entities;

/// <summary>
/// Common issues database for AI pattern recognition.
/// Helps AI quickly identify and diagnose frequent problems.
/// </summary>
public class CommonIssue : BaseEntity
{
    public required string IssueName { get; set; }
    public required ServiceType Industry { get; set; }
    public required string EquipmentType { get; set; }

    public List<string> CommonSymptoms { get; set; } = new();
    public List<string> VisualIndicators { get; set; } = new();
    public List<string> CustomerKeywords { get; set; } = new();

    public required string ProbableCause { get; set; }
    public List<string> AlternativeCauses { get; set; } = new();
    public required string Severity { get; set; }
    public bool IsEmergency { get; set; }

    public required string RecommendedSolution { get; set; }
    public List<string> RequiredParts { get; set; } = new();
    public List<string> RequiredTools { get; set; } = new();
    public int EstimatedRepairTimeMinutes { get; set; }
    public string RepairComplexity { get; set; } = "Moderate";

    public decimal MinimumCost { get; set; }
    public decimal MaximumCost { get; set; }
    public decimal AverageCost { get; set; }

    public int OccurrenceCount { get; set; } = 0;
    public double AiMatchConfidence { get; set; } = 0.8;
    public int Priority { get; set; } = 50;

    public List<string>? SeasonalFactors { get; set; }
    public List<string>? EnvironmentalFactors { get; set; }

    public string? SafetyWarning { get; set; }
    public List<string>? SafetyEquipmentRequired { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime? LastUpdated { get; set; }
}
