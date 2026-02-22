using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SpeakHalo.Application.DTOs;
using SpeakHalo.Application.Interfaces;
using SpeakHalo.Domain.Entities;
using SpeakHalo.Domain.Enums;
using SpeakHalo.Domain.Interfaces.Repositories;
using SpeakHalo.Shared.Constants;
using SpeakHalo.Shared.Extensions;
using SpeakHalo.Shared.Helpers;
using SpeakHalo.Shared.Models;

namespace SpeakHalo.Application.Services;

public class CallService : ICallService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CallService> _logger;

    public CallService(IUnitOfWork unitOfWork, ILogger<CallService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<CallResponse>> CreateCallAsync(CreateCallRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var call = new Call
            {
                UserId = request.UserId,
                CustomerId = request.CustomerId,
                CallerPhone = request.FromNumber,
                RecipientPhone = request.ToNumber,
                Direction = request.Direction,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Calls.AddAsync(call, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<CallResponse>.Success(MapToCallResponse(call));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create call for user {UserId}", request.UserId);
            return Result<CallResponse>.Failure($"Failed to create call: {ex.Message}");
        }
    }

    public async Task<Result<CallResponse>> GetCallByIdAsync(string callId, CancellationToken cancellationToken = default)
    {
        try
        {
            var call = await _unitOfWork.Calls.GetByIdAsync(callId, cancellationToken);

            if (call == null)
                return Result<CallResponse>.Failure(ErrorMessages.NotFound("Call"));

            return Result<CallResponse>.Success(MapToCallResponse(call));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get call {CallId}", callId);
            return Result<CallResponse>.Failure($"Failed to get call: {ex.Message}");
        }
    }

    public async Task<Result<PaginatedList<CallResponse>>> GetUserCallsAsync(string userId, int pageNumber = 1, int pageSize = 10, string? search = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var skip = (pageNumber - 1) * pageSize;
            var totalCount = await _unitOfWork.Calls.CountUserCallsAsync(userId, search, cancellationToken);
            var calls = await _unitOfWork.Calls.GetUserCallsAsync(userId, skip, pageSize, search, cancellationToken);

            var responses = calls.Select(MapToCallResponse).ToList();

            var result = new PaginatedList<CallResponse>(
                responses,
                totalCount,
                pageNumber,
                pageSize
            );

            return Result<PaginatedList<CallResponse>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get calls for user {UserId}", userId);
            return Result<PaginatedList<CallResponse>>.Failure($"Failed to get user calls: {ex.Message}");
        }
    }

    public async Task<Result<CallResponse>> UpdateCallAsync(string callId, UpdateCallRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var call = await _unitOfWork.Calls.GetByIdAsync(callId, cancellationToken);

            if (call == null)
                return Result<CallResponse>.Failure(ErrorMessages.NotFound("Call"));

            if (request.DurationSeconds.HasValue)
                call.DurationSeconds = request.DurationSeconds.Value;

            if (request.RecordingUrl != null)
                call.RecordingUrl = request.RecordingUrl;

            if (request.Outcome.HasValue)
                call.Outcome = request.Outcome;

            if (request.Sentiment.HasValue)
                call.Sentiment = request.Sentiment;

            if (request.ServiceType.HasValue)
                call.ServiceType = request.ServiceType;

            if (request.Summary != null)
                call.Summary = request.Summary;

            if (request.ActionItems != null)
                call.ActionItems = request.ActionItems;

            if (request.AppointmentScheduled.HasValue)
                call.AppointmentScheduled = request.AppointmentScheduled.Value;

            if (request.QuoteRequested.HasValue)
                call.QuoteRequested = request.QuoteRequested.Value;

            _unitOfWork.Calls.Update(call);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<CallResponse>.Success(MapToCallResponse(call));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update call {CallId}", callId);
            return Result<CallResponse>.Failure($"Failed to update call: {ex.Message}");
        }
    }

    public async Task<Result<bool>> EndCallAsync(string callId, int durationSeconds, CancellationToken cancellationToken = default)
    {
        try
        {
            var call = await _unitOfWork.Calls.GetByIdAsync(callId, cancellationToken);

            if (call == null)
                return Result<bool>.Failure(ErrorMessages.NotFound("Call"));

            call.DurationSeconds = durationSeconds;
            call.EndedAt = DateTime.UtcNow;

            _unitOfWork.Calls.Update(call);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to end call {CallId}", callId);
            return Result<bool>.Failure($"Failed to end call: {ex.Message}");
        }
    }

    public async Task<Result<bool>> AddTranscriptEntryAsync(string callId, string speaker, string text, CancellationToken cancellationToken = default)
    {
        try
        {
            var call = await _unitOfWork.Calls.GetByIdAsync(callId, cancellationToken);

            if (call == null)
                return Result<bool>.Failure(ErrorMessages.NotFound("Call"));

            // Sanitize inputs to prevent XSS and injection attacks
            var sanitizedSpeaker = InputSanitizer.SanitizeSpeakerName(speaker);
            var sanitizedText = InputSanitizer.SanitizeTranscriptText(text);

            var entry = new TranscriptEntry
            {
                CallId = callId,
                Speaker = sanitizedSpeaker,
                Text = sanitizedText,
                Timestamp = DateTime.UtcNow
            };

            call.TranscriptEntries.Add(entry);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add transcript entry for call {CallId}", callId);
            return Result<bool>.Failure($"Failed to add transcript entry: {ex.Message}");
        }
    }

    public async Task<Result<List<TranscriptEntryResponse>>> GetCallTranscriptAsync(string callId, CancellationToken cancellationToken = default)
    {
        try
        {
            var call = await _unitOfWork.Calls.GetByIdAsync(callId, cancellationToken);

            if (call == null)
                return Result<List<TranscriptEntryResponse>>.Failure(ErrorMessages.NotFound("Call"));

            var responses = call.TranscriptEntries
                .OrderBy(e => e.Timestamp)
                .Select(e => new TranscriptEntryResponse
                {
                    Id = e.Id,
                    CallId = e.CallId,
                    Speaker = e.Speaker,
                    Text = e.Text,
                    Confidence = e.Confidence,
                    Timestamp = e.Timestamp
                })
                .ToList();

            return Result<List<TranscriptEntryResponse>>.Success(responses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get transcript for call {CallId}", callId);
            return Result<List<TranscriptEntryResponse>>.Failure($"Failed to get transcript: {ex.Message}");
        }
    }

    public async Task<Result<CallAnalyticsResponse>> GetCallAnalyticsAsync(string userId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        try
        {
            var now = DateTime.UtcNow;
            var effectiveEnd = endDate > now ? now : endDate;
            var calls = await _unitOfWork.Calls.FindAsync(
                c => c.UserId == userId && c.CreatedAt >= startDate && c.CreatedAt <= effectiveEnd,
                cancellationToken
            );

            var callsList = calls.ToList();

            var analytics = new CallAnalyticsResponse
            {
                TotalCalls = callsList.Count,
                TotalMinutes = (int)(callsList.Sum(c => c.DurationSeconds) / 60.0),
                TotalCost = callsList.Sum(c => c.Cost),
                AverageCallDuration = callsList.Any() ? (decimal)callsList.Average(c => c.DurationSeconds) : 0,
                InboundCalls = callsList.Count(c => c.Direction == "inbound"),
                OutboundCalls = callsList.Count(c => c.Direction == "outbound"),
                AppointmentsScheduled = callsList.Count(c => c.AppointmentScheduled),
                QuotesRequested = callsList.Count(c => c.QuoteRequested),
                OutcomeBreakdown = callsList
                    .Where(c => c.Outcome.HasValue)
                    .GroupBy(c => c.Outcome!.Value.ToString())
                    .ToDictionary(g => g.Key, g => g.Count()),
                SentimentBreakdown = callsList
                    .Where(c => c.Sentiment.HasValue)
                    .GroupBy(c => c.Sentiment!.Value.ToString())
                    .ToDictionary(g => g.Key, g => g.Count()),
                ServiceTypeBreakdown = callsList
                    .Where(c => c.ServiceType.HasValue)
                    .GroupBy(c => c.ServiceType!.Value.ToString())
                    .ToDictionary(g => g.Key, g => g.Count())
            };

            return Result<CallAnalyticsResponse>.Success(analytics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get call analytics for user {UserId}", userId);
            return Result<CallAnalyticsResponse>.Failure($"Failed to get analytics: {ex.Message}");
        }
    }

    private static CallResponse MapToCallResponse(Call call)
    {
        return new CallResponse
        {
            Id = call.Id,
            UserId = call.UserId,
            CustomerId = call.CustomerId,
            FromNumber = call.CallerPhone,
            ToNumber = call.RecipientPhone,
            Direction = call.Direction,
            TwilioCallSid = call.TwilioCallSid,
            DurationSeconds = call.DurationSeconds,
            RecordingUrl = call.RecordingUrl,
            TranscriptText = call.TranscriptText,
            Outcome = call.Outcome,
            Sentiment = call.Sentiment,
            ServiceType = call.ServiceType,
            Summary = call.Summary,
            ActionItems = call.ActionItems,
            AppointmentScheduled = call.AppointmentScheduled,
            QuoteRequested = call.QuoteRequested,
            Cost = call.Cost,
            CreatedAt = call.CreatedAt
        };
    }
}
