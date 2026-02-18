namespace SpeakHalo.Domain.Entities;

public class VoiceAgent : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string SystemPrompt { get; set; } = string.Empty;
    public string VoiceName { get; set; } = "Kore";
    public string Language { get; set; } = "en-US";
    public int MaxCallDurationMinutes { get; set; } = 30;
    public bool IsActive { get; set; } = true;

    // Multi-location support (Growth/Business plans)
    public string? LocationName { get; set; }
    public string? LocationAddress { get; set; }
    public string? LocationPhone { get; set; }

    public virtual User User { get; set; } = null!;
}
