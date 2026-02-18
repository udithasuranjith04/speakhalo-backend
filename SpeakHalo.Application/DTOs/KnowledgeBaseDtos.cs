using SpeakHalo.Domain.Enums;

namespace SpeakHalo.Application.DTOs;

// Request DTOs
public record CreateKnowledgeArticleRequest
{
    public required string Title { get; init; }
    public required string Content { get; init; }
    public required ServiceType Industry { get; init; }
    public required string Category { get; init; }
    public List<string> Tags { get; init; } = new();
    public string? EquipmentType { get; init; }
    public string? EquipmentBrand { get; init; }
    public string? EquipmentModel { get; init; }
    public string? IssueType { get; init; }
    public List<string>? PartNumbers { get; init; }
    public string? Symptoms { get; init; }
    public string? DiagnosisSteps { get; init; }
    public string? RepairSteps { get; init; }
    public string? SafetyWarnings { get; init; }
    public List<string>? RequiredTools { get; init; }
    public string? Complexity { get; init; }
    public int? EstimatedTimeMinutes { get; init; }
    public string? SkillLevel { get; init; }
    public decimal? TypicalPartsCost { get; init; }
    public decimal? TypicalLaborCost { get; init; }
    public string? Author { get; init; }
    public string? Source { get; init; }
}

public record UpdateKnowledgeArticleRequest
{
    public string? Title { get; init; }
    public string? Content { get; init; }
    public string? Category { get; init; }
    public List<string>? Tags { get; init; }
    public bool? IsActive { get; init; }
}

public record KnowledgeSearchRequest
{
    public required string SearchQuery { get; init; }
    public ServiceType? Industry { get; init; }
    public string? Category { get; init; }
    public string? EquipmentType { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

// Response DTOs
public record KnowledgeRecommendation
{
    public required string Type { get; init; } // "Article", "CommonIssue", "Part"
    public required string Title { get; init; }
    public required string Summary { get; init; }
    public double RelevanceScore { get; init; }
    public string? Source { get; init; }
    public string? EquipmentType { get; init; }
    public decimal? EstimatedCost { get; init; }
    public int? EstimatedTimeMinutes { get; init; }
    public string? Severity { get; init; }
}

public record KnowledgeBaseExport
{
    public int TotalArticles { get; init; }
    public int TotalCommonIssues { get; init; }
    public string? Industry { get; init; }
    public DateTime ExportedAt { get; init; }
    public List<object> Articles { get; init; } = new();
    public List<object> CommonIssues { get; init; } = new();
}

public record KnowledgeArticleResponse
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public required string Content { get; init; }
    public ServiceType Industry { get; init; }
    public string Category { get; init; } = string.Empty;
    public List<string> Tags { get; init; } = new();
    public string? EquipmentType { get; init; }
    public string? EquipmentBrand { get; init; }
    public string? IssueType { get; init; }
    public string? Complexity { get; init; }
    public int? EstimatedTimeMinutes { get; init; }
    public decimal? TypicalPartsCost { get; init; }
    public decimal? TypicalLaborCost { get; init; }
    public int UseCount { get; init; }
    public bool IsVerified { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record CommonIssueResponse
{
    public required string Id { get; init; }
    public required string IssueName { get; init; }
    public ServiceType Industry { get; init; }
    public string EquipmentType { get; init; } = string.Empty;
    public List<string> CommonSymptoms { get; init; } = new();
    public string ProbableCause { get; init; } = string.Empty;
    public string Severity { get; init; } = string.Empty;
    public bool IsEmergency { get; init; }
    public string RecommendedSolution { get; init; } = string.Empty;
    public List<string> RequiredParts { get; init; } = new();
    public int EstimatedRepairTimeMinutes { get; init; }
    public decimal AverageCost { get; init; }
    public int OccurrenceCount { get; init; }
}

