namespace SpeakHalo.Domain.Entities;

/// <summary>
/// Admin-created trial invitations. A unique token allows a user to sign up with a free trial
/// for a specific pricing plan without requiring payment upfront.
/// </summary>
public class TrialInvite : BaseEntity
{
    /// <summary>Crypto-random token used in the invite URL.</summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>Optional target email. If set, only this email can redeem the invite.</summary>
    public string? Email { get; set; }

    public string PricingPlanId { get; set; } = string.Empty;
    public int TrialDays { get; set; } = 14;

    public DateTime ExpiresAt { get; set; }

    public bool IsRedeemed { get; set; } = false;
    public DateTime? RedeemedAt { get; set; }
    public string? RedeemedByUserId { get; set; }

    public string CreatedByAdminId { get; set; } = string.Empty;
    public string? Notes { get; set; }

    public virtual PricingPlan PricingPlan { get; set; } = null!;
}
