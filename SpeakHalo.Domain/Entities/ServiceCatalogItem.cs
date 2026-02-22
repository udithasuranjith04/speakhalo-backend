namespace SpeakHalo.Domain.Entities;

public class ServiceCatalogItem : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal BasePrice { get; set; }
    public string Category { get; set; } = string.Empty;
    public int? EstimatedDurationMinutes { get; set; }
    public bool IsActive { get; set; } = true;

    public virtual User User { get; set; } = null!;
}
