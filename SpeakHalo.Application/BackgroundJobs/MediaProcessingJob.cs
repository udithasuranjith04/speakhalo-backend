using Microsoft.Extensions.Logging;
using SpeakHalo.Application.Interfaces;

namespace SpeakHalo.Application.BackgroundJobs;

/// <summary>
/// Background job for media processing tasks
/// - Downloads pending media from Twilio
/// - Processes images with AI vision
/// - Generates thumbnails
/// - Cleans up old media files
/// </summary>
public class MediaProcessingJob
{
    private readonly IMediaService _mediaService;
    private readonly ILogger<MediaProcessingJob> _logger;

    public MediaProcessingJob(
        IMediaService mediaService,
        ILogger<MediaProcessingJob> logger)
    {
        _mediaService = mediaService;
        _logger = logger;
    }

    /// <summary>
    /// Process pending media downloads
    /// Runs every 5 minutes to download and store WhatsApp media
    /// </summary>
    public async Task ProcessPendingDownloadsAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Starting pending media downloads processing");

            var result = await _mediaService.ProcessPendingDownloadsAsync(
                batchSize: 20,
                cancellationToken: cancellationToken
            );

            if (result.IsSuccess)
            {
                _logger.LogInformation(
                    "Processed {Count} pending media downloads",
                    result.Data
                );
            }
            else
            {
                _logger.LogError(
                    "Failed to process pending downloads: {Errors}",
                    string.Join(", ", result.Errors)
                );
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Media download processing cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing pending media downloads");
        }
    }

    /// <summary>
    /// Cleanup old media files
    /// Runs daily to delete media older than 90 days
    /// </summary>
    public async Task CleanupOldMediaAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Starting old media cleanup");

            var result = await _mediaService.CleanupOldMediaAsync(
                daysToKeep: 90,
                cancellationToken: cancellationToken
            );

            if (result.IsSuccess)
            {
                _logger.LogInformation(
                    "Cleaned up {Count} old media files",
                    result.Data
                );
            }
            else
            {
                _logger.LogError(
                    "Failed to cleanup old media: {Errors}",
                    string.Join(", ", result.Errors)
                );
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Media cleanup cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up old media");
        }
    }
}
