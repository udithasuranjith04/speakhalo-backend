namespace SpeakHalo.Domain.Entities;

/// <summary>
/// Tracks phone number OTP verification for registration.
/// Prevents abuse by requiring SMS verification before account creation.
/// </summary>
public class PhoneVerification : BaseEntity
{
    public string PhoneNumber { get; set; } = string.Empty;
    public string OtpHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public int Attempts { get; set; } = 0;
    public int MaxAttempts { get; set; } = 3;
    public bool IsVerified { get; set; } = false;

    /// <summary>
    /// Token issued upon successful verification (GUID hex string).
    /// Must be presented during registration.
    /// </summary>
    public string? VerificationToken { get; set; }

    public DateTime? VerificationTokenExpiresAt { get; set; }
    public string? IpAddress { get; set; }
    public bool IsConsumed { get; set; } = false;
}
