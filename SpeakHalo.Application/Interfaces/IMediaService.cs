using SpeakHalo.Application.DTOs;
using SpeakHalo.Domain.Enums;
using SpeakHalo.Shared.Models;

namespace SpeakHalo.Application.Interfaces;

/// <summary>
/// Media handling service for WhatsApp images, videos, documents, audio
/// Handles download, storage, processing, and AI analysis
/// </summary>
public interface IMediaService
{
    /// <summary>
    /// Download media from Twilio WhatsApp URL and store it
    /// </summary>
    Task<Result<MediaDownloadResult>> DownloadAndStoreMediaAsync(
        DownloadMediaRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Upload media file directly
    /// </summary>
    Task<Result<MediaUploadResult>> UploadMediaAsync(
        UploadMediaRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get media file by ID
    /// </summary>
    Task<Result<MediaFileResponse>> GetMediaFileAsync(
        string mediaFileId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all media files for a conversation
    /// </summary>
    Task<Result<List<MediaFileResponse>>> GetConversationMediaAsync(
        string conversationId,
        MediaType? mediaType = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all media files for a user
    /// </summary>
    Task<Result<List<MediaFileResponse>>> GetUserMediaAsync(
        string userId,
        MediaType? mediaType = null,
        int limit = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate temporary public URL for media file
    /// </summary>
    Task<Result<PublicUrlResult>> GeneratePublicUrlAsync(
        GeneratePublicUrlRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Process media file (thumbnail, AI analysis, metadata)
    /// </summary>
    Task<Result<MediaProcessingResult>> ProcessMediaAsync(
        ProcessMediaRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get storage statistics for user
    /// </summary>
    Task<Result<MediaStorageStats>> GetStorageStatsAsync(
        string userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete media file
    /// </summary>
    Task<Result<bool>> DeleteMediaAsync(
        string mediaFileId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Flag media for review
    /// </summary>
    Task<Result<bool>> FlagMediaAsync(
        string mediaFileId,
        string reason,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get flagged media files
    /// </summary>
    Task<Result<List<MediaFileResponse>>> GetFlaggedMediaAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Process pending media downloads (background job)
    /// </summary>
    Task<Result<int>> ProcessPendingDownloadsAsync(
        int batchSize = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cleanup old media files (background job)
    /// </summary>
    Task<Result<int>> CleanupOldMediaAsync(
        int daysToKeep = 90,
        CancellationToken cancellationToken = default);
}
