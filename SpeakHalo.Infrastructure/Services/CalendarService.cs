using System.Text.Json;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SpeakHalo.Application.Interfaces;
using SpeakHalo.Domain.Interfaces.Repositories;
using SpeakHalo.Shared.Models;

#pragma warning disable CS0618 // Google Calendar API obsolete properties (DateTime, TimeMin, TimeMax) still work correctly

namespace SpeakHalo.Infrastructure.Services;

public class CalendarService : ICalendarService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<CalendarService> _logger;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEncryptionService _encryptionService;

    public CalendarService(
        IConfiguration configuration,
        ILogger<CalendarService> logger,
        IUnitOfWork unitOfWork,
        IEncryptionService encryptionService)
    {
        _configuration = configuration;
        _logger = logger;
        _unitOfWork = unitOfWork;
        _encryptionService = encryptionService;
    }

    private async Task<Google.Apis.Calendar.v3.CalendarService?> GetGoogleCalendarServiceAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
        var profile = user?.Profile;

        if (profile == null || !profile.GoogleCalendarConnected || string.IsNullOrEmpty(profile.GoogleCalendarToken))
        {
            _logger.LogDebug("User {UserId} has no Google Calendar connected", userId);
            return null;
        }

        try
        {
            var tokenData = JsonSerializer.Deserialize<StoredTokenData>(profile.GoogleCalendarToken);

            if (tokenData == null)
                return null;

            var clientId = _configuration["GoogleCalendar:ClientId"]!;
            var clientSecret = _configuration["GoogleCalendar:ClientSecret"]!;

            var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = new ClientSecrets
                {
                    ClientId = clientId,
                    ClientSecret = clientSecret
                },
                Scopes = new[] { Google.Apis.Calendar.v3.CalendarService.Scope.Calendar }
            });

            var tokenResponse = new TokenResponse
            {
                AccessToken = tokenData.AccessToken,
                RefreshToken = tokenData.RefreshToken,
                ExpiresInSeconds = tokenData.ExpiresInSeconds,
                IssuedUtc = tokenData.IssuedUtc
            };

            var credential = new UserCredential(flow, userId, tokenResponse);

            // Auto-refresh if expired
            if (credential.Token.IsStale)
            {
                var refreshed = await credential.RefreshTokenAsync(cancellationToken);
                if (refreshed)
                {
                    // Persist updated tokens
                    var updatedTokenData = new StoredTokenData
                    {
                        AccessToken = credential.Token.AccessToken,
                        RefreshToken = credential.Token.RefreshToken ?? tokenData.RefreshToken,
                        ExpiresInSeconds = credential.Token.ExpiresInSeconds,
                        IssuedUtc = credential.Token.IssuedUtc
                    };

                    profile.GoogleCalendarToken = JsonSerializer.Serialize(updatedTokenData);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }
            }

            return new Google.Apis.Calendar.v3.CalendarService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "SpeakHalo"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create Google Calendar service for user {UserId}", userId);
            return null;
        }
    }

    public Task<Result<string>> GetAuthorizationUrlAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var clientId = _configuration["GoogleCalendar:ClientId"];
            var redirectUri = _configuration["GoogleCalendar:RedirectUri"];
            var scopes = _configuration["GoogleCalendar:Scopes"];

            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(redirectUri))
                return Task.FromResult(Result<string>.Failure("Google Calendar is not configured"));

            var state = _encryptionService.Encrypt(userId);
            var encodedState = Uri.EscapeDataString(state);

            var url = $"https://accounts.google.com/o/oauth2/v2/auth" +
                      $"?client_id={Uri.EscapeDataString(clientId)}" +
                      $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                      $"&response_type=code" +
                      $"&scope={Uri.EscapeDataString(scopes ?? "https://www.googleapis.com/auth/calendar")}" +
                      $"&access_type=offline" +
                      $"&prompt=consent" +
                      $"&state={encodedState}";

            return Task.FromResult(Result<string>.Success(url));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate authorization URL for user {UserId}", userId);
            return Task.FromResult(Result<string>.Failure($"Failed to generate authorization URL: {ex.Message}"));
        }
    }

    public async Task<Result<bool>> HandleOAuthCallbackAsync(string code, string state, CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = _encryptionService.Decrypt(state);

            var clientId = _configuration["GoogleCalendar:ClientId"]!;
            var clientSecret = _configuration["GoogleCalendar:ClientSecret"]!;
            var redirectUri = _configuration["GoogleCalendar:RedirectUri"]!;

            var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = new ClientSecrets
                {
                    ClientId = clientId,
                    ClientSecret = clientSecret
                },
                Scopes = new[] { Google.Apis.Calendar.v3.CalendarService.Scope.Calendar }
            });

            var tokenResponse = await flow.ExchangeCodeForTokenAsync(
                userId, code, redirectUri, cancellationToken);

            var tokenData = new StoredTokenData
            {
                AccessToken = tokenResponse.AccessToken,
                RefreshToken = tokenResponse.RefreshToken,
                ExpiresInSeconds = tokenResponse.ExpiresInSeconds,
                IssuedUtc = tokenResponse.IssuedUtc
            };

            var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
            if (user?.Profile == null)
                return Result<bool>.Failure("User not found");

            user.Profile.GoogleCalendarToken = JsonSerializer.Serialize(tokenData);
            user.Profile.GoogleCalendarConnected = true;
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Google Calendar connected for user {UserId}", userId);
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle OAuth callback");
            return Result<bool>.Failure($"Failed to connect Google Calendar: {ex.Message}");
        }
    }

    public async Task<Result<bool>> DisconnectAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
            if (user?.Profile == null)
                return Result<bool>.Failure("User not found");

            // Revoke the token with Google
            if (!string.IsNullOrEmpty(user.Profile.GoogleCalendarToken))
            {
                try
                {
                    var tokenData = JsonSerializer.Deserialize<StoredTokenData>(user.Profile.GoogleCalendarToken);

                    if (tokenData != null)
                    {
                        using var httpClient = new HttpClient();
                        await httpClient.PostAsync(
                            $"https://oauth2.googleapis.com/revoke?token={tokenData.AccessToken}",
                            null, cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to revoke Google token for user {UserId}, clearing locally", userId);
                }
            }

            user.Profile.GoogleCalendarToken = null;
            user.Profile.GoogleCalendarConnected = false;
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Google Calendar disconnected for user {UserId}", userId);
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to disconnect Google Calendar for user {UserId}", userId);
            return Result<bool>.Failure($"Failed to disconnect Google Calendar: {ex.Message}");
        }
    }

    public async Task<Result<bool>> IsConnectedAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
            var connected = user?.Profile?.GoogleCalendarConnected ?? false;
            return Result<bool>.Success(connected);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check calendar connection for user {UserId}", userId);
            return Result<bool>.Failure($"Failed to check connection: {ex.Message}");
        }
    }

    public async Task<Result<string>> CreateEventAsync(string userId, string summary, string description, DateTime startTime, DateTime endTime, string? location = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var service = await GetGoogleCalendarServiceAsync(userId, cancellationToken);
            if (service == null)
                return Result<string>.Failure("Google Calendar not connected");

            using (service)
            {
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
            var service = await GetGoogleCalendarServiceAsync(userId, cancellationToken);
            if (service == null)
                return Result<bool>.Failure("Google Calendar not connected");

            using (service)
            {
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
            var service = await GetGoogleCalendarServiceAsync(userId, cancellationToken);
            if (service == null)
                return Result<bool>.Failure("Google Calendar not connected");

            using (service)
            {
                var request = service.Events.Delete("primary", eventId);
                await request.ExecuteAsync(cancellationToken);

                return Result<bool>.Success(true);
            }
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
            var service = await GetGoogleCalendarServiceAsync(userId, cancellationToken);
            if (service == null)
                return Result<List<object>>.Failure("Google Calendar not connected");

            using (service)
            {
                var request = service.Events.List("primary");
                request.TimeMin = startDate;
                request.TimeMax = endDate;
                request.ShowDeleted = false;
                request.SingleEvents = true;
                request.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;

                var events = await request.ExecuteAsync(cancellationToken);

                var eventList = events.Items?.Select(e => new
                {
                    id = e.Id,
                    summary = e.Summary,
                    description = e.Description,
                    start = e.Start.DateTime,
                    end = e.End.DateTime,
                    location = e.Location
                }).Cast<object>().ToList() ?? new List<object>();

                return Result<List<object>>.Success(eventList);
            }
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

    private class StoredTokenData
    {
        public string AccessToken { get; set; } = string.Empty;
        public string? RefreshToken { get; set; }
        public long? ExpiresInSeconds { get; set; }
        public DateTime IssuedUtc { get; set; }
    }
}

#pragma warning restore CS0618
