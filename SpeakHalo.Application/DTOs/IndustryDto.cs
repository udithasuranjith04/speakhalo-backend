using SpeakHalo.Domain.Enums;

namespace SpeakHalo.Application.DTOs;

public record IndustryDto
{
    public string Id { get; init; } = string.Empty;
    public ServiceType ServiceType { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public List<string> Keywords { get; init; } = new();
    public List<string> CommonServices { get; init; } = new();
    public List<string> CommonEquipment { get; init; } = new();
    public int? AverageServiceDuration { get; init; }
    public string? TypicalPriceRange { get; init; }
    public int DisplayOrder { get; init; }
}

public record CreateIndustryRequest
{
    public ServiceType ServiceType { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public List<string> Keywords { get; init; } = new();
    public List<string> CommonServices { get; init; } = new();
    public List<string> CommonEquipment { get; init; } = new();
    public List<string> TypicalIssues { get; init; } = new();
    public List<string> TechnicalTerms { get; init; } = new();
    public List<string> SeasonalConsiderations { get; init; } = new();
    public List<string> EmergencyIndicators { get; init; } = new();
    public int? AverageServiceDuration { get; init; }
    public string? TypicalPriceRange { get; init; }
    public int DisplayOrder { get; init; }
}
