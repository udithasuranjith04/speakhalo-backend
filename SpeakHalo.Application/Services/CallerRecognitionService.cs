using Microsoft.Extensions.Logging;
using SpeakHalo.Application.Interfaces;
using SpeakHalo.Domain.Entities;
using SpeakHalo.Domain.Enums;
using SpeakHalo.Domain.Interfaces.Repositories;

namespace SpeakHalo.Application.Services;

public class CallerRecognitionService : ICallerRecognitionService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cache;
    private readonly ILogger<CallerRecognitionService> _logger;

    private static readonly TimeSpan CallerCacheTtl = TimeSpan.FromMinutes(5);

    public CallerRecognitionService(IUnitOfWork unitOfWork, ICacheService cache, ILogger<CallerRecognitionService> logger)
    {
        _unitOfWork = unitOfWork;
        _cache = cache;
        _logger = logger;
    }

    public async Task<RecognizedCaller?> RecognizeCallerAsync(string userId, string phone, string channel, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(phone))
            return null;

        try
        {
            var normalizedPhone = phone.Replace("whatsapp:", "").Trim();

            // Same caller within 5 min gets instant recognition via cache
            var cacheKey = $"caller:{userId}:{normalizedPhone}";
            var cached = await _cache.GetAsync<RecognizedCaller>(cacheKey, ct);
            if (cached != null)
                return cached;

            var profiles = await _unitOfWork.Repository<UserProfile>()
                .FindAsync(p => p.UserId == userId, ct);
            var profile = profiles.FirstOrDefault();

            if (profile == null || !profile.CallerRecognitionEnabled)
                return null;

            var customerTask = _unitOfWork.Repository<Customer>()
                .FindAsync(c => c.UserId == userId && c.Phone == normalizedPhone && !c.IsDeleted, ct);
            var callHistoryTask = BuildCallHistoryAsync(userId, normalizedPhone, ct);
            var whatsAppHistoryTask = BuildWhatsAppHistoryAsync(userId, normalizedPhone, ct);
            var smsHistoryTask = BuildSmsHistoryAsync(userId, normalizedPhone, ct);

            await Task.WhenAll(customerTask, callHistoryTask, whatsAppHistoryTask, smsHistoryTask);

            var customer = (await customerTask).FirstOrDefault();
            var callHistory = await callHistoryTask;
            var whatsAppHistory = await whatsAppHistoryTask;
            var smsHistory = await smsHistoryTask;

            var upcomingAppointments = new List<CallerAppointmentInfo>();
            var pastAppointments = new List<CallerAppointmentInfo>();
            string? lastServiceType = null;
            DateTime? lastServiceDate = customer?.LastServiceDate;

            if (customer != null)
            {
                var appointments = await _unitOfWork.Repository<Appointment>()
                    .FindAsync(a => a.CustomerId == customer.Id && a.UserId == userId && !a.IsDeleted, ct);

                var now = DateTime.UtcNow;

                foreach (var appt in appointments.OrderByDescending(a => a.ScheduledAt).Take(50))
                {
                    var info = new CallerAppointmentInfo(
                        ServiceType: appt.ServiceType.ToString(),
                        Status: appt.Status.ToString(),
                        ScheduledAt: appt.ScheduledAt,
                        Description: appt.Notes
                    );

                    if (appt.Status == AppointmentStatus.Scheduled || appt.Status == AppointmentStatus.Confirmed)
                    {
                        if (appt.ScheduledAt > now)
                            upcomingAppointments.Add(info);
                        else
                            pastAppointments.Add(info);
                    }
                    else
                    {
                        pastAppointments.Add(info);
                    }
                }

                var lastCompleted = appointments
                    .Where(a => a.Status == AppointmentStatus.Completed)
                    .OrderByDescending(a => a.CompletedAt ?? a.ScheduledAt)
                    .FirstOrDefault();

                if (lastCompleted != null)
                {
                    lastServiceType = lastCompleted.ServiceType.ToString();
                    lastServiceDate ??= lastCompleted.CompletedAt ?? lastCompleted.ScheduledAt;
                }
            }

            bool hasAnyHistory = customer != null
                || callHistory.TotalCalls > 0
                || whatsAppHistory.ConversationCount > 0
                || smsHistory.ConversationCount > 0;

            if (!hasAnyHistory)
                return null;

            _logger.LogInformation(
                "Recognized caller for user {UserId} via {Channel}: CustomerId={CustomerId}, Calls={Calls}, WA={WA}, SMS={SMS}",
                userId, channel, customer?.Id ?? "(none)", callHistory.TotalCalls,
                whatsAppHistory.ConversationCount, smsHistory.ConversationCount);

            var result = new RecognizedCaller(
                CustomerId: customer?.Id,
                Name: customer?.Name,
                JobCount: customer?.JobCount ?? 0,
                LastServiceType: lastServiceType,
                LastServiceDate: lastServiceDate,
                Notes: customer?.Notes,
                CallHistory: callHistory,
                WhatsAppHistory: whatsAppHistory,
                SmsHistory: smsHistory,
                UpcomingAppointments: upcomingAppointments,
                PastAppointments: pastAppointments
            );

            await _cache.SetAsync(cacheKey, result, CallerCacheTtl, ct);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Caller recognition failed for phone {Phone}, user {UserId}", phone, userId);
            return null;
        }
    }

    private async Task<CallerCallHistory> BuildCallHistoryAsync(string userId, string normalizedPhone, CancellationToken ct)
    {
        var calls = await _unitOfWork.Repository<Call>()
            .FindAsync(c => c.UserId == userId && (c.CallerPhone == normalizedPhone || c.RecipientPhone == normalizedPhone) && !c.IsDeleted, ct);

        var callList = calls.OrderByDescending(c => c.CreatedAt).Take(100).ToList();

        if (callList.Count == 0)
            return new CallerCallHistory(0, null, null, null);

        var mostRecent = callList.First();
        return new CallerCallHistory(
            TotalCalls: callList.Count,
            LastCallDate: mostRecent.CreatedAt,
            LastCallSummary: mostRecent.Summary,
            LastCallServiceType: mostRecent.ServiceType?.ToString()
        );
    }

    private async Task<CallerMessagingHistory> BuildWhatsAppHistoryAsync(string userId, string normalizedPhone, CancellationToken ct)
    {
        // WhatsApp phones include "whatsapp:" prefix in CustomerPhone
        var waPhone = $"whatsapp:{normalizedPhone}";
        var conversations = await _unitOfWork.Repository<WhatsAppConversation>()
            .FindAsync(c => c.UserId == userId && (c.CustomerPhone == waPhone || c.CustomerPhone == normalizedPhone) && !c.IsDeleted, ct);

        var convoList = conversations.OrderByDescending(c => c.LastMessageAt).Take(50).ToList();

        if (convoList.Count == 0)
            return new CallerMessagingHistory(0, 0, null, null);

        var mostRecent = convoList.First();
        var totalMessages = convoList.Sum(c => c.MessageCount);

        return new CallerMessagingHistory(
            ConversationCount: convoList.Count,
            TotalMessages: totalMessages,
            LastMessageDate: mostRecent.LastMessageAt,
            LastConversationSummary: mostRecent.Summary
        );
    }

    private async Task<CallerMessagingHistory> BuildSmsHistoryAsync(string userId, string normalizedPhone, CancellationToken ct)
    {
        var conversations = await _unitOfWork.Repository<SMSConversation>()
            .FindAsync(c => c.UserId == userId && c.CustomerPhone == normalizedPhone && !c.IsDeleted, ct);

        var convoList = conversations.OrderByDescending(c => c.LastMessageAt).Take(50).ToList();

        if (convoList.Count == 0)
            return new CallerMessagingHistory(0, 0, null, null);

        var mostRecent = convoList.First();
        var totalMessages = convoList.Sum(c => c.MessageCount);

        return new CallerMessagingHistory(
            ConversationCount: convoList.Count,
            TotalMessages: totalMessages,
            LastMessageDate: mostRecent.LastMessageAt,
            LastConversationSummary: null // SMS conversations don't have a Summary field
        );
    }
}
