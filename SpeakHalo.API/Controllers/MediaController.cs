using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpeakHalo.API.Filters;
using SpeakHalo.Application.DTOs;
using SpeakHalo.Application.Interfaces;
using SpeakHalo.Domain.Entities;
using SpeakHalo.Domain.Enums;
using SpeakHalo.Domain.Interfaces.Repositories;

namespace SpeakHalo.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/media")]
[Authorize]
public class MediaController : ControllerBase
{
    private readonly IMediaService _mediaService;
    private readonly IUsageTrackingService _usageTracking;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<MediaController> _logger;

    public MediaController(
        IMediaService mediaService,
        IUsageTrackingService usageTracking,
        IUnitOfWork unitOfWork,
        ILogger<MediaController> logger)
    {
        _mediaService = mediaService;
        _usageTracking = usageTracking;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    [HttpPost("download")]
    [ProducesResponseType(typeof(MediaDownloadResult), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> DownloadMedia(
        [FromBody] DownloadMediaRequest request,
        CancellationToken cancellationToken)
    {
        // Validate media URL domain to prevent SSRF
        if (string.IsNullOrWhiteSpace(request.MediaUrl) ||
            !Uri.TryCreate(request.MediaUrl, UriKind.Absolute, out var mediaUri) ||
            !IsAllowedMediaDomain(mediaUri))
        {
            return BadRequest(new { error = "Media URL must be a valid HTTPS URL from an allowed domain (e.g. Twilio)" });
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var securedRequest = request with { UserId = userId };

        var result = await _mediaService.DownloadAndStoreMediaAsync(securedRequest, cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }

        if (request.ProcessWithAI)
        {
            await _usageTracking.TrackImageAnalysisAsync(userId, securedRequest.MediaUrl ?? "");
        }

        return Ok(result);
    }

    [HttpPost("upload")]
    [ProducesResponseType(typeof(MediaUploadResult), 200)]
    [ProducesResponseType(400)]
    [RequestSizeLimit(104857600)] // 100MB
    public async Task<IActionResult> UploadMedia(
        [FromForm] IFormFile file,
        [FromForm] string? conversationId,
        [FromForm] string? customerId,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        if (file == null || file.Length == 0)
        {
            return BadRequest("No file provided");
        }

        using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream, cancellationToken);

        var request = new UploadMediaRequest
        {
            UserId = userId,
            FileName = file.FileName,
            ContentType = file.ContentType,
            FileData = memoryStream.ToArray(),
            ConversationId = conversationId,
            CustomerId = customerId
        };

        var result = await _mediaService.UploadMediaAsync(request, cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpGet("{mediaFileId}")]
    [ProducesResponseType(typeof(MediaFileResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetMediaFile(
        string mediaFileId,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        if (!await UserOwnsMediaAsync(mediaFileId, userId))
        {
            return NotFound(new { message = "Media file not found" });
        }

        var result = await _mediaService.GetMediaFileAsync(mediaFileId, cancellationToken);

        if (!result.IsSuccess)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    [HttpGet("conversation/{conversationId}")]
    [ProducesResponseType(typeof(List<MediaFileResponse>), 200)]
    public async Task<IActionResult> GetConversationMedia(
        string conversationId,
        [FromQuery] MediaType? mediaType = null,
        CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var conversationOwned = await _unitOfWork.Repository<WhatsAppConversation>()
            .AnyAsync(c => c.Id == conversationId && c.UserId == userId, cancellationToken);

        if (!conversationOwned)
        {
            return NotFound(new { message = "Conversation not found" });
        }

        var result = await _mediaService.GetConversationMediaAsync(conversationId, mediaType, cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpGet("user")]
    [ProducesResponseType(typeof(List<MediaFileResponse>), 200)]
    public async Task<IActionResult> GetUserMedia(
        [FromQuery] MediaType? mediaType = null,
        [FromQuery] int limit = 100,
        CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var result = await _mediaService.GetUserMediaAsync(userId, mediaType, limit, cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpPost("{mediaFileId}/public-url")]
    [ProducesResponseType(typeof(PublicUrlResult), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GeneratePublicUrl(
        string mediaFileId,
        [FromBody] GeneratePublicUrlRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        if (!await UserOwnsMediaAsync(mediaFileId, userId))
        {
            return NotFound(new { message = "Media file not found" });
        }

        var urlRequest = request ?? new GeneratePublicUrlRequest
        {
            MediaFileId = mediaFileId,
            ExpirationMinutes = 60
        };

        if (urlRequest.MediaFileId != mediaFileId)
        {
            urlRequest = urlRequest with { MediaFileId = mediaFileId };
        }

        var result = await _mediaService.GeneratePublicUrlAsync(urlRequest, cancellationToken);

        if (!result.IsSuccess)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    [HttpPost("{mediaFileId}/process")]
    [RequireFeature(PlanFeature.AIPhotoQuotes)]
    [ProducesResponseType(typeof(MediaProcessingResult), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> ProcessMedia(
        string mediaFileId,
        [FromBody] ProcessMediaRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        if (!await UserOwnsMediaAsync(mediaFileId, userId))
        {
            return NotFound(new { message = "Media file not found" });
        }

        var processRequest = request ?? new ProcessMediaRequest
        {
            MediaFileId = mediaFileId,
            AnalyzeWithVision = true,
            GenerateThumbnail = true,
            ExtractMetadata = true
        };

        if (processRequest.MediaFileId != mediaFileId)
        {
            processRequest = processRequest with { MediaFileId = mediaFileId };
        }

        var result = await _mediaService.ProcessMediaAsync(processRequest, cancellationToken);

        if (!result.IsSuccess)
        {
            return NotFound(result);
        }

        if (processRequest.AnalyzeWithVision)
        {
            await _usageTracking.TrackImageAnalysisAsync(userId, mediaFileId);
        }

        return Ok(result);
    }

    [HttpGet("stats")]
    [ProducesResponseType(typeof(MediaStorageStats), 200)]
    public async Task<IActionResult> GetStorageStats(
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var result = await _mediaService.GetStorageStatsAsync(userId, cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpDelete("{mediaFileId}")]
    [ProducesResponseType(typeof(bool), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> DeleteMedia(
        string mediaFileId,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        if (!await UserOwnsMediaAsync(mediaFileId, userId))
        {
            return NotFound(new { message = "Media file not found" });
        }

        var result = await _mediaService.DeleteMediaAsync(mediaFileId, cancellationToken);

        if (!result.IsSuccess)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    [HttpPost("{mediaFileId}/flag")]
    [ProducesResponseType(typeof(bool), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> FlagMedia(
        string mediaFileId,
        [FromBody] string reason,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        if (!await UserOwnsMediaAsync(mediaFileId, userId))
        {
            return NotFound(new { message = "Media file not found" });
        }

        var result = await _mediaService.FlagMediaAsync(mediaFileId, reason, cancellationToken);

        if (!result.IsSuccess)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    [HttpGet("flagged")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(List<MediaFileResponse>), 200)]
    public async Task<IActionResult> GetFlaggedMedia(
        CancellationToken cancellationToken)
    {
        var result = await _mediaService.GetFlaggedMediaAsync(cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpPost("process-pending")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(int), 200)]
    public async Task<IActionResult> ProcessPendingDownloads(
        [FromQuery] int batchSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediaService.ProcessPendingDownloadsAsync(batchSize, cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }

        return Ok(new { ProcessedCount = result.Data });
    }

    [HttpPost("cleanup")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(int), 200)]
    public async Task<IActionResult> CleanupOldMedia(
        [FromQuery] int daysToKeep = 90,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediaService.CleanupOldMediaAsync(daysToKeep, cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }

        return Ok(new { DeletedCount = result.Data });
    }

    private async Task<bool> UserOwnsMediaAsync(string mediaFileId, string userId)
    {
        var items = await _unitOfWork.Repository<MediaFile>().FindAsync(m => m.Id == mediaFileId && m.UserId == userId);
        return items.Any();
    }

    private static readonly string[] AllowedMediaDomains =
    {
        "api.twilio.com",
        "media.twilio.com",
        "mms.twilio.com"
    };

    private static bool IsAllowedMediaDomain(Uri uri)
    {
        if (uri.Scheme != "https")
            return false;

        var host = uri.Host.ToLowerInvariant();
        return AllowedMediaDomains.Any(d => host == d || host.EndsWith("." + d));
    }
}
