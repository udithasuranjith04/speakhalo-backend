using SpeakHalo.Shared.Models;

namespace SpeakHalo.Application.Interfaces;

public interface ICalendarService
{
    Task<Result<string>> CreateEventAsync(string userId, string summary, string description, DateTime startTime, DateTime endTime, string? location = null, CancellationToken cancellationToken = default);
    Task<Result<bool>> UpdateEventAsync(string userId, string eventId, string? summary = null, DateTime? startTime = null, DateTime? endTime = null, CancellationToken cancellationToken = default);
    Task<Result<bool>> DeleteEventAsync(string userId, string eventId, CancellationToken cancellationToken = default);
    Task<Result<List<object>>> GetEventsAsync(string userId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    Task<Result<bool>> CheckAvailabilityAsync(string userId, DateTime startTime, DateTime endTime, CancellationToken cancellationToken = default);

    // OAuth methods
    Task<Result<string>> GetAuthorizationUrlAsync(string userId, CancellationToken cancellationToken = default);
    Task<Result<bool>> HandleOAuthCallbackAsync(string code, string state, CancellationToken cancellationToken = default);
    Task<Result<bool>> DisconnectAsync(string userId, CancellationToken cancellationToken = default);
    Task<Result<bool>> IsConnectedAsync(string userId, CancellationToken cancellationToken = default);
}
