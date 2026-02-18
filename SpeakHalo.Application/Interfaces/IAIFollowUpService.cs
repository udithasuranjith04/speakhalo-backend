using SpeakHalo.Domain.Enums;

namespace SpeakHalo.Application.Interfaces;

public interface IAIFollowUpService
{
    /// <summary>
    /// Schedule a follow-up message after a call ends
    /// </summary>
    Task<FollowUpResult> ScheduleFollowUpAsync(string callId, int delayMinutes = 5);

    /// <summary>
    /// Process and send pending follow-up messages (called by background job)
    /// </summary>
    Task<int> ProcessPendingFollowUpsAsync();

    /// <summary>
    /// Generate personalized follow-up message based on call outcome
    /// </summary>
    Task<string> GenerateFollowUpMessageAsync(string callId);

    /// <summary>
    /// Cancel a scheduled follow-up
    /// </summary>
    Task<bool> CancelFollowUpAsync(string followUpId);

    /// <summary>
    /// Get follow-up statistics for a user
    /// </summary>
    Task<FollowUpStats> GetFollowUpStatsAsync(string userId);

    /// <summary>
    /// Manually send a follow-up message immediately
    /// </summary>
    Task<FollowUpResult> SendFollowUpNowAsync(string callId);
}

public class FollowUpResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? FollowUpId { get; set; }
    public DateTime? ScheduledFor { get; set; }
    public string? MessagePreview { get; set; }
}

public class FollowUpStats
{
    public int TotalScheduled { get; set; }
    public int TotalSent { get; set; }
    public int TotalFailed { get; set; }
    public int PendingSend { get; set; }
    public decimal DeliveryRate { get; set; }
    public decimal ResponseRate { get; set; }
    public Dictionary<CallOutcome, int> ByOutcome { get; set; } = new();
}
