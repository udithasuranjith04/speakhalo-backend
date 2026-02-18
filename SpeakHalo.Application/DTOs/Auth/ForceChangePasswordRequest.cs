namespace SpeakHalo.Application.DTOs.Auth;

public class ForceChangePasswordRequest
{
    /// <summary>
    /// Short-lived signed token issued by login when PasswordMustChange is true.
    /// Proves the user already authenticated with their temp password.
    /// </summary>
    public string PasswordChangeToken { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}
