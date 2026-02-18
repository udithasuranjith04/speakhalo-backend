using SpeakHalo.Application.DTOs;
using SpeakHalo.Shared.Models;

namespace SpeakHalo.Application.Interfaces;

public interface IWhatsAppService
{
    Task<Result<WhatsAppMessageResponse>> SendMessageAsync(SendWhatsAppMessageRequest request, CancellationToken cancellationToken = default);
    Task<Result<WhatsAppConversationResponse>> GetConversationByIdAsync(string conversationId, CancellationToken cancellationToken = default);
    Task<Result<WhatsAppConversationDetailResponse>> GetConversationWithMessagesAsync(string conversationId, int limit = 50, CancellationToken cancellationToken = default);
    Task<Result<PaginatedList<WhatsAppConversationResponse>>> GetUserConversationsAsync(string userId, int pageNumber = 1, int pageSize = 20, CancellationToken cancellationToken = default);
    Task<Result<bool>> HandleIncomingMessageAsync(string fromNumber, string toNumber, string content, string twilioSid, string? mediaUrl = null, string? mediaType = null, CancellationToken cancellationToken = default);
    Task<Result<bool>> UpdateMessageStatusAsync(string twilioSid, string status, CancellationToken cancellationToken = default);
    Task<Result<bool>> MarkMessagesAsReadAsync(string conversationId, CancellationToken cancellationToken = default);
    Task<Result<WhatsAppConversationResponse>> UpdateConversationAsync(string conversationId, UpdateWhatsAppConversationRequest request, CancellationToken cancellationToken = default);
}
