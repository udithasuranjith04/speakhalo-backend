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
[RequireFeature(PlanFeature.SMS)]
[RequireApiScope("messaging")]
public class SMSController : ControllerBase
{
    private readonly ISMSConversationService _smsService;
    private readonly IUnitOfWork _unitOfWork;

    public SMSController(
        ISMSConversationService smsService,
        IUnitOfWork unitOfWork)
    {
        _smsService = smsService;
        _unitOfWork = unitOfWork;
    }

    private async Task<bool> UserOwnsConversationAsync(string conversationId, string userId)
    {
        var items = await _unitOfWork.Repository<SMSConversation>().FindAsync(c => c.Id == conversationId && c.UserId == userId);
        return items.Any();
    }

    [HttpPost("send")]
    public async Task<IActionResult> SendMessage([FromBody] SendSMSRequest request, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var requestWithUserId = request with { UserId = userId };

        var result = await _smsService.SendSMSAsync(requestWithUserId, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpGet("conversations")]
    public async Task<IActionResult> GetConversations([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 50, CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _smsService.GetUserConversationsAsync(userId, pageNumber, pageSize, cancellationToken);

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

        var result = await _smsService.GetConversationByIdAsync(conversationId, cancellationToken);

        if (!result.IsSuccess)
            return NotFound(result);

        return Ok(result);
    }

    [HttpGet("conversations/{conversationId}/messages")]
    public async Task<IActionResult> GetConversationMessages(string conversationId, CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        if (!await UserOwnsConversationAsync(conversationId, userId))
            return NotFound(new { message = "Conversation not found." });

        var result = await _smsService.GetConversationWithMessagesAsync(conversationId, cancellationToken);

        if (!result.IsSuccess)
            return NotFound(result);

        return Ok(result);
    }

    [HttpPost("conversations/{conversationId}/read")]
    public async Task<IActionResult> MarkAsRead(string conversationId, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        if (!await UserOwnsConversationAsync(conversationId, userId))
            return NotFound(new { message = "Conversation not found." });

        // Mark all messages in this conversation as read
        var conversationResult = await _smsService.GetConversationWithMessagesAsync(conversationId, cancellationToken);
        if (!conversationResult.IsSuccess)
            return BadRequest(conversationResult);

        // Update unread inbound messages to Read status
        var messages = await _unitOfWork.SMSMessages.GetByConversationIdAsync(conversationId, cancellationToken);
        foreach (var msg in messages.Where(m => m.Direction == MessageDirection.Inbound && m.Status != MessageStatus.Read))
        {
            msg.Status = MessageStatus.Read;
            _unitOfWork.SMSMessages.Update(msg);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(new { isSuccess = true, message = "Messages marked as read." });
    }
}
