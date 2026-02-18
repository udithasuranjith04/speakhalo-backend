using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SpeakHalo.Application.Interfaces;
using SpeakHalo.Shared.Models;

#pragma warning disable CS0618 // Google Calendar API obsolete properties (DateTime, TimeMin, TimeMax) still work correctly

namespace SpeakHalo.Infrastructure.Services;

public class CalendarService : ICalendarService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<CalendarService> _logger;

    public CalendarService(IConfiguration configuration, ILogger<CalendarService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    private Google.Apis.Calendar.v3.CalendarService GetGoogleCalendarService(string userId)
    {
        var credentialsPath = _configuration["GoogleCalendar:CredentialsPath"];
        var credential = GoogleCredential.FromFile(credentialsPath)
            .CreateScoped(Google.Apis.Calendar.v3.CalendarService.Scope.Calendar);

        return new Google.Apis.Calendar.v3.CalendarService(new BaseClientService.Initializer()
        {
            HttpClientInitializer = credential,
            ApplicationName = "SpeakHalo"
        });
    }

    public async Task<Result<string>> CreateEventAsync(string userId, string summary, string description, DateTime startTime, DateTime endTime, string? location = null, CancellationToken cancellationToken = default)
    {
        try
        {
            using var service = GetGoogleCalendarService(userId);

            var newEvent = new Event
            {
                Summary = summary,
                Description = description,
                Location = location,
                Start = new EventDateTime
                {
                    DateTime = startTime,
                    TimeZone = "UTC"
                },
                End = new EventDateTime
                {
                    DateTime = endTime,
                    TimeZone = "UTC"
                },
                Reminders = new Event.RemindersData
                {
                    UseDefault = false,
                    Overrides = new[]
                    {
                        new EventReminder { Method = "email", Minutes = 24 * 60 },
                        new EventReminder { Method = "popup", Minutes = 30 }
                    }
                }
            };

            var request = service.Events.Insert(newEvent, "primary");
            var createdEvent = await request.ExecuteAsync(cancellationToken);

            return Result<string>.Success(createdEvent.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create calendar event for user {UserId}", userId);
            return Result<string>.Failure($"Failed to create calendar event: {ex.Message}");
        }
    }

    public async Task<Result<bool>> UpdateEventAsync(string userId, string eventId, string? summary = null, DateTime? startTime = null, DateTime? endTime = null, CancellationToken cancellationToken = default)
    {
        try
        {
            using var service = GetGoogleCalendarService(userId);

            var request = service.Events.Get("primary", eventId);
            var existingEvent = await request.ExecuteAsync(cancellationToken);

            if (summary != null)
                existingEvent.Summary = summary;

            if (startTime.HasValue)
                existingEvent.Start = new EventDateTime { DateTime = startTime.Value, TimeZone = "UTC" };

            if (endTime.HasValue)
                existingEvent.End = new EventDateTime { DateTime = endTime.Value, TimeZone = "UTC" };

            var updateRequest = service.Events.Update(existingEvent, "primary", eventId);
            await updateRequest.ExecuteAsync(cancellationToken);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update calendar event {EventId} for user {UserId}", eventId, userId);
            return Result<bool>.Failure($"Failed to update calendar event: {ex.Message}");
        }
    }

    public async Task<Result<bool>> DeleteEventAsync(string userId, string eventId, CancellationToken cancellationToken = default)
    {
        try
        {
            using var service = GetGoogleCalendarService(userId);

            var request = service.Events.Delete("primary", eventId);
            await request.ExecuteAsync(cancellationToken);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete calendar event {EventId} for user {UserId}", eventId, userId);
            return Result<bool>.Failure($"Failed to delete calendar event: {ex.Message}");
        }
    }

    public async Task<Result<List<object>>> GetEventsAsync(string userId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        try
        {
            using var service = GetGoogleCalendarService(userId);

            var request = service.Events.List("primary");
            request.TimeMin = startDate;
            request.TimeMax = endDate;
            request.ShowDeleted = false;
            request.SingleEvents = true;
            request.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;

            var events = await request.ExecuteAsync(cancellationToken);

            var eventList = events.Items.Select(e => new
            {
                id = e.Id,
                summary = e.Summary,
                description = e.Description,
                start = e.Start.DateTime,
                end = e.End.DateTime,
                location = e.Location
            }).Cast<object>().ToList();

            return Result<List<object>>.Success(eventList);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get calendar events for user {UserId}", userId);
            return Result<List<object>>.Failure($"Failed to get calendar events: {ex.Message}");
        }
    }

    public async Task<Result<bool>> CheckAvailabilityAsync(string userId, DateTime startTime, DateTime endTime, CancellationToken cancellationToken = default)
    {
        try
        {
            var eventsResult = await GetEventsAsync(userId, startTime, endTime, cancellationToken);

            if (!eventsResult.IsSuccess)
                return Result<bool>.Failure(eventsResult.Errors.FirstOrDefault() ?? "Failed to get events");

            var isAvailable = eventsResult.Data!.Count == 0;
            return Result<bool>.Success(isAvailable);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check availability for user {UserId}", userId);
            return Result<bool>.Failure($"Failed to check availability: {ex.Message}");
        }
    }
}

#pragma warning restore CS0618
