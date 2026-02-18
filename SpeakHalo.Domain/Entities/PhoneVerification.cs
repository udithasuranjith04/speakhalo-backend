namespace SpeakHalo.Domain.Entities;

/// <summary>
/// Tracks phone number OTP verification for registration.
/// Prevents abuse by requiring SMS verification before account creation.
/// </summary>
public class PhoneVerification : BaseEntity
{
    /// <summary>
    /// Phone number in E.164 format (e.g., +14155552671)
    /// </summary>
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>
    /// SHA256 hash of the 6-digit OTP code
    /// </summary>
    public string OtpHash { get; set; } = string.Empty;

    /// <summary>
    /// When this OTP expires (5 minutes from creation)
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Number of verification attempts made
    /// </summary>
    public int Attempts { get; set; } = 0;

    /// <summary>
    /// Maximum allowed verification attempts
    /// </summary>
    public int MaxAttempts { get; set; } = 3;

    /// <summary>
    /// Whether this OTP has been successfully verified
    /// </summary>
    public bool IsVerified { get; set; } = false;

    /// <summary>
    /// Token issued upon successful verification (GUID hex string).
    /// Must be presented during registration.
    /// </summary>
    public string? VerificationToken { get; set; }

    /// <summary>
    /// When the verification token expires (15 minutes from verification)
    /// </summary>
    public DateTime? VerificationTokenExpiresAt { get; set; }

    /// <summary>
    /// IP address of the requester
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// True after registration consumes the verification token
    /// </summary>
    public bool IsConsumed { get; set; } = false;
}
