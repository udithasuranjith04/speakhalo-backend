using SpeakHalo.Application.DTOs;
using SpeakHalo.Shared.Models;

namespace SpeakHalo.Application.Interfaces;

public interface ISMSConversationService
{
    Task<Result<SMSMessageResponse>> SendSMSAsync(SendSMSRequest request, CancellationToken cancellationToken = default);
    Task<Result<SMSConversationResponse>> GetConversationByIdAsync(string conversationId, CancellationToken cancellationToken = default);
    Task<Result<SMSConversationDetailResponse>> GetConversationWithMessagesAsync(string conversationId, CancellationToken cancellationToken = default);
    Task<Result<PaginatedList<SMSConversationResponse>>> GetUserConversationsAsync(string userId, int pageNumber = 1, int pageSize = 20, string? search = null, CancellationToken cancellationToken = default);
    Task<Result<bool>> HandleIncomingSMSAsync(string fromNumber, string toNumber, string content, string twilioSid, CancellationToken cancellationToken = default);
    Task<Result<bool>> UpdateMessageStatusAsync(string twilioSid, string status, CancellationToken cancellationToken = default);
}
