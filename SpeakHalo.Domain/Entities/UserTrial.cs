namespace SpeakHalo.Domain.Entities;

/// <summary>
/// User trial information and tracking
/// </summary>
public class UserTrial : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public bool IsTrial { get; set; } = false;
    public DateTime? TrialStartedAt { get; set; }
    public DateTime? TrialEndsAt { get; set; }
    public int? TrialMinutesLimit { get; set; }
    public int? TrialDurationDays { get; set; }
    public string? TrialType { get; set; }

    // Admin Management
    public string? TrialAssignedByAdminId { get; set; }
    public string? TrialNotes { get; set; }

    // Expiration Warning Tracking
    public DateTime? LastExpirationWarningAt { get; set; }
    public int? LastWarningDaysRemaining { get; set; }

    public virtual User User { get; set; } = null!;
}
