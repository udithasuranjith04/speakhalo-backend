namespace SpeakHalo.Domain.Entities;

public class WhatsAppVoiceNote : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string MediaUrl { get; set; } = string.Empty;
    public string? TranscriptText { get; set; }
    public int DurationSeconds { get; set; }
    public bool IsTranscribed { get; set; } = false;
    public DateTime? TranscribedAt { get; set; }
    public string? ErrorMessage { get; set; }

    public virtual User User { get; set; } = null!;
}
