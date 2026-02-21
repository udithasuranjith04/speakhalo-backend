namespace SpeakHalo.Application.DTOs.Auth;

public class LoginResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public UserDto User { get; set; } = null!;
    public bool RequiresTwoFactor { get; set; }
    public bool RequiresPasswordChange { get; set; }

    /// <summary>
    /// Short-lived signed token for forced password change.
    /// Only present when RequiresPasswordChange is true.
    /// </summary>
    public string? PasswordChangeToken { get; set; }

    /// <summary>
    /// Short-lived signed token for 2FA verification during login.
    /// Only present when RequiresTwoFactor is true.
    /// </summary>
    public string? TwoFactorToken { get; set; }

    public string? Message { get; set; }
}
