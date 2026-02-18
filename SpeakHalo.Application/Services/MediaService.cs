using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SpeakHalo.Application.DTOs;
using SpeakHalo.Application.Interfaces;
using SpeakHalo.Domain.Entities;
using SpeakHalo.Domain.Enums;
using SpeakHalo.Domain.Interfaces.Repositories;
using SpeakHalo.Shared.Models;
using System.Text.Json;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;

namespace SpeakHalo.Application.Services;

/// <summary>
/// Media handling service implementation
/// Downloads, stores, and processes WhatsApp media files
/// </summary>
public class MediaService : IMediaService
{
    private readonly IMediaFileRepository _mediaRepository;
    private readonly IFileStorageService _fileStorage;
    private readonly IAiVisionService _visionService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<MediaService> _logger;

    private const int THUMBNAIL_WIDTH = 300;
    private const int THUMBNAIL_HEIGHT = 300;
    private const long MAX_FILE_SIZE = 104857600; // 100MB

    public MediaService(
        IMediaFileRepository mediaRepository,
        IFileStorageService fileStorage,
        IAiVisionService visionService,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<MediaService> logger)
    {
        _mediaRepository = mediaRepository;
        _fileStorage = fileStorage;
        _visionService = visionService;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<Result<MediaDownloadResult>> DownloadAndStoreMediaAsync(
        DownloadMediaRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Downloading media from: {Url}", request.MediaUrl);

            // SSRF protection: validate URL before downloading
            if (!IsAllowedMediaUrl(request.MediaUrl))
            {
                _logger.LogWarning("Blocked media download from disallowed URL: {Url}", request.MediaUrl);
                return Result<MediaDownloadResult>.Failure("Media URL is not from an allowed source");
            }

            // Download media from Twilio
            var httpClient = _httpClientFactory.CreateClient();

            // Add Twilio auth if available
            var twilioAccountSid = _configuration["Twilio:AccountSid"];
            var twilioAuthToken = _configuration["Twilio:AuthToken"];

            if (!string.IsNullOrEmpty(twilioAccountSid) && !string.IsNullOrEmpty(twilioAuthToken))
            {
                var authBytes = System.Text.Encoding.ASCII.GetBytes($"{twilioAccountSid}:{twilioAuthToken}");
                var authHeader = Convert.ToBase64String(authBytes);
                httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authHeader);
            }

            var response = await httpClient.GetAsync(request.MediaUrl, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return Result<MediaDownloadResult>.Failure($"Failed to download media: {response.StatusCode}");
            }

            var fileData = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";

            // Check file size
            if (fileData.Length > MAX_FILE_SIZE)
            {
                return Result<MediaDownloadResult>.Failure($"File too large: {fileData.Length} bytes (max: {MAX_FILE_SIZE})");
            }

            // Determine media type
            var mediaType = DetermineMediaType(contentType);
            var fileExtension = GetFileExtension(contentType);

            // Generate unique filename
            var fileName = $"{Guid.NewGuid()}{fileExtension}";
            var folder = $"media/{request.UserId}/{DateTime.UtcNow:yyyy/MM}";

            // Save to storage
            var storedPath = await _fileStorage.SaveFileAsync(
                fileName,
                fileData,
                contentType,
                folder,
                cancellationToken
            );

            // Create media file record
            var mediaFile = new MediaFile
            {
                OriginalUrl = request.MediaUrl,
                StoredPath = storedPath,
                StoredFileName = fileName,
                MimeType = contentType,
                MediaType = mediaType,
                FileSizeBytes = fileData.Length,
                StorageType = StorageType.Local,
                DownloadStatus = DownloadStatus.Completed,
                DownloadedAt = DateTime.UtcNow,
                UserId = request.UserId,
                WhatsAppMessageId = request.WhatsAppMessageId,
                ConversationId = request.ConversationId,
                CustomerId = request.CustomerId
            };

            string? thumbnailPath = null;
            string? visionAnalysis = null;
            double? analysisConfidence = null;

            // Generate thumbnail for images
            if (mediaType == MediaType.Image && request.GenerateThumbnail)
            {
                thumbnailPath = await GenerateThumbnailAsync(
                    fileData,
                    fileName,
                    folder,
                    cancellationToken
                );
                mediaFile.ThumbnailPath = thumbnailPath;
            }

            // Extract image dimensions
            if (mediaType == MediaType.Image)
            {
                var (width, height) = await GetImageDimensionsAsync(fileData);
                mediaFile.Width = width;
                mediaFile.Height = height;
            }

            // AI vision analysis for images
            if (mediaType == MediaType.Image && request.ProcessWithAI)
            {
                var visionRequest = new VisionAnalysisRequest
                {
                    ImageData = new List<byte[]> { fileData },
                    AnalysisType = "equipment_diagnosis",
                    Industry = string.Empty
                };

                var visionResult = await _visionService.AnalyzeImageAsync(
                    visionRequest,
                    cancellationToken
                );

                if (visionResult.IsSuccess && visionResult.Data != null)
                {
                    visionAnalysis = JsonSerializer.Serialize(visionResult.Data);
                    analysisConfidence = visionResult.Data.ConfidenceScore;

                    mediaFile.VisionAnalysis = visionAnalysis;
                    mediaFile.AnalysisConfidence = analysisConfidence;
                    mediaFile.IsProcessed = true;
                    mediaFile.ProcessedAt = DateTime.UtcNow;
                }
            }

            await _mediaRepository.AddAsync(mediaFile, cancellationToken);

            _logger.LogInformation(
                "Media downloaded and stored: {MediaFileId}, Size: {Size} bytes, Type: {Type}",
                mediaFile.Id,
                mediaFile.FileSizeBytes,
                mediaFile.MediaType
            );

            return Result<MediaDownloadResult>.Success(new MediaDownloadResult
            {
                MediaFileId = mediaFile.Id,
                StoredPath = storedPath,
                ThumbnailPath = thumbnailPath,
                MediaType = mediaType,
                FileSizeBytes = fileData.Length,
                DownloadSuccess = true,
                VisionAnalysis = visionAnalysis,
                AnalysisConfidence = analysisConfidence
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download and store media: {Url}", request.MediaUrl);
            return Result<MediaDownloadResult>.Failure($"Media download failed: {ex.Message}");
        }
    }

    public async Task<Result<MediaUploadResult>> UploadMediaAsync(
        UploadMediaRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Check file size
            if (request.FileData.Length > MAX_FILE_SIZE)
            {
                return Result<MediaUploadResult>.Failure($"File too large: {request.FileData.Length} bytes");
            }

            var mediaType = DetermineMediaType(request.ContentType);
            var fileExtension = Path.GetExtension(request.FileName);

            var fileName = $"{Guid.NewGuid()}{fileExtension}";
            var folder = $"media/{request.UserId}/{DateTime.UtcNow:yyyy/MM}";

            var storedPath = await _fileStorage.SaveFileAsync(
                fileName,
                request.FileData,
                request.ContentType,
                folder,
                cancellationToken
            );

            var mediaFile = new MediaFile
            {
                OriginalUrl = string.Empty,
                StoredPath = storedPath,
                OriginalFileName = request.FileName,
                StoredFileName = fileName,
                MimeType = request.ContentType,
                MediaType = mediaType,
                FileSizeBytes = request.FileData.Length,
                StorageType = StorageType.Local,
                DownloadStatus = DownloadStatus.Completed,
                DownloadedAt = DateTime.UtcNow,
                UserId = request.UserId,
                ConversationId = request.ConversationId,
                CustomerId = request.CustomerId,
                Metadata = request.Metadata != null ? JsonSerializer.Serialize(request.Metadata) : null
            };

            await _mediaRepository.AddAsync(mediaFile, cancellationToken);

            var publicUrl = await _fileStorage.GetPublicUrlAsync(storedPath, 60, cancellationToken);

            return Result<MediaUploadResult>.Success(new MediaUploadResult
            {
                MediaFileId = mediaFile.Id,
                StoredPath = storedPath,
                PublicUrl = publicUrl,
                PublicUrlExpiresAt = DateTime.UtcNow.AddMinutes(60)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload media");
            return Result<MediaUploadResult>.Failure($"Media upload failed: {ex.Message}");
        }
    }

    public async Task<Result<MediaFileResponse>> GetMediaFileAsync(
        string mediaFileId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var mediaFile = await _mediaRepository.GetByIdAsync(mediaFileId, cancellationToken);

            if (mediaFile == null)
            {
                return Result<MediaFileResponse>.Failure("Media file not found");
            }

            return Result<MediaFileResponse>.Success(MapToResponse(mediaFile));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get media file: {MediaFileId}", mediaFileId);
            return Result<MediaFileResponse>.Failure($"Failed to get media file: {ex.Message}");
        }
    }

    public async Task<Result<List<MediaFileResponse>>> GetConversationMediaAsync(
        string conversationId,
        MediaType? mediaType = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var mediaFiles = await _mediaRepository.GetByConversationIdAsync(conversationId, cancellationToken);

            if (mediaType.HasValue)
            {
                mediaFiles = mediaFiles.Where(m => m.MediaType == mediaType.Value).ToList();
            }

            var responses = mediaFiles.Select(MapToResponse).ToList();

            return Result<List<MediaFileResponse>>.Success(responses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get conversation media: {ConversationId}", conversationId);
            return Result<List<MediaFileResponse>>.Failure($"Failed to get media: {ex.Message}");
        }
    }

    public async Task<Result<List<MediaFileResponse>>> GetUserMediaAsync(
        string userId,
        MediaType? mediaType = null,
        int limit = 100,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var mediaFiles = await _mediaRepository.GetByUserIdAsync(userId, mediaType, cancellationToken);

            var responses = mediaFiles
                .Take(limit)
                .Select(MapToResponse)
                .ToList();

            return Result<List<MediaFileResponse>>.Success(responses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user media: {UserId}", userId);
            return Result<List<MediaFileResponse>>.Failure($"Failed to get media: {ex.Message}");
        }
    }

    public async Task<Result<PublicUrlResult>> GeneratePublicUrlAsync(
        GeneratePublicUrlRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var mediaFile = await _mediaRepository.GetByIdAsync(request.MediaFileId, cancellationToken);

            if (mediaFile == null)
            {
                return Result<PublicUrlResult>.Failure("Media file not found");
            }

            var publicUrl = await _fileStorage.GetPublicUrlAsync(
                mediaFile.StoredPath,
                request.ExpirationMinutes,
                cancellationToken
            );

            var expiresAt = DateTime.UtcNow.AddMinutes(request.ExpirationMinutes);

            // Update media file
            mediaFile.PublicUrl = publicUrl;
            mediaFile.PublicUrlExpiresAt = expiresAt;
            _mediaRepository.Update(mediaFile);

            return Result<PublicUrlResult>.Success(new PublicUrlResult
            {
                PublicUrl = publicUrl,
                ExpiresAt = expiresAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate public URL: {MediaFileId}", request.MediaFileId);
            return Result<PublicUrlResult>.Failure($"Failed to generate URL: {ex.Message}");
        }
    }

    public async Task<Result<MediaProcessingResult>> ProcessMediaAsync(
        ProcessMediaRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var mediaFile = await _mediaRepository.GetByIdAsync(request.MediaFileId, cancellationToken);

            if (mediaFile == null)
            {
                return Result<MediaProcessingResult>.Failure("Media file not found");
            }

            string? thumbnailPath = null;
            string? visionAnalysis = null;
            double? analysisConfidence = null;

            if (mediaFile.MediaType == MediaType.Image)
            {
                var fileData = await _fileStorage.GetFileAsync(mediaFile.StoredPath, cancellationToken);

                // Generate thumbnail
                if (request.GenerateThumbnail && string.IsNullOrEmpty(mediaFile.ThumbnailPath))
                {
                    thumbnailPath = await GenerateThumbnailAsync(
                        fileData,
                        mediaFile.StoredFileName,
                        Path.GetDirectoryName(mediaFile.StoredPath) ?? string.Empty,
                        cancellationToken
                    );
                    mediaFile.ThumbnailPath = thumbnailPath;
                }

                // AI vision analysis
                if (request.AnalyzeWithVision && !mediaFile.IsProcessed)
                {
                    var visionRequest = new VisionAnalysisRequest
                    {
                        ImageData = new List<byte[]> { fileData },
                        AnalysisType = "equipment_diagnosis",
                        Industry = string.Empty
                    };

                    var visionResult = await _visionService.AnalyzeImageAsync(
                        visionRequest,
                        cancellationToken
                    );

                    if (visionResult.IsSuccess && visionResult.Data != null)
                    {
                        visionAnalysis = JsonSerializer.Serialize(visionResult.Data);
                        analysisConfidence = visionResult.Data.ConfidenceScore;

                        mediaFile.VisionAnalysis = visionAnalysis;
                        mediaFile.AnalysisConfidence = analysisConfidence;
                        mediaFile.IsProcessed = true;
                        mediaFile.ProcessedAt = DateTime.UtcNow;
                    }
                }
            }

            _mediaRepository.Update(mediaFile);

            return Result<MediaProcessingResult>.Success(new MediaProcessingResult
            {
                MediaFileId = mediaFile.Id,
                ProcessingSuccess = true,
                ThumbnailPath = thumbnailPath ?? mediaFile.ThumbnailPath,
                VisionAnalysis = visionAnalysis ?? mediaFile.VisionAnalysis,
                AnalysisConfidence = analysisConfidence ?? mediaFile.AnalysisConfidence,
                Metadata = null
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process media: {MediaFileId}", request.MediaFileId);
            return Result<MediaProcessingResult>.Failure($"Processing failed: {ex.Message}");
        }
    }

    public async Task<Result<MediaStorageStats>> GetStorageStatsAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var mediaFiles = await _mediaRepository.GetByUserIdAsync(userId, null, cancellationToken);

            var totalSize = await _mediaRepository.GetTotalStorageSizeAsync(userId, cancellationToken);
            var totalFiles = mediaFiles.Count;
            var imageCount = mediaFiles.Count(m => m.MediaType == MediaType.Image);
            var videoCount = mediaFiles.Count(m => m.MediaType == MediaType.Video);
            var audioCount = mediaFiles.Count(m => m.MediaType == MediaType.Audio || m.MediaType == MediaType.Voice);
            var documentCount = mediaFiles.Count(m => m.MediaType == MediaType.Document);

            // Get storage quota from configuration (default 5GB)
            var quotaBytes = long.Parse(_configuration["Storage:QuotaBytes"] ?? "5368709120");
            var usedPercent = totalSize > 0 ? (double)totalSize / quotaBytes * 100 : 0;

            return Result<MediaStorageStats>.Success(new MediaStorageStats
            {
                UserId = userId,
                TotalFileSizeBytes = totalSize,
                TotalFiles = totalFiles,
                ImageCount = imageCount,
                VideoCount = videoCount,
                AudioCount = audioCount,
                DocumentCount = documentCount,
                StorageQuotaBytes = quotaBytes,
                StorageUsedPercent = usedPercent
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get storage stats: {UserId}", userId);
            return Result<MediaStorageStats>.Failure($"Failed to get stats: {ex.Message}");
        }
    }

    public async Task<Result<bool>> DeleteMediaAsync(
        string mediaFileId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var mediaFile = await _mediaRepository.GetByIdAsync(mediaFileId, cancellationToken);

            if (mediaFile == null)
            {
                return Result<bool>.Failure("Media file not found");
            }

            // Delete from storage
            await _fileStorage.DeleteFileAsync(mediaFile.StoredPath, cancellationToken);

            if (!string.IsNullOrEmpty(mediaFile.ThumbnailPath))
            {
                await _fileStorage.DeleteFileAsync(mediaFile.ThumbnailPath, cancellationToken);
            }

            // Delete from database
            _mediaRepository.Remove(mediaFile);

            _logger.LogInformation("Deleted media file: {MediaFileId}", mediaFileId);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete media: {MediaFileId}", mediaFileId);
            return Result<bool>.Failure($"Delete failed: {ex.Message}");
        }
    }

    public async Task<Result<bool>> FlagMediaAsync(
        string mediaFileId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var mediaFile = await _mediaRepository.GetByIdAsync(mediaFileId, cancellationToken);

            if (mediaFile == null)
            {
                return Result<bool>.Failure("Media file not found");
            }

            mediaFile.IsFlagged = true;
            mediaFile.FlagReason = reason;
            _mediaRepository.Update(mediaFile);

            _logger.LogInformation("Flagged media: {MediaFileId}, Reason: {Reason}", mediaFileId, reason);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to flag media: {MediaFileId}", mediaFileId);
            return Result<bool>.Failure($"Flag failed: {ex.Message}");
        }
    }

    public async Task<Result<List<MediaFileResponse>>> GetFlaggedMediaAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var mediaFiles = await _mediaRepository.GetFlaggedMediaAsync(cancellationToken);
            var responses = mediaFiles.Select(MapToResponse).ToList();

            return Result<List<MediaFileResponse>>.Success(responses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get flagged media");
            return Result<List<MediaFileResponse>>.Failure($"Failed to get flagged media: {ex.Message}");
        }
    }

