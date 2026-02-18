namespace SpeakHalo.Application.DTOs;

/// <summary>
/// Result from Gemini Vision analysis of cleaning job photos
/// </summary>
public record VisionAnalysisResult
{
    public bool Success { get; init; }
    public double ConfidenceScore { get; init; } // 0-100
    public string AnalysisType { get; init; } = string.Empty;
    public EquipmentIdentification? Equipment { get; init; }
    public IssueDiagnosis? Diagnosis { get; init; }
    public List<RecommendedPart> RecommendedParts { get; init; } = new();
    public string RawAnalysis { get; init; } = string.Empty;
    public List<string> Warnings { get; init; } = new();
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Equipment identification from photo
/// </summary>
public record EquipmentIdentification
{
    public string Type { get; init; } = string.Empty; // "Carpet", "Tile Floor", "Upholstery", etc.
    public string? Brand { get; init; }
    public string? Model { get; init; }
    public string? SerialNumber { get; init; }
    public int? EstimatedAge { get; init; } // Years
    public string? Condition { get; init; } // "Good", "Fair", "Poor", "Critical"
    public List<string> VisibleComponents { get; init; } = new();
}

/// <summary>
/// Diagnosed issue from visual analysis
/// </summary>
public record IssueDiagnosis
{
    public string PrimaryIssue { get; init; } = string.Empty;
    public List<string> SecondaryIssues { get; init; } = new();
    public string Severity { get; init; } = string.Empty; // "Low", "Medium", "High", "Critical"
    public bool IsEmergency { get; init; }
    public string? RootCause { get; init; }
    public string? RepairComplexity { get; init; } // "Simple", "Moderate", "Complex"
    public int EstimatedRepairTimeMinutes { get; init; }
    public string? SafetyWarning { get; init; }
}

/// <summary>
/// Recommended part for repair
/// </summary>
public record RecommendedPart
{
    public string Name { get; init; } = string.Empty;
    public string PartNumber { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty; // "Critical", "Recommended", "Optional"
    public string? AlternativePartNumbers { get; init; }
    public decimal? EstimatedCost { get; init; }
    public string? Supplier { get; init; }
    public string Reason { get; init; } = string.Empty;
}

/// <summary>
/// Request to analyze images with Gemini Vision
/// </summary>
public record VisionAnalysisRequest
{
    public List<string> ImagePaths { get; init; } = new(); // Local file paths or URLs
    public List<byte[]> ImageData { get; init; } = new(); // Raw image bytes
    public string AnalysisType { get; init; } = "equipment_diagnosis"; // equipment_diagnosis, quote_generation, triage
    public string Industry { get; init; } = string.Empty; // Cleaning
    public string? CustomerDescription { get; init; } // What customer reported
    public bool IncludePartRecommendations { get; init; } = true;
    public bool UseHighDetailMode { get; init; } = true; // Gemini Vision detail level
}

/// <summary>
/// Multi-image analysis combining multiple photos
/// </summary>
public record MultiImageAnalysisResult
{
    public List<VisionAnalysisResult> IndividualResults { get; init; } = new();
    public VisionAnalysisResult CombinedAnalysis { get; init; } = new();
    public int TotalImagesAnalyzed { get; init; }
    public List<string> MissingAngles { get; init; } = new(); // "Close-up of serial plate", "Side view", etc.
    public bool SufficientDataForQuote { get; init; }
}
