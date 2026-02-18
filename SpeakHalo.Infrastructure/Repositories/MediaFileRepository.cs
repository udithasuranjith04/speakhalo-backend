using Microsoft.EntityFrameworkCore;
using SpeakHalo.Domain.Entities;
using SpeakHalo.Domain.Enums;
using SpeakHalo.Domain.Interfaces.Repositories;
using SpeakHalo.Infrastructure.Data;

namespace SpeakHalo.Infrastructure.Repositories;

public class MediaFileRepository : Repository<MediaFile>, IMediaFileRepository
{
    public MediaFileRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<MediaFile?> GetByWhatsAppMessageIdAsync(
        string messageId,
        CancellationToken cancellationToken = default)
    {
        return await _context.MediaFiles
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.WhatsAppMessageId == messageId, cancellationToken);
    }

    public async Task<List<MediaFile>> GetByConversationIdAsync(
        string conversationId,
        CancellationToken cancellationToken = default)
    {
        return await _context.MediaFiles
            .AsNoTracking()
            .Where(m => m.ConversationId == conversationId)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<MediaFile>> GetByUserIdAsync(
        string userId,
        MediaType? mediaType = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.MediaFiles
            .AsNoTracking()
            .Where(m => m.UserId == userId);

        if (mediaType.HasValue)
        {
            query = query.Where(m => m.MediaType == mediaType.Value);
        }

        return await query
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<MediaFile>> GetByCustomerIdAsync(
        string customerId,
        CancellationToken cancellationToken = default)
    {
        return await _context.MediaFiles
            .AsNoTracking()
            .Where(m => m.CustomerId == customerId)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<MediaFile>> GetPendingDownloadsAsync(
        int limit = 100,
        CancellationToken cancellationToken = default)
    {
        return await _context.MediaFiles
            .AsNoTracking()
            .Where(m => m.DownloadStatus == DownloadStatus.Pending)
            .OrderBy(m => m.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<MediaFile>> GetPendingProcessingAsync(
        int limit = 100,
        CancellationToken cancellationToken = default)
    {
        return await _context.MediaFiles
            .AsNoTracking()
            .Where(m => !m.IsProcessed &&
                       m.DownloadStatus == DownloadStatus.Completed &&
                       m.MediaType == MediaType.Image)
            .OrderBy(m => m.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<MediaFile>> GetByDownloadStatusAsync(
        DownloadStatus status,
        CancellationToken cancellationToken = default)
    {
        return await _context.MediaFiles
            .AsNoTracking()
            .Where(m => m.DownloadStatus == status)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<MediaFile>> GetFlaggedMediaAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.MediaFiles
            .AsNoTracking()
            .Where(m => m.IsFlagged)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<long> GetTotalStorageSizeAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        return await _context.MediaFiles
            .Where(m => m.UserId == userId && m.DownloadStatus == DownloadStatus.Completed)
            .SumAsync(m => m.FileSizeBytes, cancellationToken);
    }

    public async Task<int> DeleteOldMediaAsync(
        DateTime olderThan,
        CancellationToken cancellationToken = default)
    {
        var oldMedia = await _context.MediaFiles
            .Where(m => m.CreatedAt < olderThan)
            .ToListAsync(cancellationToken);

        _context.MediaFiles.RemoveRange(oldMedia);
        await _context.SaveChangesAsync(cancellationToken);

        return oldMedia.Count;
    }
}
