using SpeakHalo.Domain.Enums;

namespace SpeakHalo.Application.DTOs;

public record CreateKnowledgeTemplateRequest
{
    public required string Industry { get; init; }
    public required string Category { get; init; }
    public required string Title { get; init; }
    public required string Content { get; init; }
    public int SortOrder { get; init; } = 0;
}

public record UpdateKnowledgeTemplateRequest
{
    public string? Category { get; init; }
    public string? Title { get; init; }
    public string? Content { get; init; }
    public int? SortOrder { get; init; }
    public bool? IsActive { get; init; }
}

public record KnowledgeTemplateResponse
{
    public required string Id { get; init; }
    public required string Industry { get; init; }
    public required string Category { get; init; }
    public required string Title { get; init; }
    public required string Content { get; init; }
    public int SortOrder { get; init; }
    public bool IsActive { get; init; }
    public string? LastEditedBy { get; init; }
    public DateTime? LastEditedAt { get; init; }
    public int Version { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}
