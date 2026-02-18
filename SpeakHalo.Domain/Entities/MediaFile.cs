using SpeakHalo.Domain.Enums;

namespace SpeakHalo.Domain.Entities;

public class MediaFile : BaseEntity
{
    public string OriginalUrl { get; set; } = string.Empty;
    public string StoredPath { get; set; } = string.Empty;
    public string? ThumbnailPath { get; set; }
    public string? OriginalFileName { get; set; }
    public string StoredFileName { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public MediaType MediaType { get; set; }
    public long FileSizeBytes { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public int? DurationSeconds { get; set; }
    public StorageType StorageType { get; set; } = StorageType.Local;
    public DownloadStatus DownloadStatus { get; set; } = DownloadStatus.Pending;
    public DateTime? DownloadedAt { get; set; }
    public string? DownloadError { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string? WhatsAppMessageId { get; set; }
    public string? ConversationId { get; set; }
    public string? CustomerId { get; set; }
    public string? VisionAnalysis { get; set; }
    public double? AnalysisConfidence { get; set; }
    public bool IsProcessed { get; set; } = false;
    public DateTime? ProcessedAt { get; set; }
    public bool IsFlagged { get; set; } = false;
    public string? FlagReason { get; set; }
    public string? PublicUrl { get; set; }
    public DateTime? PublicUrlExpiresAt { get; set; }
    public string? Metadata { get; set; }

    public virtual WhatsAppMessage? WhatsAppMessage { get; set; }
    public virtual User? User { get; set; }
    public virtual Customer? Customer { get; set; }
}
