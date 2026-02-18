namespace SpeakHalo.Domain.Entities;

public class TranscriptEntry : BaseEntity
{
    public string CallId { get; set; } = string.Empty;
    public string Speaker { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public double Confidence { get; set; } = 1.0;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public int SequenceNumber { get; set; }

    public virtual Call Call { get; set; } = null!;
}
