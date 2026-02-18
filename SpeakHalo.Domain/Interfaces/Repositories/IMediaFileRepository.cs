using SpeakHalo.Domain.Entities;
using SpeakHalo.Domain.Enums;

namespace SpeakHalo.Domain.Interfaces.Repositories;

public interface IMediaFileRepository : IRepository<MediaFile>
{
    /// <summary>
    /// Get media file by WhatsApp message ID
    /// </summary>
    Task<MediaFile?> GetByWhatsAppMessageIdAsync(
        string messageId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all media files for a conversation
    /// </summary>
    Task<List<MediaFile>> GetByConversationIdAsync(
        string conversationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all media files for a user
    /// </summary>
    Task<List<MediaFile>> GetByUserIdAsync(
        string userId,
        MediaType? mediaType = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all media files for a customer
    /// </summary>
    Task<List<MediaFile>> GetByCustomerIdAsync(
        string customerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get pending downloads
    /// </summary>
    Task<List<MediaFile>> GetPendingDownloadsAsync(
        int limit = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get media files pending AI processing
    /// </summary>
    Task<List<MediaFile>> GetPendingProcessingAsync(
        int limit = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get media files by download status
    /// </summary>
    Task<List<MediaFile>> GetByDownloadStatusAsync(
        DownloadStatus status,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get flagged media files for review
    /// </summary>
    Task<List<MediaFile>> GetFlaggedMediaAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get total storage size for user
    /// </summary>
    Task<long> GetTotalStorageSizeAsync(
        string userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete old media files (cleanup)
    /// </summary>
    Task<int> DeleteOldMediaAsync(
        DateTime olderThan,
        CancellationToken cancellationToken = default);
}
