using SpeakHalo.Domain.Enums;

namespace SpeakHalo.Domain.Entities;

/// <summary>
/// Represents a media file (image, video, document, audio) from WhatsApp
/// Tracks download, storage, and processing metadata
/// </summary>
public class MediaFile : BaseEntity
{
    /// <summary>
    /// Original Twilio media URL (expires after some time)
    /// </summary>
    public string OriginalUrl { get; set; } = string.Empty;

    /// <summary>
    /// Stored file path or URL (local storage or cloud)
    /// </summary>
    public string StoredPath { get; set; } = string.Empty;

    /// <summary>
    /// Thumbnail path for images/videos (if applicable)
    /// </summary>
    public string? ThumbnailPath { get; set; }

    /// <summary>
    /// Original filename from upload
    /// </summary>
    public string? OriginalFileName { get; set; }

    /// <summary>
    /// Stored filename (usually GUID-based)
    /// </summary>
    public string StoredFileName { get; set; } = string.Empty;

    /// <summary>
    /// MIME type (image/jpeg, video/mp4, application/pdf, etc.)
    /// </summary>
    public string MimeType { get; set; } = string.Empty;

    /// <summary>
    /// Media category
    /// </summary>
    public MediaType MediaType { get; set; }

    /// <summary>
    /// File size in bytes
    /// </summary>
    public long FileSizeBytes { get; set; }

    /// <summary>
    /// Image width (for images/videos)
    /// </summary>
    public int? Width { get; set; }

    /// <summary>
    /// Image height (for images/videos)
    /// </summary>
    public int? Height { get; set; }

    /// <summary>
    /// Duration in seconds (for audio/video)
    /// </summary>
    public int? DurationSeconds { get; set; }

    /// <summary>
    /// Storage location type
    /// </summary>
    public StorageType StorageType { get; set; } = StorageType.Local;

    /// <summary>
    /// Download status
    /// </summary>
    public DownloadStatus DownloadStatus { get; set; } = DownloadStatus.Pending;

    /// <summary>
    /// When download completed
    /// </summary>
    public DateTime? DownloadedAt { get; set; }

    /// <summary>
    /// Download error message if failed
    /// </summary>
    public string? DownloadError { get; set; }

    /// <summary>
    /// User who owns this media
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Related WhatsApp message ID
    /// </summary>
    public string? WhatsAppMessageId { get; set; }

    /// <summary>
    /// Related conversation ID
    /// </summary>
    public string? ConversationId { get; set; }

    /// <summary>
    /// Customer who sent the media
    /// </summary>
    public string? CustomerId { get; set; }

    /// <summary>
    /// AI vision analysis result (if processed)
    /// </summary>
    public string? VisionAnalysis { get; set; }

    /// <summary>
    /// AI analysis confidence (0-100)
    /// </summary>
    public double? AnalysisConfidence { get; set; }

    /// <summary>
    /// Whether this media was processed by AI
    /// </summary>
    public bool IsProcessed { get; set; } = false;

    /// <summary>
    /// When AI processing completed
    /// </summary>
    public DateTime? ProcessedAt { get; set; }

    /// <summary>
    /// Whether this media is flagged for review
    /// </summary>
    public bool IsFlagged { get; set; } = false;

    /// <summary>
    /// Flag reason
    /// </summary>
    public string? FlagReason { get; set; }

    /// <summary>
    /// Public access URL (if shared externally)
    /// </summary>
    public string? PublicUrl { get; set; }

    /// <summary>
    /// URL expiration time (for temporary public access)
    /// </summary>
    public DateTime? PublicUrlExpiresAt { get; set; }

    /// <summary>
    /// Metadata JSON (additional info)
    /// </summary>
    public string? Metadata { get; set; }

    // Navigation properties
    public virtual WhatsAppMessage? WhatsAppMessage { get; set; }
    public virtual User? User { get; set; }
    public virtual Customer? Customer { get; set; }
}
