using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpeakHalo.API.Filters;
using SpeakHalo.Application.DTOs;
using SpeakHalo.Application.Interfaces;
using SpeakHalo.Domain.Entities;
using SpeakHalo.Domain.Enums;
using SpeakHalo.Domain.Interfaces.Repositories;
using System.Security.Claims;

namespace SpeakHalo.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
[RequireFeature(PlanFeature.WhatsApp)]
[RequireApiScope("messaging")]
public class WhatsAppController : ControllerBase
{
    private readonly IWhatsAppService _whatsAppService;
    private readonly IUsageTrackingService _usageTracking;
    private readonly IUnitOfWork _unitOfWork;

    public WhatsAppController(
        IWhatsAppService whatsAppService,
        IUsageTrackingService usageTracking,
        IUnitOfWork unitOfWork)
    {
        _whatsAppService = whatsAppService;
        _usageTracking = usageTracking;
        _unitOfWork = unitOfWork;
    }

    private async Task<bool> UserOwnsConversationAsync(string conversationId, string userId)
    {
        var items = await _unitOfWork.Repository<WhatsAppConversation>().FindAsync(c => c.Id == conversationId && c.UserId == userId);
        return items.Any();
    }

    [HttpPost("send")]
    public async Task<IActionResult> SendMessage([FromBody] SendWhatsAppMessageRequest request, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var requestWithUserId = request with { UserId = userId };

        var result = await _whatsAppService.SendMessageAsync(requestWithUserId, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(result);

        // Note: Usage tracking is handled inside WhatsAppService.SendMessageAsync
        return Ok(result);
    }

    [HttpGet("conversations")]
    public async Task<IActionResult> GetConversations([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, [FromQuery] string? search = null, CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _whatsAppService.GetUserConversationsAsync(userId, pageNumber, pageSize, search, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpGet("conversations/{conversationId}")]
    public async Task<IActionResult> GetConversation(string conversationId, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        if (!await UserOwnsConversationAsync(conversationId, userId))
            return NotFound(new { message = "Conversation not found." });

        var result = await _whatsAppService.GetConversationByIdAsync(conversationId, cancellationToken);

        if (!result.IsSuccess)
            return NotFound(result);

        return Ok(result);
    }

    [HttpGet("conversations/{conversationId}/messages")]
    public async Task<IActionResult> GetConversationMessages(string conversationId, [FromQuery] int limit = 50, CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        if (!await UserOwnsConversationAsync(conversationId, userId))
            return NotFound(new { message = "Conversation not found." });

        var result = await _whatsAppService.GetConversationWithMessagesAsync(conversationId, limit, cancellationToken);

        if (!result.IsSuccess)
            return NotFound(result);

        return Ok(result);
    }

    [HttpPatch("conversations/{conversationId}")]
    public async Task<IActionResult> UpdateConversation(string conversationId, [FromBody] UpdateWhatsAppConversationRequest request, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        if (!await UserOwnsConversationAsync(conversationId, userId))
            return NotFound(new { message = "Conversation not found." });

        var result = await _whatsAppService.UpdateConversationAsync(conversationId, request, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPost("conversations/{conversationId}/read")]
    public async Task<IActionResult> MarkAsRead(string conversationId, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        if (!await UserOwnsConversationAsync(conversationId, userId))
            return NotFound(new { message = "Conversation not found." });

        var result = await _whatsAppService.MarkMessagesAsReadAsync(conversationId, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpDelete("conversations/{conversationId}")]
    public async Task<IActionResult> DeleteConversation(string conversationId, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        if (!await UserOwnsConversationAsync(conversationId, userId))
            return NotFound(new { message = "Conversation not found." });

        var messages = await _unitOfWork.WhatsAppMessages.GetByConversationIdAsync(conversationId, cancellationToken: cancellationToken);
        _unitOfWork.Repository<WhatsAppMessage>().HardDeleteRange(messages);

        var conversation = await _unitOfWork.WhatsAppConversations.GetByIdAsync(conversationId, cancellationToken);
        if (conversation != null)
            _unitOfWork.Repository<WhatsAppConversation>().HardDelete(conversation);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(new { isSuccess = true, message = "Conversation deleted successfully." });
    }

    [HttpPost("webhook")]
    [AllowAnonymous]
    [ValidateTwilioSignature]
    public async Task<IActionResult> TwilioWebhook([FromForm] TwilioWhatsAppWebhook webhook, CancellationToken cancellationToken)
    {
        var result = await _whatsAppService.HandleIncomingMessageAsync(
            webhook.From,
            webhook.To,
            webhook.Body,
            webhook.MessageSid,
            webhook.MediaUrl0,
            webhook.MediaContentType0,
            cancellationToken
        );

        if (!result.IsSuccess)
            return BadRequest();

        return Ok();
    }

    [HttpPost("webhook/status")]
    [AllowAnonymous]
    [ValidateTwilioSignature]
    public async Task<IActionResult> TwilioStatusWebhook([FromForm] TwilioStatusWebhook webhook, CancellationToken cancellationToken)
    {
        var result = await _whatsAppService.UpdateMessageStatusAsync(webhook.MessageSid, webhook.MessageStatus, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest();

        return Ok();
    }
}

public record TwilioWhatsAppWebhook
{
    public string From { get; init; } = string.Empty;
    public string To { get; init; } = string.Empty;
    public string Body { get; init; } = string.Empty;
    public string MessageSid { get; init; } = string.Empty;
    public string? MediaUrl0 { get; init; }
    public string? MediaContentType0 { get; init; }
}

public record TwilioStatusWebhook
{
    public string MessageSid { get; init; } = string.Empty;
    public string MessageStatus { get; init; } = string.Empty;
}
