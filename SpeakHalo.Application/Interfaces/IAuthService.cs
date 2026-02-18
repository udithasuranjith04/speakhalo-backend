using SpeakHalo.Application.DTOs.Auth;
using SpeakHalo.Shared.Models;

namespace SpeakHalo.Application.Interfaces;

public interface IAuthService
{
    Task<Result<LoginResponse>> LoginAsync(LoginRequest request, string? ipAddress = null, CancellationToken cancellationToken = default);
    Task<Result<LoginResponse>> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task<Result> RevokeTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task<Result> LogoutAsync(string userId, CancellationToken cancellationToken = default);
    Task<Result<LoginResponse>> GenerateTokensForUserAsync(string userId, string? ipAddress = null, CancellationToken cancellationToken = default);
    Task<Result<LoginResponse>> ForceChangePasswordAsync(ForceChangePasswordRequest request, string? ipAddress = null, CancellationToken cancellationToken = default);
    Task<Result> ForgotPasswordAsync(string email, CancellationToken cancellationToken = default);
    Task<Result> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default);
}
