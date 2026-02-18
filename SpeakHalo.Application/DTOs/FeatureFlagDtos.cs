namespace SpeakHalo.Application.DTOs;

public record CreateFeatureFlagRequest
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public bool IsEnabled { get; init; } = false;
    public string? EnabledForUsers { get; init; }
    public int RolloutPercentage { get; init; } = 0;
    public string? Metadata { get; init; }
}

public record UpdateFeatureFlagRequest
{
    public string? Description { get; init; }
    public bool? IsEnabled { get; init; }
    public string? EnabledForUsers { get; init; }
    public int? RolloutPercentage { get; init; }
    public string? Metadata { get; init; }
}

public record FeatureFlagResponse
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public bool IsEnabled { get; init; }
    public string? EnabledForUsers { get; init; }
    public int RolloutPercentage { get; init; }
    public string? Metadata { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}
