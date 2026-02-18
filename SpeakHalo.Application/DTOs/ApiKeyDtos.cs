namespace SpeakHalo.Application.DTOs;

public record CreateApiKeyRequest
{
    public required string Name { get; init; }
    public string? Scopes { get; init; }
    public DateTime? ExpiresAt { get; init; }
}

public record UpdateApiKeyRequest
{
    public string? Name { get; init; }
    public bool? IsActive { get; init; }
    public string? Scopes { get; init; }
}

public record ApiKeyResponse
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Prefix { get; init; }
    public string? Scopes { get; init; }
    public bool IsActive { get; init; }
    public DateTime? ExpiresAt { get; init; }
    public DateTime? LastUsedAt { get; init; }
    public string? LastUsedIp { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public record CreateApiKeyResponse
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string ApiKey { get; init; }
    public required string Prefix { get; init; }
    public string? Scopes { get; init; }
    public DateTime? ExpiresAt { get; init; }
    public DateTime CreatedAt { get; init; }
}
