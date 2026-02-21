using SpeakHalo.Domain.Enums;

namespace SpeakHalo.Domain.Entities;

public class IndustryKnowledgeTemplate : BaseEntity
{
    public required ServiceType Industry { get; set; }
    public required string Category { get; set; }
    public required string Title { get; set; }
    public required string Content { get; set; }
    public int SortOrder { get; set; } = 0;
    public bool IsActive { get; set; } = true;
    public string? LastEditedBy { get; set; }
    public DateTime? LastEditedAt { get; set; }
    public int Version { get; set; } = 1;
}