    public async Task<Result<int>> ProcessPendingDownloadsAsync(
        int batchSize = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var pending = await _mediaRepository.GetPendingDownloadsAsync(batchSize, cancellationToken);

            _logger.LogInformation("Processing {Count} pending downloads", pending.Count);

            int processed = 0;

            foreach (var mediaFile in pending)
            {
                try
                {
                    var request = new DownloadMediaRequest
                    {
                        MediaUrl = mediaFile.OriginalUrl,
                        UserId = mediaFile.UserId,
                        WhatsAppMessageId = mediaFile.WhatsAppMessageId,
                        ConversationId = mediaFile.ConversationId,
                        CustomerId = mediaFile.CustomerId,
                        ProcessWithAI = true,
                        GenerateThumbnail = true
                    };

                    await DownloadAndStoreMediaAsync(request, cancellationToken);
                    processed++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process pending download: {MediaFileId}", mediaFile.Id);

                    mediaFile.DownloadStatus = DownloadStatus.Failed;
                    mediaFile.DownloadError = ex.Message;
                    _mediaRepository.Update(mediaFile);
                }
            }

            return Result<int>.Success(processed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process pending downloads");
            return Result<int>.Failure($"Processing failed: {ex.Message}");
        }
    }

    public async Task<Result<int>> CleanupOldMediaAsync(
        int daysToKeep = 90,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var olderThan = DateTime.UtcNow.AddDays(-daysToKeep);
            var deletedCount = await _mediaRepository.DeleteOldMediaAsync(olderThan, cancellationToken);

            _logger.LogInformation("Cleaned up {Count} old media files", deletedCount);

            return Result<int>.Success(deletedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup old media");
            return Result<int>.Failure($"Cleanup failed: {ex.Message}");
        }
    }

