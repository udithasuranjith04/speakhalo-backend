using SpeakHalo.Domain.Enums;

namespace SpeakHalo.Application.DTOs;

// Request DTOs
public record DownloadMediaRequest
{
    public required string MediaUrl { get; init; }
    public required string UserId { get; init; }
    public string? WhatsAppMessageId { get; init; }
    public string? ConversationId { get; init; }
    public string? CustomerId { get; init; }
    public bool ProcessWithAI { get; init; } = false;
    public bool GenerateThumbnail { get; init; } = true;
}

public record UploadMediaRequest
{
    public required string UserId { get; init; }
    public required string FileName { get; init; }
    public required string ContentType { get; init; }
    public required byte[] FileData { get; init; }
    public string? ConversationId { get; init; }
    public string? CustomerId { get; init; }
    public Dictionary<string, string>? Metadata { get; init; }
}

public record GeneratePublicUrlRequest
{
    public required string MediaFileId { get; init; }
    public int ExpirationMinutes { get; init; } = 60; // 1 hour default
}

public record ProcessMediaRequest
{
    public required string MediaFileId { get; init; }
    public bool AnalyzeWithVision { get; init; } = true;
    public bool GenerateThumbnail { get; init; } = true;
    public bool ExtractMetadata { get; init; } = true;
}

// Response DTOs
public record MediaFileResponse
{
    public required string Id { get; init; }
    public required string StoredPath { get; init; }
    public string? ThumbnailPath { get; init; }
    public string? OriginalFileName { get; init; }
    public required string StoredFileName { get; init; }
    public required string MimeType { get; init; }
    public required MediaType MediaType { get; init; }
    public long FileSizeBytes { get; init; }
    public int? Width { get; init; }
    public int? Height { get; init; }
    public int? DurationSeconds { get; init; }
    public required StorageType StorageType { get; init; }
    public required DownloadStatus DownloadStatus { get; init; }
    public DateTime? DownloadedAt { get; init; }
    public string? DownloadError { get; init; }
    public string? UserId { get; init; }
    public string? WhatsAppMessageId { get; init; }
    public string? ConversationId { get; init; }
    public string? CustomerId { get; init; }
    public string? VisionAnalysis { get; init; }
    public double? AnalysisConfidence { get; init; }
    public bool IsProcessed { get; init; }
    public DateTime? ProcessedAt { get; init; }
    public bool IsFlagged { get; init; }
    public string? FlagReason { get; init; }
    public string? PublicUrl { get; init; }
    public DateTime? PublicUrlExpiresAt { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record MediaDownloadResult
{
    public required string MediaFileId { get; init; }
    public required string StoredPath { get; init; }
    public string? ThumbnailPath { get; init; }
    public required MediaType MediaType { get; init; }
    public long FileSizeBytes { get; init; }
    public bool DownloadSuccess { get; init; }
    public string? ErrorMessage { get; init; }
    public string? VisionAnalysis { get; init; }
    public double? AnalysisConfidence { get; init; }
}

public record MediaUploadResult
{
    public required string MediaFileId { get; init; }
    public required string StoredPath { get; init; }
    public required string PublicUrl { get; init; }
    public DateTime PublicUrlExpiresAt { get; init; }
}

public record PublicUrlResult
{
    public required string PublicUrl { get; init; }
    public DateTime ExpiresAt { get; init; }
}

public record MediaStorageStats
{
    public required string UserId { get; init; }
    public long TotalFileSizeBytes { get; init; }
    public int TotalFiles { get; init; }
    public int ImageCount { get; init; }
    public int VideoCount { get; init; }
    public int AudioCount { get; init; }
    public int DocumentCount { get; init; }
    public long StorageQuotaBytes { get; init; }
    public double StorageUsedPercent { get; init; }
}

public record MediaProcessingResult
{
    public required string MediaFileId { get; init; }
    public bool ProcessingSuccess { get; init; }
    public string? ThumbnailPath { get; init; }
    public string? VisionAnalysis { get; init; }
    public double? AnalysisConfidence { get; init; }
    public Dictionary<string, object>? Metadata { get; init; }
    public string? ErrorMessage { get; init; }
}
