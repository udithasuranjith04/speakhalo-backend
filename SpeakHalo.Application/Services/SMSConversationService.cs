using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SpeakHalo.Application.DTOs;
using SpeakHalo.Application.Interfaces;
using SpeakHalo.Domain.Entities;
using SpeakHalo.Domain.Enums;
using SpeakHalo.Domain.Interfaces.Repositories;
using SpeakHalo.Shared.Constants;
using SpeakHalo.Shared.Extensions;
using SpeakHalo.Shared.Models;

namespace SpeakHalo.Application.Services;

public class SMSConversationService : ISMSConversationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITwilioService _twilioService;
    private readonly IUsageTrackingService _usageTracking;
    private readonly ICallerRecognitionService _callerRecognition;
    private readonly ILogger<SMSConversationService> _logger;

    public SMSConversationService(IUnitOfWork unitOfWork, ITwilioService twilioService, IUsageTrackingService usageTracking, ICallerRecognitionService callerRecognition, ILogger<SMSConversationService> logger)
    {
        _unitOfWork = unitOfWork;
        _twilioService = twilioService;
        _usageTracking = usageTracking;
        _callerRecognition = callerRecognition;
        _logger = logger;
    }

    public async Task<Result<SMSMessageResponse>> SendSMSAsync(SendSMSRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _unitOfWork.Users.GetByIdAsync(request.UserId, cancellationToken);
            if (user == null)
                return Result<SMSMessageResponse>.Failure(ErrorMessages.NotFound("User"));

            // Look up user's phone number from PhoneNumber entity
            var userPhoneNumbers = await _unitOfWork.Repository<PhoneNumber>()
                .FindAsync(p => p.UserId == request.UserId && p.IsActive && p.SmsEnabled, cancellationToken);
            var userPhone = userPhoneNumbers.FirstOrDefault();
            if (userPhone == null)
                return Result<SMSMessageResponse>.Failure("No SMS-enabled phone number configured for this user");

            var businessPhone = userPhone.Number;

            var conversation = await _unitOfWork.SMSConversations.GetByPhoneNumbersAsync(businessPhone, request.ToNumber, cancellationToken);

            if (conversation == null)
            {
                conversation = await GetOrCreateSMSConversationAsync(request.UserId, businessPhone, request.ToNumber, request.CustomerId, cancellationToken);
            }

            var twilioResult = await _twilioService.SendSmsAsync(request.ToNumber, businessPhone, request.Content, cancellationToken);
            if (!twilioResult.IsSuccess)
                return Result<SMSMessageResponse>.Failure(twilioResult.Errors);

            var message = new SMSMessage
            {
                ConversationId = conversation.Id,
                Direction = MessageDirection.Outbound,
                Content = request.Content,
                Status = MessageStatus.Sent,
                TwilioMessageSid = twilioResult.Data
            };

            await _unitOfWork.SMSMessages.AddAsync(message, cancellationToken);

            conversation.LastMessageAt = DateTime.UtcNow;
            conversation.MessageCount++;
            _unitOfWork.SMSConversations.Update(conversation);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Track SMS usage for billing
            await _usageTracking.TrackSMSMessageAsync(request.UserId);

            return Result<SMSMessageResponse>.Success(MapToMessageResponse(message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send SMS for user {UserId}", request.UserId);
            return Result<SMSMessageResponse>.Failure($"Failed to send SMS: {ex.Message}");
        }
    }

    public async Task<Result<SMSConversationResponse>> GetConversationByIdAsync(string conversationId, CancellationToken cancellationToken = default)
    {
        try
        {
            var conversation = await _unitOfWork.SMSConversations.GetByIdAsync(conversationId, cancellationToken);
            if (conversation == null)
                return Result<SMSConversationResponse>.Failure(ErrorMessages.NotFound("Conversation"));

            return Result<SMSConversationResponse>.Success(MapToConversationResponse(conversation));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get SMS conversation {ConversationId}", conversationId);
            return Result<SMSConversationResponse>.Failure($"Failed to get conversation: {ex.Message}");
        }
    }

    public async Task<Result<SMSConversationDetailResponse>> GetConversationWithMessagesAsync(string conversationId, CancellationToken cancellationToken = default)
    {
        try
        {
            var conversation = await _unitOfWork.SMSConversations.GetWithMessagesAsync(conversationId, cancellationToken);
            if (conversation == null)
                return Result<SMSConversationDetailResponse>.Failure(ErrorMessages.NotFound("Conversation"));

            var response = new SMSConversationDetailResponse
            {
                Id = conversation.Id,
                UserId = conversation.UserId,
                CustomerId = conversation.CustomerId,
                CustomerPhone = conversation.CustomerPhone,
                BusinessPhone = conversation.BusinessPhone,
                IsActive = conversation.IsActive,
                LastMessageAt = conversation.LastMessageAt,
                MessageCount = conversation.MessageCount,
                Messages = conversation.Messages.Select(MapToMessageResponse).ToList(),
                CreatedAt = conversation.CreatedAt
            };

            return Result<SMSConversationDetailResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get SMS conversation with messages {ConversationId}", conversationId);
            return Result<SMSConversationDetailResponse>.Failure($"Failed to get conversation: {ex.Message}");
        }
    }

    public async Task<Result<PaginatedList<SMSConversationResponse>>> GetUserConversationsAsync(string userId, int pageNumber = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        try
        {
            var conversations = await _unitOfWork.SMSConversations.GetByUserIdAsync(userId, cancellationToken);

            var paginatedConversations = await conversations
                .AsQueryable()
                .ToPaginatedListAsync(pageNumber, pageSize, cancellationToken);

            var responses = paginatedConversations.Items.Select(MapToConversationResponse).ToList();

            var result = new PaginatedList<SMSConversationResponse>(
                responses,
                paginatedConversations.TotalCount,
                paginatedConversations.CurrentPage,
                paginatedConversations.PageSize
            );

            return Result<PaginatedList<SMSConversationResponse>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get SMS conversations for user {UserId}", userId);
            return Result<PaginatedList<SMSConversationResponse>>.Failure($"Failed to get conversations: {ex.Message}");
        }
    }

    public async Task<Result<bool>> HandleIncomingSMSAsync(string fromNumber, string toNumber, string content, string twilioSid, CancellationToken cancellationToken = default)
    {
        try
        {
            // Idempotency: check if message with this Twilio SID already exists
            if (!string.IsNullOrEmpty(twilioSid))
            {
                var existing = await _unitOfWork.SMSMessages.GetByTwilioSidAsync(twilioSid, cancellationToken);
                if (existing != null)
                    return Result<bool>.Success(true); // Already processed
            }

            var conversation = await _unitOfWork.SMSConversations.GetByPhoneNumbersAsync(toNumber, fromNumber, cancellationToken);

            if (conversation == null)
            {
                // Look up the user who owns the "To" phone number (must be active)
                var phoneNumbers = await _unitOfWork.Repository<PhoneNumber>()
                    .FindAsync(p => p.Number == toNumber && p.IsActive, cancellationToken);
                var phoneNumber = phoneNumbers.FirstOrDefault();

                string? userId = phoneNumber?.UserId;

                if (string.IsNullOrEmpty(userId))
                {
                    return Result<bool>.Failure($"No business found for phone number {toNumber}");
                }

                conversation = await GetOrCreateSMSConversationAsync(userId, toNumber, fromNumber, null, cancellationToken);
            }

            // Recognize returning customer (opt-in, multi-tenant)
            if (string.IsNullOrEmpty(conversation.CustomerId))
            {
                var recognized = await _callerRecognition.RecognizeCallerAsync(
                    conversation.UserId, fromNumber, "sms", cancellationToken);
                if (recognized != null && recognized.CustomerId != null)
                {
                    conversation.CustomerId = recognized.CustomerId;
                    _unitOfWork.SMSConversations.Update(conversation);
                }
            }

            var message = new SMSMessage
            {
                ConversationId = conversation.Id,
                Direction = MessageDirection.Inbound,
                Content = content,
                Status = MessageStatus.Delivered,
                TwilioMessageSid = twilioSid,
                DeliveredAt = DateTime.UtcNow
            };

            await _unitOfWork.SMSMessages.AddAsync(message, cancellationToken);

            conversation.LastMessageAt = DateTime.UtcNow;
            conversation.MessageCount++;
            _unitOfWork.SMSConversations.Update(conversation);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle incoming SMS from {FromNumber}", fromNumber);
            return Result<bool>.Failure($"Failed to handle incoming SMS: {ex.Message}");
        }
    }

    public async Task<Result<bool>> UpdateMessageStatusAsync(string twilioSid, string status, CancellationToken cancellationToken = default)
    {
        try
        {
            var message = await _unitOfWork.SMSMessages.GetByTwilioSidAsync(twilioSid, cancellationToken);
            if (message == null)
                return Result<bool>.Failure(ErrorMessages.NotFound("Message"));

            message.Status = status.ToLower() switch
            {
                "delivered" => MessageStatus.Delivered,
                "failed" => MessageStatus.Failed,
                "undelivered" => MessageStatus.Failed,
                _ => message.Status
            };

            if (message.Status == MessageStatus.Delivered)
                message.DeliveredAt = DateTime.UtcNow;

            _unitOfWork.SMSMessages.Update(message);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update SMS message status for {TwilioSid}", twilioSid);
            return Result<bool>.Failure($"Failed to update message status: {ex.Message}");
        }
    }

    private static SMSMessageResponse MapToMessageResponse(SMSMessage message)
    {
        return new SMSMessageResponse
        {
            Id = message.Id,
            ConversationId = message.ConversationId,
            Direction = message.Direction,
            Content = message.Content,
            Status = message.Status,
            DeliveredAt = message.DeliveredAt,
            TwilioMessageSid = message.TwilioMessageSid,
            CreatedAt = message.CreatedAt
        };
    }

    private static SMSConversationResponse MapToConversationResponse(SMSConversation conversation)
    {
        return new SMSConversationResponse
        {
            Id = conversation.Id,
            UserId = conversation.UserId,
            CustomerId = conversation.CustomerId,
            CustomerPhone = conversation.CustomerPhone,
            BusinessPhone = conversation.BusinessPhone,
            IsActive = conversation.IsActive,
            LastMessageAt = conversation.LastMessageAt,
            MessageCount = conversation.MessageCount,
            CreatedAt = conversation.CreatedAt
        };
    }

    /// <summary>
    /// Creates a new SMS conversation, handling unique constraint violations from concurrent requests
    /// by re-reading the existing conversation instead.
    /// </summary>
    private async Task<SMSConversation> GetOrCreateSMSConversationAsync(
        string userId, string businessPhone, string customerPhone, string? customerId, CancellationToken ct)
    {
        var conversation = new SMSConversation
        {
            UserId = userId,
            CustomerId = customerId,
            BusinessPhone = businessPhone,
            CustomerPhone = customerPhone,
            IsActive = true,
            MessageCount = 0
        };

        try
        {
            await _unitOfWork.SMSConversations.AddAsync(conversation, ct);
            await _unitOfWork.SaveChangesAsync(ct);
            return conversation;
        }
        catch (DbUpdateException)
        {
            // Unique constraint violation — another request created it first; re-read
            var existing = await _unitOfWork.SMSConversations.GetByPhoneNumbersAsync(businessPhone, customerPhone, ct);
            return existing ?? throw new InvalidOperationException("Failed to create or find SMS conversation");
        }
    }
}
