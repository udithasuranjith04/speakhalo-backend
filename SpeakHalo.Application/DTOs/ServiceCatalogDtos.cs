namespace SpeakHalo.Application.DTOs;

public record CreateServiceCatalogItemRequest
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public decimal BasePrice { get; init; }
    public string Category { get; init; } = string.Empty;
    public int? EstimatedDurationMinutes { get; init; }
}

public record UpdateServiceCatalogItemRequest
{
    public string? Name { get; init; }
    public string? Description { get; init; }
    public decimal? BasePrice { get; init; }
    public string? Category { get; init; }
    public int? EstimatedDurationMinutes { get; init; }
    public bool? IsActive { get; init; }
}

public record ServiceCatalogItemResponse
{
    public required string Id { get; init; }
    public required string UserId { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public decimal BasePrice { get; init; }
    public required string Category { get; init; }
    public int? EstimatedDurationMinutes { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}
