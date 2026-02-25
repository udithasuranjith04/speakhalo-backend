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

public class WhatsAppService : IWhatsAppService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITwilioService _twilioService;
    private readonly IWhatsAppPhotoQuoteService _photoQuoteService;
    private readonly IMediaService _mediaService;
    private readonly IUsageTrackingService _usageTracking;
    private readonly ICallerRecognitionService _callerRecognition;
    private readonly ILogger<WhatsAppService> _logger;

    public WhatsAppService(
        IUnitOfWork unitOfWork,
        ITwilioService twilioService,
        IWhatsAppPhotoQuoteService photoQuoteService,
        IMediaService mediaService,
        IUsageTrackingService usageTracking,
        ICallerRecognitionService callerRecognition,
        ILogger<WhatsAppService> logger)
    {
        _unitOfWork = unitOfWork;
        _twilioService = twilioService;
        _photoQuoteService = photoQuoteService;
        _mediaService = mediaService;
        _usageTracking = usageTracking;
        _callerRecognition = callerRecognition;
        _logger = logger;
    }

    public async Task<Result<WhatsAppMessageResponse>> SendMessageAsync(SendWhatsAppMessageRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _unitOfWork.Users.GetByIdAsync(request.UserId, cancellationToken);
            if (user == null)
                return Result<WhatsAppMessageResponse>.Failure(ErrorMessages.NotFound("User"));

            var userPhoneNumbers = await _unitOfWork.Repository<PhoneNumber>()
                .FindAsync(p => p.UserId == request.UserId && p.IsActive && p.WhatsAppEnabled, cancellationToken);
            var userPhone = userPhoneNumbers.FirstOrDefault();
            if (userPhone == null)
                return Result<WhatsAppMessageResponse>.Failure("No WhatsApp-enabled phone number configured for this user");

            var businessPhone = $"whatsapp:{userPhone.Number}";
            var customerPhone = request.ToNumber.StartsWith("whatsapp:") ? request.ToNumber : $"whatsapp:{request.ToNumber}";

            var conversation = await _unitOfWork.WhatsAppConversations.GetByPhoneNumbersAsync(businessPhone, customerPhone, cancellationToken);

            if (conversation == null)
            {
                conversation = await GetOrCreateWhatsAppConversationAsync(request.UserId, businessPhone, customerPhone, request.CustomerId, cancellationToken);
            }

            // Hard-limit check: Starter users are blocked when their monthly WhatsApp quota is exhausted
            if (await _usageTracking.IsHardLimitBlockedAsync(request.UserId, UsageType.WhatsAppMessages))
                return Result<WhatsAppMessageResponse>.Failure("Monthly WhatsApp limit reached. Upgrade to Professional or Business to continue sending messages.");

            var twilioResult = await _twilioService.SendWhatsAppMessageAsync(customerPhone, businessPhone, request.Content, cancellationToken);
            if (!twilioResult.IsSuccess)
                return Result<WhatsAppMessageResponse>.Failure(twilioResult.Errors);

            var message = new WhatsAppMessage
            {
                ConversationId = conversation.Id,
                Direction = MessageDirection.Outbound,
                Content = request.Content,
                MediaUrl = request.MediaUrl,
                MediaType = request.MediaType,
                Status = MessageStatus.Sent,
                TwilioMessageSid = twilioResult.Data,
                IsAiGenerated = false
            };

            await _unitOfWork.WhatsAppMessages.AddAsync(message, cancellationToken);

            conversation.LastMessageAt = DateTime.UtcNow;
            conversation.MessageCount++;
            _unitOfWork.WhatsAppConversations.Update(conversation);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _usageTracking.TrackWhatsAppMessageAsync(request.UserId);

            return Result<WhatsAppMessageResponse>.Success(MapToMessageResponse(message));
        }
        catch (Exception ex)
        {
            return Result<WhatsAppMessageResponse>.Failure($"Failed to send WhatsApp message: {ex.Message}");
        }
    }

    public async Task<Result<WhatsAppConversationResponse>> GetConversationByIdAsync(string conversationId, CancellationToken cancellationToken = default)
    {
        try
        {
            var conversation = await _unitOfWork.WhatsAppConversations.GetByIdAsync(conversationId, cancellationToken);
            if (conversation == null)
                return Result<WhatsAppConversationResponse>.Failure(ErrorMessages.NotFound("Conversation"));

            return Result<WhatsAppConversationResponse>.Success(MapToConversationResponse(conversation));
        }
        catch (Exception ex)
        {
            return Result<WhatsAppConversationResponse>.Failure($"Failed to get conversation: {ex.Message}");
        }
    }

    public async Task<Result<WhatsAppConversationDetailResponse>> GetConversationWithMessagesAsync(string conversationId, int limit = 50, CancellationToken cancellationToken = default)
    {
        try
        {
            var conversation = await _unitOfWork.WhatsAppConversations.GetWithMessagesAsync(conversationId, limit, cancellationToken);
            if (conversation == null)
                return Result<WhatsAppConversationDetailResponse>.Failure(ErrorMessages.NotFound("Conversation"));

            var response = new WhatsAppConversationDetailResponse
            {
                Id = conversation.Id,
                UserId = conversation.UserId,
                CustomerId = conversation.CustomerId,
                CustomerPhone = conversation.CustomerPhone,
                CustomerName = conversation.CustomerName,
                BusinessPhone = conversation.BusinessPhone,
                IsActive = conversation.IsActive,
                LastMessageAt = conversation.LastMessageAt,
                MessageCount = conversation.MessageCount,
                UnreadCount = conversation.UnreadCount,
                AiMode = conversation.AiMode,
                Summary = conversation.Summary,
                Language = conversation.Language,
                Messages = conversation.Messages.Select(MapToMessageResponse).ToList(),
                CreatedAt = conversation.CreatedAt
            };

            return Result<WhatsAppConversationDetailResponse>.Success(response);
        }
        catch (Exception ex)
        {
            return Result<WhatsAppConversationDetailResponse>.Failure($"Failed to get conversation: {ex.Message}");
        }
    }

    public async Task<Result<PaginatedList<WhatsAppConversationResponse>>> GetUserConversationsAsync(string userId, int pageNumber = 1, int pageSize = 20, string? search = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var conversations = await _unitOfWork.WhatsAppConversations.GetByUserIdAsync(userId, cancellationToken);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                conversations = conversations.Where(c =>
                    (c.CustomerName != null && c.CustomerName.Contains(s, StringComparison.OrdinalIgnoreCase)) ||
                    (c.CustomerPhone != null && c.CustomerPhone.Contains(s, StringComparison.OrdinalIgnoreCase))
                ).ToList();
            }

            var paginatedConversations = await conversations
                .AsQueryable()
                .ToPaginatedListAsync(pageNumber, pageSize, cancellationToken);

            var responses = paginatedConversations.Items.Select(MapToConversationResponse).ToList();

            var result = new PaginatedList<WhatsAppConversationResponse>(
                responses,
                paginatedConversations.TotalCount,
                paginatedConversations.CurrentPage,
                paginatedConversations.PageSize
            );

            return Result<PaginatedList<WhatsAppConversationResponse>>.Success(result);
        }
        catch (Exception ex)
        {
            return Result<PaginatedList<WhatsAppConversationResponse>>.Failure($"Failed to get conversations: {ex.Message}");
        }
    }

    public async Task<Result<bool>> HandleIncomingMessageAsync(string fromNumber, string toNumber, string content, string twilioSid, string? mediaUrl = null, string? mediaType = null, CancellationToken cancellationToken = default)
    {
        try
        {
            // Idempotency: skip if this Twilio SID was already processed
            if (!string.IsNullOrEmpty(twilioSid))
            {
                var existing = await _unitOfWork.WhatsAppMessages.GetByTwilioSidAsync(twilioSid, cancellationToken);
                if (existing != null)
                    return Result<bool>.Success(true);
            }

            var conversation = await _unitOfWork.WhatsAppConversations.GetByPhoneNumbersAsync(toNumber, fromNumber, cancellationToken);

            if (conversation == null)
            {
                // Strip whatsapp: prefix -- PhoneNumber.Number stores E.164 format without prefix
                var normalizedToNumber = toNumber.Replace("whatsapp:", "");
                var phoneNumbers = await _unitOfWork.Repository<PhoneNumber>()
                    .FindAsync(p => p.Number == normalizedToNumber && p.IsActive, cancellationToken);
                var phoneNumber = phoneNumbers.FirstOrDefault();

                string? userId = phoneNumber?.UserId;

                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("No phone number record found for {ToNumber}, unable to route WhatsApp message", toNumber);
                    return Result<bool>.Failure($"No business found for phone number {toNumber}");
                }

                conversation = await GetOrCreateWhatsAppConversationAsync(userId, toNumber, fromNumber, null, cancellationToken);
            }

            if (string.IsNullOrEmpty(conversation.CustomerId))
            {
                var recognized = await _callerRecognition.RecognizeCallerAsync(
                    conversation.UserId, fromNumber, "whatsapp", cancellationToken);
                if (recognized != null && recognized.CustomerId != null)
                {
                    conversation.CustomerId = recognized.CustomerId;
                    conversation.CustomerName = recognized.Name;
                    _unitOfWork.WhatsAppConversations.Update(conversation);
                }
            }

            var message = new WhatsAppMessage
            {
                ConversationId = conversation.Id,
                Direction = MessageDirection.Inbound,
                Content = content,
                MediaUrl = mediaUrl,
                MediaType = mediaType,
                Status = MessageStatus.Delivered,
                TwilioMessageSid = twilioSid,
                DeliveredAt = DateTime.UtcNow,
                IsAiGenerated = false
            };

            await _unitOfWork.WhatsAppMessages.AddAsync(message, cancellationToken);

            conversation.LastMessageAt = DateTime.UtcNow;
            conversation.MessageCount++;
            conversation.UnreadCount++;
            _unitOfWork.WhatsAppConversations.Update(conversation);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            if (!string.IsNullOrEmpty(mediaUrl))
            {
                try
                {
                    var downloadRequest = new DownloadMediaRequest
                    {
                        MediaUrl = mediaUrl,
                        UserId = conversation.UserId,
                        WhatsAppMessageId = message.Id,
                        ConversationId = conversation.Id,
                        CustomerId = conversation.CustomerId ?? string.Empty,
                        ProcessWithAI = mediaType?.StartsWith("image/") == true,
                        GenerateThumbnail = true
                    };

                    var downloadResult = await _mediaService.DownloadAndStoreMediaAsync(
                        downloadRequest,
                        cancellationToken
                    );

                    if (downloadResult.IsSuccess)
                    {
                        _logger.LogInformation(
                            "Media downloaded and stored: {MediaFileId}, Type: {MediaType}",
                            downloadResult.Data!.MediaFileId,
                            downloadResult.Data.MediaType
                        );
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to download media from {MediaUrl}", mediaUrl);
                    // Don't fail the whole request if media download fails
                }
            }

            if (!string.IsNullOrEmpty(mediaUrl) && mediaType?.StartsWith("image/") == true)
            {
                try
                {
                    var photoQuoteRequest = new PhotoQuoteRequest
                    {
                        ConversationId = conversation.Id,
                        UserId = conversation.UserId,
                        ImageUrls = new List<string> { mediaUrl },
                        CustomerDescription = content
                    };

                    var photoQuoteResult = await _photoQuoteService.ProcessPhotoQuoteAsync(
                        photoQuoteRequest,
                        cancellationToken
                    );

                    if (photoQuoteResult.IsSuccess && photoQuoteResult.Data != null)
                    {
                        var quoteData = photoQuoteResult.Data;

                        await _usageTracking.TrackImageAnalysisAsync(conversation.UserId, mediaUrl);

                        message.AiContext = System.Text.Json.JsonSerializer.Serialize(new
                        {
                            confidence = quoteData.ConfidenceScore,
                            workflow = quoteData.Workflow,
                            analysis = quoteData.VisionAnalysis,
                            quote = quoteData.Quote
                        });
                        _unitOfWork.WhatsAppMessages.Update(message);

                        var aiResponse = quoteData.CustomerMessage;

                        var twilioSendResult = await _twilioService.SendWhatsAppMessageAsync(
                            fromNumber,
                            toNumber,
                            aiResponse,
                            cancellationToken
                        );

                        if (twilioSendResult.IsSuccess)
                        {
                            var aiMessage = new WhatsAppMessage
                            {
                                ConversationId = conversation.Id,
                                Direction = MessageDirection.Outbound,
                                Content = aiResponse,
                                Status = MessageStatus.Sent,
                                TwilioMessageSid = twilioSendResult.Data,
                                IsAiGenerated = true,
                                AiContext = $"Photo quote workflow: {quoteData.Workflow}"
                            };

                            await _unitOfWork.WhatsAppMessages.AddAsync(aiMessage, cancellationToken);

                            conversation.LastMessageAt = DateTime.UtcNow;
                            conversation.MessageCount++;
                            _unitOfWork.WhatsAppConversations.Update(conversation);

                            await _unitOfWork.SaveChangesAsync(cancellationToken);

                            await _usageTracking.TrackWhatsAppMessageAsync(conversation.UserId);

                            _logger.LogInformation(
                                "Photo quote processed successfully. Conversation: {ConversationId}, Workflow: {Workflow}, Confidence: {Confidence}%",
                                conversation.Id,
                                quoteData.Workflow,
                                quoteData.ConfidenceScore
                            );
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process photo quote for conversation {ConversationId}", conversation.Id);
                    // Don't fail the whole request if photo processing fails
                }
            }

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return Result<bool>.Failure($"Failed to handle incoming message: {ex.Message}");
        }
    }

    public async Task<Result<bool>> UpdateMessageStatusAsync(string twilioSid, string status, CancellationToken cancellationToken = default)
    {
        try
        {
            var message = await _unitOfWork.WhatsAppMessages.GetByTwilioSidAsync(twilioSid, cancellationToken);
            if (message == null)
                return Result<bool>.Failure(ErrorMessages.NotFound("Message"));

            message.Status = status.ToLower() switch
            {
                "delivered" => MessageStatus.Delivered,
                "read" => MessageStatus.Read,
                "failed" => MessageStatus.Failed,
                "undelivered" => MessageStatus.Failed,
                _ => message.Status
            };

            if (message.Status == MessageStatus.Delivered && message.DeliveredAt == null)
                message.DeliveredAt = DateTime.UtcNow;

            if (message.Status == MessageStatus.Read && message.ReadAt == null)
                message.ReadAt = DateTime.UtcNow;

            _unitOfWork.WhatsAppMessages.Update(message);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return Result<bool>.Failure($"Failed to update message status: {ex.Message}");
        }
    }

    public async Task<Result<bool>> MarkMessagesAsReadAsync(string conversationId, CancellationToken cancellationToken = default)
    {
        try
        {
            var conversation = await _unitOfWork.WhatsAppConversations.GetByIdAsync(conversationId, cancellationToken);
            if (conversation == null)
                return Result<bool>.Failure(ErrorMessages.NotFound("Conversation"));

            var messages = await _unitOfWork.WhatsAppMessages.GetByConversationIdAsync(conversationId, cancellationToken: cancellationToken);
            var unreadMessages = messages.Where(m => m.ReadAt == null && m.Direction == MessageDirection.Inbound).ToList();

            foreach (var message in unreadMessages)
            {
                message.ReadAt = DateTime.UtcNow;
                message.Status = MessageStatus.Read;
                _unitOfWork.WhatsAppMessages.Update(message);
            }

            conversation.UnreadCount = 0;
            _unitOfWork.WhatsAppConversations.Update(conversation);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return Result<bool>.Failure($"Failed to mark messages as read: {ex.Message}");
        }
    }

    public async Task<Result<WhatsAppConversationResponse>> UpdateConversationAsync(string conversationId, UpdateWhatsAppConversationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var conversation = await _unitOfWork.WhatsAppConversations.GetByIdAsync(conversationId, cancellationToken);
            if (conversation == null)
                return Result<WhatsAppConversationResponse>.Failure(ErrorMessages.NotFound("Conversation"));

            if (request.IsActive.HasValue)
                conversation.IsActive = request.IsActive.Value;

            if (request.AiMode != null)
                conversation.AiMode = request.AiMode;

            if (request.Language != null)
                conversation.Language = request.Language;

            _unitOfWork.WhatsAppConversations.Update(conversation);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<WhatsAppConversationResponse>.Success(MapToConversationResponse(conversation));
        }
        catch (Exception ex)
        {
            return Result<WhatsAppConversationResponse>.Failure($"Failed to update conversation: {ex.Message}");
        }
    }

    private static WhatsAppMessageResponse MapToMessageResponse(WhatsAppMessage message)
    {
        return new WhatsAppMessageResponse
        {
            Id = message.Id,
            ConversationId = message.ConversationId,
            Direction = message.Direction,
            Content = message.Content,
            MediaUrl = message.MediaUrl,
            MediaType = message.MediaType,
            Status = message.Status,
            DeliveredAt = message.DeliveredAt,
            ReadAt = message.ReadAt,
            TwilioMessageSid = message.TwilioMessageSid,
            IsAiGenerated = message.IsAiGenerated,
            CreatedAt = message.CreatedAt
        };
    }

    private static WhatsAppConversationResponse MapToConversationResponse(WhatsAppConversation conversation)
    {
        return new WhatsAppConversationResponse
        {
            Id = conversation.Id,
            UserId = conversation.UserId,
            CustomerId = conversation.CustomerId,
            CustomerPhone = conversation.CustomerPhone,
            CustomerName = conversation.CustomerName,
            BusinessPhone = conversation.BusinessPhone,
            IsActive = conversation.IsActive,
            LastMessageAt = conversation.LastMessageAt,
            MessageCount = conversation.MessageCount,
            UnreadCount = conversation.UnreadCount,
            AiMode = conversation.AiMode,
            Summary = conversation.Summary,
            Language = conversation.Language,
            CreatedAt = conversation.CreatedAt
        };
    }

    /// <summary>
    /// Creates a new WhatsApp conversation, handling unique constraint violations from concurrent requests
    /// by re-reading the existing conversation instead.
    /// </summary>
    private async Task<WhatsAppConversation> GetOrCreateWhatsAppConversationAsync(
        string userId, string businessPhone, string customerPhone, string? customerId, CancellationToken ct)
    {
        var conversation = new WhatsAppConversation
        {
            UserId = userId,
            CustomerId = customerId,
            BusinessPhone = businessPhone,
            CustomerPhone = customerPhone,
            IsActive = true,
            MessageCount = 0,
            UnreadCount = 0
        };

        try
        {
            await _unitOfWork.WhatsAppConversations.AddAsync(conversation, ct);
            await _unitOfWork.SaveChangesAsync(ct);
            return conversation;
        }
        catch (DbUpdateException)
        {
            // Unique constraint violation — another request created it first; re-read
            var existing = await _unitOfWork.WhatsAppConversations.GetByPhoneNumbersAsync(businessPhone, customerPhone, ct);
            return existing ?? throw new InvalidOperationException("Failed to create or find WhatsApp conversation");
        }
    }
}
