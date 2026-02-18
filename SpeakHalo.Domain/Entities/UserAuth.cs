namespace SpeakHalo.Domain.Entities;

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

    public string? TotpSecret { get; set; }
    public bool TotpEnabled { get; set; } = false;
    public string? TotpBackupCodes { get; set; }

    public int FailedLoginAttempts { get; set; } = 0;
    public DateTime? LockoutEndTime { get; set; }

    // 2FA lockout is separate from login lockout to avoid conflict
    public int Failed2faAttempts { get; set; } = 0;
    public DateTime? TwoFactorLockoutEndTime { get; set; }

    public string? SecuritySettings { get; set; }

    public bool EmailVerified { get; set; } = false;
    public string? EmailVerificationToken { get; set; }
    public DateTime? EmailVerificationTokenExpiresAt { get; set; }
    public DateTime? EmailVerifiedAt { get; set; }

    public virtual User User { get; set; } = null!;
}
