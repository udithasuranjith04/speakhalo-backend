using SpeakHalo.Domain.Enums;

namespace SpeakHalo.Domain.Entities;

/// <summary>
/// Knowledge base article for AI training and reference
/// Contains industry-specific technical information, troubleshooting guides, and best practices
/// </summary>
public class KnowledgeBaseArticle : BaseEntity
{
    public required string Title { get; set; }
    public required string Content { get; set; }
    public required ServiceType Industry { get; set; }
    public required string Category { get; set; } // "Equipment", "Diagnosis", "Repair", "Parts", "Safety", "Procedures"
    public List<string> Tags { get; set; } = new(); // For searching: ["cleaning", "carpet", "stain", "deep clean"]

    // Equipment/Issue specifics
    public string? EquipmentType { get; set; } // "Carpet Extractor", "Pressure Washer", etc.
    public string? EquipmentBrand { get; set; }
    public string? EquipmentModel { get; set; }
    public string? IssueType { get; set; } // "Not Cooling", "Leaking", etc.

    // Technical details
    public string? PartNumbers { get; set; } // JSON array of relevant part numbers
    public string? Symptoms { get; set; } // Common symptoms customers report
    public string? DiagnosisSteps { get; set; } // Step-by-step diagnosis guide
    public string? RepairSteps { get; set; } // Step-by-step repair guide
    public string? SafetyWarnings { get; set; } // Safety considerations
    public string? RequiredTools { get; set; } // JSON array of tools needed

    // Difficulty and timing
    public string? Complexity { get; set; } // "Simple", "Moderate", "Complex"
    public int? EstimatedTimeMinutes { get; set; }
    public string? SkillLevel { get; set; } // "Beginner", "Intermediate", "Advanced"

    // Cost information
    public decimal? TypicalPartsCost { get; set; }
    public decimal? TypicalLaborCost { get; set; }

    // AI Training metadata
    public int UseCount { get; set; } = 0; // How many times this article was used by AI
    public double Relevance { get; set; } = 1.0; // Relevance score for ranking (0-1)
    public bool IsVerified { get; set; } = false; // Reviewed by expert technician
    public string? VerifiedBy { get; set; } // Technician who verified
    public DateTime? VerifiedAt { get; set; }

    // Metadata
    public string? Author { get; set; }
    public string? Source { get; set; } // "Manufacturer Manual", "Field Experience", etc.
    public bool IsActive { get; set; } = true;
    public DateTime? PublishedAt { get; set; }
}
