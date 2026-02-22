using SpeakHalo.Application.DTOs;
using SpeakHalo.Shared.Helpers;
using SpeakHalo.Shared.Models;

namespace SpeakHalo.Application.Interfaces;

public interface ICallService
{
    Task<Result<CallResponse>> CreateCallAsync(CreateCallRequest request, CancellationToken cancellationToken = default);
    Task<Result<CallResponse>> GetCallByIdAsync(string callId, CancellationToken cancellationToken = default);
    Task<Result<PaginatedList<CallResponse>>> GetUserCallsAsync(string userId, int pageNumber = 1, int pageSize = 10, string? search = null, CancellationToken cancellationToken = default);
    Task<Result<CallResponse>> UpdateCallAsync(string callId, UpdateCallRequest request, CancellationToken cancellationToken = default);
    Task<Result<bool>> EndCallAsync(string callId, int durationSeconds, CancellationToken cancellationToken = default);
    Task<Result<bool>> AddTranscriptEntryAsync(string callId, string speaker, string text, CancellationToken cancellationToken = default);
    Task<Result<List<TranscriptEntryResponse>>> GetCallTranscriptAsync(string callId, CancellationToken cancellationToken = default);
    Task<Result<CallAnalyticsResponse>> GetCallAnalyticsAsync(string userId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
}
