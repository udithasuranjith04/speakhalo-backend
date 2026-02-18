using SpeakHalo.Shared.Models;

namespace SpeakHalo.Application.Interfaces;

public interface IEmailVerificationService
{
    /// <summary>
    /// Generate and send email verification token
    /// </summary>
    Task<Result> SendVerificationEmailAsync(string userId, string email, string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verify email with token
    /// </summary>
    Task<Result> VerifyEmailAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resend verification email
    /// </summary>
    Task<Result> ResendVerificationEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if email is verified
    /// </summary>
    Task<bool> IsEmailVerifiedAsync(string userId, CancellationToken cancellationToken = default);
}
