namespace SpeakHalo.Application.DTOs.Payment;

public class CreateTrialInviteRequest
{
    public string PricingPlanId { get; set; } = string.Empty;
    public int TrialDays { get; set; } = 14;
    public string? Email { get; set; }
    public string? Notes { get; set; }
}

public class TrialInviteResponse
{
    public string Id { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string PricingPlanId { get; set; } = string.Empty;
    public string? PricingPlanName { get; set; }
    public int TrialDays { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsRedeemed { get; set; }
    public DateTime? RedeemedAt { get; set; }
    public string? RedeemedByUserId { get; set; }
    public string? Notes { get; set; }
    public string InviteUrl { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class TrialInviteCreatedResponse
{
    public string Id { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string InviteUrl { get; set; } = string.Empty;
}

public class TrialInviteValidationResponse
{
    public bool IsValid { get; set; }
    public string? Error { get; set; }
    public string? PricingPlanName { get; set; }
    public decimal? PlanPrice { get; set; }
    public int? TrialDays { get; set; }
    public string? Email { get; set; }
}
