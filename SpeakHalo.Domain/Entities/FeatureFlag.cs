namespace SpeakHalo.Domain.Entities;

public class FeatureFlag : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = false;
    public int RolloutPercentage { get; set; } = 0;
    public string? EnabledForUsers { get; set; }
    public string? Metadata { get; set; }
}
