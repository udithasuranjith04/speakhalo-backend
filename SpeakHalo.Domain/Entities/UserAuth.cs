namespace SpeakHalo.Domain.Entities;

/// <summary>
/// Authentication and security-related user data
/// </summary>
public class UserAuth : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public string HashedPassword { get; set; } = string.Empty;
    public string? PasswordResetToken { get; set; }
    public DateTime? PasswordResetExpires { get; set; }
    public bool PasswordMustChange { get; set; } = false;
    public DateTime? PasswordExpiresAt { get; set; }
    public bool TemporaryPassword { get; set; } = false;
    public DateTime? PasswordChangedAt { get; set; }
    public DateTime? LastLogin { get; set; }

    // Two-Factor Authentication
    public string? TotpSecret { get; set; }
    public bool TotpEnabled { get; set; } = false;
    public string? TotpBackupCodes { get; set; }

    // Account Lockout (login)
    public int FailedLoginAttempts { get; set; } = 0;
    public DateTime? LockoutEndTime { get; set; }

    // 2FA Lockout (separate from login to avoid conflict)
    public int Failed2faAttempts { get; set; } = 0;
    public DateTime? TwoFactorLockoutEndTime { get; set; }

    // Security Settings
    public string? SecuritySettings { get; set; }

    // Email Verification
    public bool EmailVerified { get; set; } = false;
    public string? EmailVerificationToken { get; set; }
    public DateTime? EmailVerificationTokenExpiresAt { get; set; }
    public DateTime? EmailVerifiedAt { get; set; }

    public virtual User User { get; set; } = null!;
}