    // Helper methods

    private MediaType DetermineMediaType(string contentType)
    {
        if (contentType.StartsWith("image/")) return MediaType.Image;
        if (contentType.StartsWith("video/")) return MediaType.Video;
        if (contentType.StartsWith("audio/")) return MediaType.Audio;
        if (contentType.Contains("pdf") || contentType.Contains("document") ||
            contentType.Contains("msword") || contentType.Contains("spreadsheet"))
            return MediaType.Document;

        return MediaType.Unknown;
    }

    private string GetFileExtension(string contentType)
    {
        return contentType switch
        {
            "image/jpeg" => ".jpg",
            "image/png" => ".png",
            "image/gif" => ".gif",
            "image/webp" => ".webp",
            "video/mp4" => ".mp4",
            "video/quicktime" => ".mov",
            "audio/mpeg" => ".mp3",
            "audio/ogg" => ".ogg",
            "application/pdf" => ".pdf",
            _ => ".bin"
        };
    }

    private async Task<string?> GenerateThumbnailAsync(
        byte[] imageData,
        string originalFileName,
        string folder,
        CancellationToken cancellationToken)
    {
        try
        {
            using var image = Image.Load(imageData);

            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new Size(THUMBNAIL_WIDTH, THUMBNAIL_HEIGHT),
                Mode = ResizeMode.Max
            }));

            using var outputStream = new MemoryStream();
            await image.SaveAsync(outputStream, new JpegEncoder { Quality = 75 }, cancellationToken);

            var thumbnailData = outputStream.ToArray();
            var thumbnailFileName = $"thumb_{Path.GetFileNameWithoutExtension(originalFileName)}.jpg";

            var thumbnailPath = await _fileStorage.SaveFileAsync(
                thumbnailFileName,
                thumbnailData,
                "image/jpeg",
                folder,
                cancellationToken
            );

            return thumbnailPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate thumbnail for: {FileName}", originalFileName);
            return null;
        }
    }

    private async Task<(int width, int height)> GetImageDimensionsAsync(byte[] imageData)
    {
        try
        {
            using var stream = new MemoryStream(imageData);
            using var image = await Image.LoadAsync(stream);
            return (image.Width, image.Height);
        }
        catch
        {
            return (0, 0);
        }
    }

    private MediaFileResponse MapToResponse(MediaFile mediaFile)
    {
        return new MediaFileResponse
        {
            Id = mediaFile.Id,
            StoredPath = mediaFile.StoredPath,
            ThumbnailPath = mediaFile.ThumbnailPath,
            OriginalFileName = mediaFile.OriginalFileName,
            StoredFileName = mediaFile.StoredFileName,
            MimeType = mediaFile.MimeType,
            MediaType = mediaFile.MediaType,
            FileSizeBytes = mediaFile.FileSizeBytes,
            Width = mediaFile.Width,
            Height = mediaFile.Height,
            DurationSeconds = mediaFile.DurationSeconds,
            StorageType = mediaFile.StorageType,
            DownloadStatus = mediaFile.DownloadStatus,
            DownloadedAt = mediaFile.DownloadedAt,
            DownloadError = mediaFile.DownloadError,
            UserId = mediaFile.UserId,
            WhatsAppMessageId = mediaFile.WhatsAppMessageId,
            ConversationId = mediaFile.ConversationId,
            CustomerId = mediaFile.CustomerId,
            VisionAnalysis = mediaFile.VisionAnalysis,
            AnalysisConfidence = mediaFile.AnalysisConfidence,
            IsProcessed = mediaFile.IsProcessed,
            ProcessedAt = mediaFile.ProcessedAt,
            IsFlagged = mediaFile.IsFlagged,
            FlagReason = mediaFile.FlagReason,
            PublicUrl = mediaFile.PublicUrl,
            PublicUrlExpiresAt = mediaFile.PublicUrlExpiresAt,
            CreatedAt = mediaFile.CreatedAt
        };
    }

    /// <summary>
    /// Validates that a media URL is from an allowed external source (Twilio).
    /// Prevents SSRF attacks by blocking private/internal network addresses (IPv4 and IPv6).
    /// </summary>
    private static bool IsAllowedMediaUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return false;

        // Only allow HTTPS
        if (!string.Equals(uri.Scheme, "https", StringComparison.OrdinalIgnoreCase))
            return false;

        var host = uri.Host.ToLowerInvariant();

        // Allow Twilio media domains
        if (host.EndsWith(".twilio.com") || host.EndsWith(".twiliocdn.com"))
            return true;

        // Block private/internal IP ranges (IPv4)
        if (host == "localhost" || host == "127.0.0.1")
            return false;
        if (host.StartsWith("192.168.") || host.StartsWith("10.") || host.StartsWith("172."))
            return false;
        if (host == "169.254.169.254") // Cloud metadata endpoint
            return false;
        if (host.EndsWith(".internal") || host.EndsWith(".local"))
            return false;

        // Block private/internal IP ranges (IPv6)
        if (host == "::1" || host == "[::1]") // IPv6 loopback
            return false;

        // Resolve hostname to check for IPv6 private ranges
        if (System.Net.IPAddress.TryParse(host.Trim('[', ']'), out var ipAddress))
        {
            if (ipAddress.IsIPv6LinkLocal || ipAddress.IsIPv6SiteLocal)
                return false;
            if (ipAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
            {
                var bytes = ipAddress.GetAddressBytes();
                // fc00::/7 — Unique Local Addresses
                if ((bytes[0] & 0xFE) == 0xFC)
                    return false;
                // fe80::/10 — Link-Local (already covered by IsIPv6LinkLocal, belt-and-suspenders)
                if (bytes[0] == 0xFE && (bytes[1] & 0xC0) == 0x80)
                    return false;
                // ::ffff:0:0/96 — IPv4-mapped IPv6 (re-check the mapped IPv4)
                if (ipAddress.IsIPv4MappedToIPv6)
                {
                    var mapped = ipAddress.MapToIPv4();
                    var mappedStr = mapped.ToString();
                    if (mappedStr.StartsWith("10.") || mappedStr.StartsWith("192.168.") ||
                        mappedStr.StartsWith("172.") || mappedStr == "127.0.0.1" ||
                        mappedStr == "169.254.169.254")
                        return false;
                }
            }
        }

        // Allow other HTTPS URLs (e.g., user-uploaded content from cloud storage)
        return true;
    }
}
