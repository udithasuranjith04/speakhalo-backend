using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SpeakHalo.Application.Interfaces;
using SpeakHalo.Domain.Entities;
using SpeakHalo.Domain.Enums;
using SpeakHalo.Infrastructure.Data;
using System.Text.Json;

#pragma warning disable CS0618

namespace SpeakHalo.Infrastructure.Services;

public class AIFollowUpService : IAIFollowUpService
{
    private readonly ApplicationDbContext _context;
    private readonly IWhatsAppService _whatsAppService;
    private readonly IUsageTrackingService _usageTracking;
    private readonly ILogger<AIFollowUpService> _logger;

    public AIFollowUpService(
        ApplicationDbContext context,
        IWhatsAppService whatsAppService,
        IUsageTrackingService usageTracking,
        ILogger<AIFollowUpService> logger)
    {
        _context = context;
        _whatsAppService = whatsAppService;
        _usageTracking = usageTracking;
        _logger = logger;
    }

    public async Task<FollowUpResult> ScheduleFollowUpAsync(string callId, int delayMinutes = 5)
    {
        try
        {
            var call = await _context.Calls
                .Include(c => c.Customer)
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.Id == callId);

            if (call == null)
            {
                return new FollowUpResult
                {
                    Success = false,
                    Message = "Call not found"
                };
            }

            // Check if follow-up already exists
            var existingFollowUp = await _context.Set<CallFollowUp>()
                .FirstOrDefaultAsync(f => f.CallId == callId);

            if (existingFollowUp != null)
            {
                return new FollowUpResult
                {
                    Success = false,
                    Message = "Follow-up already scheduled for this call",
                    FollowUpId = existingFollowUp.Id
                };
            }

            // Generate the follow-up message
            var messageContent = await GenerateFollowUpMessageAsync(callId);
            var templateName = GetTemplateNameForOutcome(call.Outcome, call.AppointmentScheduled, call.QuoteRequested);

            // Schedule the follow-up
            var scheduledFor = (call.EndedAt ?? DateTime.UtcNow).AddMinutes(delayMinutes);

            var followUp = new CallFollowUp
            {
                CallId = callId,
                UserId = call.UserId,
                CustomerId = call.CustomerId,
                Channel = "whatsapp",
                RecipientPhone = call.CallerPhone,
                CallOutcome = call.Outcome,
                AppointmentScheduled = call.AppointmentScheduled,
                QuoteRequested = call.QuoteRequested,
                MessageTemplate = templateName,
                MessageContent = messageContent,
                ScheduledFor = scheduledFor,
                DelayMinutes = delayMinutes,
                Status = "pending"
            };

            _context.Set<CallFollowUp>().Add(followUp);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Scheduled follow-up {FollowUpId} for call {CallId}, will send at {ScheduledTime}",
                followUp.Id, callId, scheduledFor);

