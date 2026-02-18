using SpeakHalo.Domain.Enums;

namespace SpeakHalo.Domain.Entities;

/// <summary>
/// Common issues database for AI pattern recognition
/// Helps AI quickly identify and diagnose frequent problems
/// </summary>
public class CommonIssue : BaseEntity
{
    public required string IssueName { get; set; } // "Carpet Stain Won't Come Out", "Grout Discoloration"
    public required ServiceType Industry { get; set; }
    public required string EquipmentType { get; set; }

    // Issue identification
    public List<string> CommonSymptoms { get; set; } = new(); // What customers typically report
    public List<string> VisualIndicators { get; set; } = new(); // What to look for in photos
    public List<string> CustomerKeywords { get; set; } = new(); // Words customers use

    // Diagnosis information
    public required string ProbableCause { get; set; }
    public List<string> AlternativeCauses { get; set; } = new();
    public required string Severity { get; set; } // "Low", "Medium", "High", "Critical"
    public bool IsEmergency { get; set; }

    // Solution information
    public required string RecommendedSolution { get; set; }
    public List<string> RequiredParts { get; set; } = new(); // Part IDs or names
    public List<string> RequiredTools { get; set; } = new();
    public int EstimatedRepairTimeMinutes { get; set; }
    public string RepairComplexity { get; set; } = "Moderate";

    // Cost estimates
    public decimal MinimumCost { get; set; }
    public decimal MaximumCost { get; set; }
    public decimal AverageCost { get; set; }

    // Frequency and importance
    public int OccurrenceCount { get; set; } = 0; // How often this issue occurs
    public double AiMatchConfidence { get; set; } = 0.8; // Threshold for AI to suggest this issue
    public int Priority { get; set; } = 50; // Higher = more important (1-100)

    // Seasonal/conditional
    public List<string>? SeasonalFactors { get; set; } // "Summer", "Winter", etc.
    public List<string>? EnvironmentalFactors { get; set; } // "High humidity", "Dusty environment"

    // Safety
    public string? SafetyWarning { get; set; }
    public List<string>? SafetyEquipmentRequired { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime? LastUpdated { get; set; }
}
