namespace SpeakHalo.Domain.Entities;

public class KnowledgeDocument : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Category { get; set; } = "general";
    public bool IsActive { get; set; } = true;
    public string? VectorEmbedding { get; set; }
    public DateTime? LastProcessedAt { get; set; }

    public virtual User User { get; set; } = null!;
}