            return new FollowUpResult
            {
                Success = true,
                Message = "Follow-up scheduled successfully",
                FollowUpId = followUp.Id,
                ScheduledFor = scheduledFor,
                MessagePreview = messageContent.Length > 100 ? messageContent.Substring(0, 100) + "..." : messageContent
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scheduling follow-up for call {CallId}", callId);
            return new FollowUpResult
            {
                Success = false,
                Message = $"Error scheduling follow-up: {ex.Message}"
            };
        }
    }

    public async Task<int> ProcessPendingFollowUpsAsync()
    {
        try
        {
            var now = DateTime.UtcNow;
            var pendingFollowUps = await _context.Set<CallFollowUp>()
                .Where(f => f.Status == "pending" && f.ScheduledFor <= now)
                .Include(f => f.Call)
                .ThenInclude(c => c.Customer)
                .Include(f => f.User)
                .OrderBy(f => f.ScheduledFor)
                .Take(50) // Process max 50 at a time
                .ToListAsync();

            _logger.LogInformation("Processing {Count} pending follow-ups", pendingFollowUps.Count);

            int successCount = 0;

            foreach (var followUp in pendingFollowUps)
            {
                try
                {
                    // Check if user's account is active
                    var subscription = await _context.UserSubscriptions
                        .AsNoTracking()
                        .FirstOrDefaultAsync(s => s.UserId == followUp.UserId);
                    if (subscription == null || !subscription.HasActivePayment || subscription.IsTemporarilySuspended)
                    {
                        followUp.Status = "skipped";
                        followUp.ErrorMessage = "User account is not active";
                        _logger.LogWarning("Skipping follow-up {FollowUpId} for user {UserId}: account not active",
                            followUp.Id, followUp.UserId);
                        continue;
                    }

                    // Send via WhatsApp
                    var request = new SpeakHalo.Application.DTOs.SendWhatsAppMessageRequest
                    {
                        UserId = followUp.UserId,
                        ToNumber = followUp.RecipientPhone,
                        Content = followUp.MessageContent,
                        CustomerId = followUp.CustomerId
                    };
                    var result = await _whatsAppService.SendMessageAsync(request);

                    if (result.IsSuccess && result.Data != null)
                    {
                        followUp.Status = "sent";
                        followUp.SentAt = DateTime.UtcNow;
                        followUp.WhatsAppMessageId = result.Data.Id;
                        followUp.DeliveryStatus = "sent";
                        successCount++;

                        // WhatsApp usage tracking is handled inside WhatsAppService.SendMessageAsync
                        // Do NOT track here — it would double-charge the user

                        _logger.LogInformation("Sent follow-up {FollowUpId} for call {CallId}",
                            followUp.Id, followUp.CallId);
                    }
                    else
                    {
                        followUp.Status = "failed";
                        followUp.ErrorMessage = string.Join(", ", result.Errors);
                        followUp.RetryCount++;
                        followUp.LastRetryAt = DateTime.UtcNow;

                        // Retry logic - if retry count < 3, reschedule for 15 minutes later
                        if (followUp.RetryCount < 3)
                        {
                            followUp.Status = "pending";
                            followUp.ScheduledFor = DateTime.UtcNow.AddMinutes(15);
                        }

                        _logger.LogWarning("Failed to send follow-up {FollowUpId}: {Error}",
                            followUp.Id, string.Join(", ", result.Errors));
                    }
                }
                catch (Exception ex)
                {
                    followUp.Status = "failed";
                    followUp.ErrorMessage = ex.Message;
                    followUp.RetryCount++;
                    followUp.LastRetryAt = DateTime.UtcNow;

                    _logger.LogError(ex, "Error sending follow-up {FollowUpId}", followUp.Id);
                }
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Processed {Total} follow-ups, {Success} sent successfully",
                pendingFollowUps.Count, successCount);

            return successCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing pending follow-ups");
            return 0;
        }
    }

    public async Task<string> GenerateFollowUpMessageAsync(string callId)
    {
        var call = await _context.Calls
            .Include(c => c.Customer)
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.Id == callId);

        if (call == null)
        {
            return "Thank you for your call. We'll be in touch soon!";
        }

        var businessName = call.User?.BusinessName ?? "our team";
        var customerName = call.Customer?.Name ?? call.CallerName ?? "there";

        // Generate message based on call outcome
        return call.Outcome switch
        {
            CallOutcome.Booked when call.AppointmentScheduled => GenerateAppointmentConfirmationMessage(businessName, customerName, call),
            CallOutcome.Callback => GenerateCallbackMessage(businessName, customerName, call),
            CallOutcome.Voicemail => GenerateVoicemailFollowUpMessage(businessName, customerName, call),
            CallOutcome.Missed => GenerateMissedCallMessage(businessName, customerName, call),
            CallOutcome.Transferred => GenerateTransferredCallMessage(businessName, customerName, call),
            _ when call.QuoteRequested => GenerateQuoteRequestMessage(businessName, customerName, call),
            _ => GenerateGenericFollowUpMessage(businessName, customerName, call)
        };
    }

    public async Task<bool> CancelFollowUpAsync(string followUpId)
    {
        try
        {
            var followUp = await _context.Set<CallFollowUp>()
                .FirstOrDefaultAsync(f => f.Id == followUpId);

            if (followUp == null)
            {
                return false;
            }

            if (followUp.Status == "sent")
            {
                _logger.LogWarning("Cannot cancel follow-up {FollowUpId} - already sent", followUpId);
                return false;
            }

            followUp.Status = "cancelled";
            await _context.SaveChangesAsync();

            _logger.LogInformation("Cancelled follow-up {FollowUpId}", followUpId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling follow-up {FollowUpId}", followUpId);
            return false;
        }
    }

    public async Task<FollowUpStats> GetFollowUpStatsAsync(string userId)
    {
        var followUps = await _context.Set<CallFollowUp>()
            .Where(f => f.UserId == userId)
            .ToListAsync();

        var totalSent = followUps.Count(f => f.Status == "sent");
        var totalDelivered = followUps.Count(f => f.DeliveryStatus == "delivered" || f.DeliveryStatus == "read");

        return new FollowUpStats
        {
            TotalScheduled = followUps.Count,
            TotalSent = totalSent,
            TotalFailed = followUps.Count(f => f.Status == "failed"),
            PendingSend = followUps.Count(f => f.Status == "pending"),
            DeliveryRate = totalSent > 0 ? (decimal)totalDelivered / totalSent * 100 : 0,
            ResponseRate = totalSent > 0 ? (decimal)followUps.Count(f => f.CustomerResponded) / totalSent * 100 : 0,
            ByOutcome = followUps
                .Where(f => f.CallOutcome.HasValue)
                .GroupBy(f => f.CallOutcome!.Value)
                .ToDictionary(g => g.Key, g => g.Count())
        };
    }

    public async Task<FollowUpResult> SendFollowUpNowAsync(string callId)
    {
        // Schedule with 0 delay, then immediately process
        var result = await ScheduleFollowUpAsync(callId, 0);

        if (result.Success && result.FollowUpId != null)
        {
            await ProcessPendingFollowUpsAsync();
        }

        return result;
    }

    // Template generation methods

    private string GenerateAppointmentConfirmationMessage(string businessName, string customerName, Call call)
    {
        return $"Hi {customerName}! 👋\n\n" +
               $"Thank you for scheduling with {businessName}! Your appointment has been confirmed.\n\n" +
               $"We'll send you a reminder 24 hours before your scheduled time.\n\n" +
               $"If you need to make any changes, just reply to this message or give us a call.\n\n" +
               $"See you soon! 🛠️";
    }

    private string GenerateCallbackMessage(string businessName, string customerName, Call call)
    {
        return $"Hi {customerName}! 👋\n\n" +
               $"Thanks for your call to {businessName}. We noted that you'd like us to call you back.\n\n" +
               $"We'll be reaching out within the next few hours during business hours.\n\n" +
               $"If you have any questions in the meantime, feel free to reply to this message!\n\n" +
               $"Talk soon! 📞";
    }

    private string GenerateVoicemailFollowUpMessage(string businessName, string customerName, Call call)
    {
        return $"Hi {customerName}! 👋\n\n" +
               $"We received your voicemail at {businessName}. Thank you for reaching out!\n\n" +
               $"We'll review your message and get back to you as soon as possible.\n\n" +
               $"In the meantime, if you need immediate assistance, you can reply to this message or visit our website.\n\n" +
               $"We appreciate your patience! 🙏";
    }

    private string GenerateMissedCallMessage(string businessName, string customerName, Call call)
    {
        return $"Hi {customerName}! 👋\n\n" +
               $"We noticed you tried to reach {businessName}. Sorry we missed your call!\n\n" +
               $"How can we help you today? You can:\n" +
               $"• Reply to this message with your question\n" +
               $"• Call us back during business hours\n" +
               $"• Schedule an appointment at your convenience\n\n" +
               $"We're here to help! 🛠️";
    }

    private string GenerateTransferredCallMessage(string businessName, string customerName, Call call)
    {
        return $"Hi {customerName}! 👋\n\n" +
               $"Thanks for speaking with us at {businessName}. We hope we were able to help!\n\n" +
               $"If you have any follow-up questions or need additional assistance, feel free to reply to this message.\n\n" +
               $"We're always here to help! 🙌";
    }

    private string GenerateQuoteRequestMessage(string businessName, string customerName, Call call)
    {
        return $"Hi {customerName}! 👋\n\n" +
               $"Thank you for requesting a quote from {businessName}!\n\n" +
               $"We're preparing a detailed estimate for you and will send it over shortly.\n\n" +
               $"If you have any questions or additional details to share, just reply to this message!\n\n" +
               $"Thanks for considering us! 💼";
    }

    private string GenerateGenericFollowUpMessage(string businessName, string customerName, Call call)
    {
        return $"Hi {customerName}! 👋\n\n" +
               $"Thanks for contacting {businessName}. We appreciate you reaching out!\n\n" +
               $"If you have any questions or need assistance, feel free to reply to this message anytime.\n\n" +
               $"We're here to help! 🛠️";
    }

    private string GetTemplateNameForOutcome(CallOutcome? outcome, bool appointmentScheduled, bool quoteRequested)
    {
        if (appointmentScheduled) return "appointment_confirmation";
        if (quoteRequested) return "quote_requested";

        return outcome switch
        {
            CallOutcome.Booked => "appointment_confirmation",
            CallOutcome.Callback => "callback_scheduled",
            CallOutcome.Voicemail => "voicemail_received",
            CallOutcome.Missed => "missed_call",
            CallOutcome.Transferred => "call_transferred",
            _ => "generic_followup"
        };
    }
}

#pragma warning restore CS0618
