using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OtpNet;
using SpeakHalo.Application.DTOs.Auth;
using SpeakHalo.Application.Interfaces;
using SpeakHalo.Domain.Entities;
using SpeakHalo.Domain.Interfaces.Repositories;
using SpeakHalo.Shared.Constants;
using SpeakHalo.Shared.Helpers;
using SpeakHalo.Shared.Models;

#pragma warning disable CS0618

namespace SpeakHalo.Application.Services.Auth;

public class AuthService : IAuthService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly JwtTokenGenerator _jwtTokenGenerator;
    private readonly IEmailService _emailService;
    private readonly ILogger<AuthService> _logger;
    private readonly string _frontendBaseUrl;

    public AuthService(
        IUnitOfWork unitOfWork,
        JwtTokenGenerator jwtTokenGenerator,
        IEmailService emailService,
        IConfiguration configuration,
        ILogger<AuthService> logger)
    {
        _unitOfWork = unitOfWork;
        _jwtTokenGenerator = jwtTokenGenerator;
        _emailService = emailService;
        _logger = logger;
        _frontendBaseUrl = configuration["Frontend:BaseUrl"]?.TrimEnd('/') ?? "https://app.speakhalo.com";
    }

    private const int MaxFailedAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan PasswordResetTokenLifetime = TimeSpan.FromHours(1);

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private async Task<UserAuth?> GetUserAuthAsync(string userId, CancellationToken cancellationToken)
    {
        var userAuths = await _unitOfWork.Repository<UserAuth>()
            .FindAsync(a => a.UserId == userId, cancellationToken);
        return userAuths.FirstOrDefault();
    }

    private bool IsLockedOut(UserAuth userAuth, out int remainingMinutes)
    {
        remainingMinutes = 0;
        if (userAuth.LockoutEndTime.HasValue && userAuth.LockoutEndTime.Value > DateTime.UtcNow)
        {
            remainingMinutes = (int)Math.Ceiling((userAuth.LockoutEndTime.Value - DateTime.UtcNow).TotalMinutes);
            return true;
        }
        return false;
    }

    private async Task TrackFailedAttemptAsync(UserAuth userAuth, CancellationToken cancellationToken)
    {
        userAuth.FailedLoginAttempts++;
        if (userAuth.FailedLoginAttempts >= MaxFailedAttempts)
        {
            userAuth.LockoutEndTime = DateTime.UtcNow.Add(LockoutDuration);
            _logger.LogWarning("Account locked for user {UserId} after {Attempts} failed attempts",
                userAuth.UserId, userAuth.FailedLoginAttempts);
        }
        _unitOfWork.Repository<UserAuth>().Update(userAuth);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private void ResetFailedAttempts(UserAuth userAuth)
    {
        if (userAuth.FailedLoginAttempts > 0 || userAuth.LockoutEndTime.HasValue)
        {
            userAuth.FailedLoginAttempts = 0;
            userAuth.LockoutEndTime = null;
            _unitOfWork.Repository<UserAuth>().Update(userAuth);
        }
    }

    private async Task RevokeAllRefreshTokensAsync(string userId, CancellationToken cancellationToken)
    {
        var refreshTokens = await _unitOfWork.Repository<RefreshToken>()
            .FindAsync(rt => rt.UserId == userId && !rt.Revoked, cancellationToken);

        foreach (var token in refreshTokens)
        {
            token.Revoked = true;
            _unitOfWork.Repository<RefreshToken>().Update(token);
        }
    }

    public async Task<Result<LoginResponse>> LoginAsync(LoginRequest request, string? ipAddress = null, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByEmailAsync(request.Email, cancellationToken);
        if (user == null)
            return Result<LoginResponse>.Failure(ErrorMessages.InvalidCredentials);

        var userAuth = await GetUserAuthAsync(user.Id, cancellationToken);

        // Check account lockout
        if (userAuth != null && IsLockedOut(userAuth, out var remaining))
            return Result<LoginResponse>.Failure($"Account is temporarily locked. Try again in {remaining} minute(s).");

        // Verify password
        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.HashedPassword))
        {
            if (userAuth != null)
                await TrackFailedAttemptAsync(userAuth, cancellationToken);
            return Result<LoginResponse>.Failure(ErrorMessages.InvalidCredentials);
        }

        if (!user.IsActive)
            return Result<LoginResponse>.Failure("Account is inactive");

        // Check email verification
        if (userAuth != null && !userAuth.EmailVerified)
        {
            ResetFailedAttempts(userAuth);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result<LoginResponse>.Failure("Please verify your email address before logging in.");
        }

        // Reset failed attempts on successful credential verification
        if (userAuth != null)
            ResetFailedAttempts(userAuth);

        // Password change required — issue a short-lived, purpose-scoped token
        // This token proves the user authenticated but can ONLY be used to change password
        if (userAuth != null && userAuth.PasswordMustChange)
        {
            var passwordChangeToken = _jwtTokenGenerator.GeneratePasswordChangeToken(user.Id);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Password change required for user {UserId}, issued password-change token", user.Id);

            return Result<LoginResponse>.Success(new LoginResponse
            {
                RequiresPasswordChange = true,
                PasswordChangeToken = passwordChangeToken,
                Message = "You must change your password before continuing.",
                User = new UserDto { Id = user.Id }
            });
        }

        // Normal login — issue access + refresh tokens
        var accessToken = _jwtTokenGenerator.GenerateAccessToken(user.Id, user.Email, user.Role.ToString());
        var refreshToken = _jwtTokenGenerator.GenerateRefreshToken();

        var refreshTokenEntity = new RefreshToken
        {
            UserId = user.Id,
            Token = HashToken(refreshToken),
            ExpiresAt = DateTime.UtcNow.AddDays(AppConstants.RefreshTokenExpirationDays),
            DeviceName = request.DeviceName,
            DeviceType = request.DeviceType,
            Browser = request.Browser,
            Os = request.Os,
            IpAddress = ipAddress
        };

        await _unitOfWork.Repository<RefreshToken>().AddAsync(refreshTokenEntity, cancellationToken);

        user.LastLogin = DateTime.UtcNow;
        _unitOfWork.Users.Update(user);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<LoginResponse>.Success(new LoginResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(AppConstants.AccessTokenExpirationMinutes),
            User = MapToUserDto(user)
        });
    }

    public async Task<Result<LoginResponse>> ForceChangePasswordAsync(ForceChangePasswordRequest request, string? ipAddress = null, CancellationToken cancellationToken = default)
    {
        // Validate the signed password-change token (issued by LoginAsync)
        var userId = _jwtTokenGenerator.ValidatePasswordChangeToken(request.PasswordChangeToken);
        if (string.IsNullOrEmpty(userId))
            return Result<LoginResponse>.Failure("Invalid or expired password change token. Please log in again.");

        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
        if (user == null || !user.IsActive)
            return Result<LoginResponse>.Failure("Invalid or expired password change token. Please log in again.");

        var userAuth = await GetUserAuthAsync(user.Id, cancellationToken);
        if (userAuth == null)
            return Result<LoginResponse>.Failure("Invalid or expired password change token. Please log in again.");

        // Verify PasswordMustChange is still true (token replay after password already changed)
        if (!userAuth.PasswordMustChange)
            return Result<LoginResponse>.Failure("Password has already been changed. Please log in.");

        // Set new password on UserAuth (source of truth)
        var newHashedPassword = BCrypt.Net.BCrypt.HashPassword(request.NewPassword, workFactor: AppConstants.BcryptWorkFactor);
        userAuth.HashedPassword = newHashedPassword;
        userAuth.PasswordMustChange = false;
        userAuth.TemporaryPassword = false;
        userAuth.PasswordChangedAt = DateTime.UtcNow;
        userAuth.FailedLoginAttempts = 0;
        userAuth.LockoutEndTime = null;
        _unitOfWork.Repository<UserAuth>().Update(userAuth);

        // Sync to User entity (legacy compatibility)
        user.HashedPassword = newHashedPassword;
        user.PasswordMustChange = false;
        user.TemporaryPassword = false;
        user.LastLogin = DateTime.UtcNow;
        _unitOfWork.Users.Update(user);

        // Issue JWT tokens — user is now fully authenticated
        var accessToken = _jwtTokenGenerator.GenerateAccessToken(user.Id, user.Email, user.Role.ToString());
        var refreshToken = _jwtTokenGenerator.GenerateRefreshToken();

        var refreshTokenEntity = new RefreshToken
        {
            UserId = user.Id,
            Token = HashToken(refreshToken),
            ExpiresAt = DateTime.UtcNow.AddDays(AppConstants.RefreshTokenExpirationDays),
            IpAddress = ipAddress
        };

        await _unitOfWork.Repository<RefreshToken>().AddAsync(refreshTokenEntity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User {UserId} completed forced password change", user.Id);

        return Result<LoginResponse>.Success(new LoginResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(AppConstants.AccessTokenExpirationMinutes),
            User = MapToUserDto(user)
        });
    }

    public async Task<Result> ForgotPasswordAsync(string email, CancellationToken cancellationToken = default)
    {
        // SECURITY: Always return the same success message to prevent email enumeration.
        // Response timing is equalized via dummy BCrypt hash when user not found.
        const string successMessage = "If an account with that email exists, a password reset link has been sent.";

        var user = await _unitOfWork.Users.GetByEmailAsync(email, cancellationToken);
        if (user == null || !user.IsActive)
        {
            BCrypt.Net.BCrypt.HashPassword("dummy-timing-equalization", workFactor: 10);
            return Result.Success(successMessage);
        }

        var userAuth = await GetUserAuthAsync(user.Id, cancellationToken);
        if (userAuth == null)
        {
            BCrypt.Net.BCrypt.HashPassword("dummy-timing-equalization", workFactor: 10);
            return Result.Success(successMessage);
        }

        // Rate limit: don't issue a new token if one was created less than 2 minutes ago
        if (userAuth.PasswordResetExpires.HasValue && userAuth.PasswordResetExpires.Value > DateTime.UtcNow)
        {
            var tokenIssuedAt = userAuth.PasswordResetExpires.Value.Subtract(PasswordResetTokenLifetime);
            if (tokenIssuedAt > DateTime.UtcNow.AddMinutes(-2))
                return Result.Success(successMessage);
        }

        // Generate crypto-secure reset token (32 bytes = 64 hex chars)
        var resetTokenBytes = RandomNumberGenerator.GetBytes(32);
        var resetToken = Convert.ToHexString(resetTokenBytes).ToLowerInvariant();

        // Store BCrypt hash of token — never store plaintext
        userAuth.PasswordResetToken = BCrypt.Net.BCrypt.HashPassword(resetToken, workFactor: 10);
        userAuth.PasswordResetExpires = DateTime.UtcNow.Add(PasswordResetTokenLifetime);

        _unitOfWork.Repository<UserAuth>().Update(userAuth);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Send reset email — failure is logged but not exposed to caller
        try
        {
            var resetUrl = $"{_frontendBaseUrl}/reset-password?token={resetToken}&email={Uri.EscapeDataString(user.Email)}";
            await _emailService.SendEmailAsync(
                user.Email,
                "SpeakHalo - Reset Your Password",
                GeneratePasswordResetEmail(user.Name, resetUrl),
                cancellationToken: cancellationToken);

            _logger.LogInformation("Password reset email sent for user {UserId}", user.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send password reset email for user {UserId}", user.Id);
        }

        return Result.Success(successMessage);
    }

    public async Task<Result> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        // Use generic error to prevent information leakage
        const string genericError = "Invalid or expired reset token.";

        var user = await _unitOfWork.Users.GetByEmailAsync(request.Email, cancellationToken);
        if (user == null)
            return Result.Failure(genericError);

        var userAuth = await GetUserAuthAsync(user.Id, cancellationToken);
        if (userAuth == null)
            return Result.Failure(genericError);

        // Verify reset token exists
        if (string.IsNullOrEmpty(userAuth.PasswordResetToken) || !userAuth.PasswordResetExpires.HasValue)
            return Result.Failure(genericError);

        // Check expiry — clear token if expired
        if (userAuth.PasswordResetExpires.Value < DateTime.UtcNow)
        {
            userAuth.PasswordResetToken = null;
            userAuth.PasswordResetExpires = null;
            _unitOfWork.Repository<UserAuth>().Update(userAuth);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Failure("Reset token has expired. Please request a new password reset.");
        }

        // Verify token against BCrypt hash
        if (!BCrypt.Net.BCrypt.Verify(request.Token, userAuth.PasswordResetToken))
            return Result.Failure(genericError);

        // Token is valid — set new password
        var newHashedPassword = BCrypt.Net.BCrypt.HashPassword(request.NewPassword, workFactor: AppConstants.BcryptWorkFactor);
        userAuth.HashedPassword = newHashedPassword;
        userAuth.PasswordResetToken = null;
        userAuth.PasswordResetExpires = null;
        userAuth.PasswordMustChange = false;
        userAuth.TemporaryPassword = false;
        userAuth.PasswordChangedAt = DateTime.UtcNow;
        // NOTE: Do NOT clear FailedLoginAttempts or LockoutEndTime here.
        // If the account was locked due to an attack, the lockout should persist
        // until it expires naturally. The user can still log in after lockout expires.
        _unitOfWork.Repository<UserAuth>().Update(userAuth);

        // Sync to User entity (legacy compatibility)
        user.HashedPassword = newHashedPassword;
        user.PasswordMustChange = false;
        user.TemporaryPassword = false;
        _unitOfWork.Users.Update(user);

        // Invalidate ALL existing refresh tokens — forces re-authentication on all devices
        await RevokeAllRefreshTokensAsync(user.Id, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Password reset completed for user {UserId}", user.Id);

        return Result.Success("Password has been reset successfully. Please log in with your new password.");
    }

    public async Task<Result<LoginResponse>> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var hashedToken = HashToken(refreshToken);
        var token = await _unitOfWork.Repository<RefreshToken>()
            .FirstOrDefaultAsync(rt => rt.Token == hashedToken, cancellationToken);

        if (token == null || token.Revoked || token.ExpiresAt < DateTime.UtcNow)
            return Result<LoginResponse>.Failure(ErrorMessages.InvalidToken);

        var user = await _unitOfWork.Users.GetByIdAsync(token.UserId, cancellationToken);
        if (user == null || !user.IsActive)
            return Result<LoginResponse>.Failure(ErrorMessages.InvalidToken);

        // Rotate: revoke old, issue new
        var newAccessToken = _jwtTokenGenerator.GenerateAccessToken(user.Id, user.Email, user.Role.ToString());
        var newRefreshToken = _jwtTokenGenerator.GenerateRefreshToken();

        token.Revoked = true;
        _unitOfWork.Repository<RefreshToken>().Update(token);

        var newRefreshTokenEntity = new RefreshToken
        {
            UserId = user.Id,
            Token = HashToken(newRefreshToken),
            ExpiresAt = DateTime.UtcNow.AddDays(AppConstants.RefreshTokenExpirationDays),
            DeviceName = token.DeviceName,
            DeviceType = token.DeviceType,
            Browser = token.Browser,
            Os = token.Os,
            IpAddress = token.IpAddress
        };

        await _unitOfWork.Repository<RefreshToken>().AddAsync(newRefreshTokenEntity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<LoginResponse>.Success(new LoginResponse
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(AppConstants.AccessTokenExpirationMinutes),
            User = MapToUserDto(user)
        });
    }

    public async Task<Result> RevokeTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var hashedToken = HashToken(refreshToken);
        var token = await _unitOfWork.Repository<RefreshToken>()
            .FirstOrDefaultAsync(rt => rt.Token == hashedToken, cancellationToken);

        if (token == null)
            return Result.Failure(ErrorMessages.InvalidToken);

        token.Revoked = true;
        _unitOfWork.Repository<RefreshToken>().Update(token);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success("Token revoked successfully");
    }

    public async Task<Result> LogoutAsync(string userId, CancellationToken cancellationToken = default)
    {
        await RevokeAllRefreshTokensAsync(userId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success("Logged out successfully");
    }

    public async Task<Result<LoginResponse>> GenerateTokensForUserAsync(string userId, string? ipAddress = null, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
        if (user == null)
            return Result<LoginResponse>.Failure("User not found");

        if (!user.IsActive)
            return Result<LoginResponse>.Failure("Account is inactive");

        var accessToken = _jwtTokenGenerator.GenerateAccessToken(user.Id, user.Email, user.Role.ToString());
        var refreshToken = _jwtTokenGenerator.GenerateRefreshToken();

        var refreshTokenEntity = new RefreshToken
        {
            UserId = user.Id,
            Token = HashToken(refreshToken),
            ExpiresAt = DateTime.UtcNow.AddDays(AppConstants.RefreshTokenExpirationDays),
            IpAddress = ipAddress
        };

        await _unitOfWork.Repository<RefreshToken>().AddAsync(refreshTokenEntity, cancellationToken);

        user.LastLogin = DateTime.UtcNow;
        _unitOfWork.Users.Update(user);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<LoginResponse>.Success(new LoginResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(AppConstants.AccessTokenExpirationMinutes),
            User = MapToUserDto(user)
        });
    }

    private const int Max2faAttempts = 5;
    private static readonly TimeSpan TwoFactorLockoutDuration = TimeSpan.FromMinutes(15);

    public async Task<Result<TotpSetupResponse>> SetupTotpAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
        if (user == null)
            return Result<TotpSetupResponse>.Failure("User not found");

        var userAuth = await GetUserAuthAsync(userId, cancellationToken);
        if (userAuth == null)
            return Result<TotpSetupResponse>.Failure("User authentication record not found");

        // Generate a 20-byte random secret
        var secretBytes = RandomNumberGenerator.GetBytes(20);
        var base32Secret = Base32Encoding.ToString(secretBytes);

        // Build OTP URI for authenticator apps
        var otpUri = $"otpauth://totp/SpeakHalo:{Uri.EscapeDataString(user.Email)}?secret={base32Secret}&issuer=SpeakHalo&digits=6&period=30";

        // Generate 8 backup codes
        var backupCodes = new List<string>();
        for (var i = 0; i < 8; i++)
        {
            var codeBytes = RandomNumberGenerator.GetBytes(4);
            var code = (BitConverter.ToUInt32(codeBytes, 0) % 100000000).ToString("D8");
            backupCodes.Add(code);
        }

        // Store secret and backup codes (TOTP not yet enabled until verify)
        userAuth.TotpSecret = base32Secret;
        userAuth.TotpBackupCodes = JsonSerializer.Serialize(backupCodes);
        // TotpEnabled stays false until verified

        _unitOfWork.Repository<UserAuth>().Update(userAuth);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("TOTP setup initiated for user {UserId}", userId);

        return Result<TotpSetupResponse>.Success(new TotpSetupResponse
        {
            QrCodeUri = otpUri,
            Secret = base32Secret,
            BackupCodes = backupCodes
        });
    }

    public async Task<Result> VerifyTotpAsync(string userId, string code, CancellationToken cancellationToken = default)
    {
        var userAuth = await GetUserAuthAsync(userId, cancellationToken);
        if (userAuth == null)
            return Result.Failure("User authentication record not found");

        if (string.IsNullOrEmpty(userAuth.TotpSecret))
            return Result.Failure("TOTP has not been set up. Please call setup first.");

        // Check 2FA lockout
        if (userAuth.TwoFactorLockoutEndTime.HasValue && userAuth.TwoFactorLockoutEndTime.Value > DateTime.UtcNow)
        {
            var remaining = (int)Math.Ceiling((userAuth.TwoFactorLockoutEndTime.Value - DateTime.UtcNow).TotalMinutes);
            return Result.Failure($"Too many failed attempts. Try again in {remaining} minute(s).");
        }

        var secretBytes = Base32Encoding.ToBytes(userAuth.TotpSecret);
        var totp = new Totp(secretBytes, step: 30, totpSize: 6);

        // Check TOTP code (window=1 allows +-30s for clock drift)
        if (totp.VerifyTotp(code, out _, new VerificationWindow(previous: 1, future: 1)))
        {
            userAuth.TotpEnabled = true;
            userAuth.Failed2faAttempts = 0;
            userAuth.TwoFactorLockoutEndTime = null;
            _unitOfWork.Repository<UserAuth>().Update(userAuth);

            // Sync to User entity
            var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
            if (user != null)
            {
                user.TotpEnabled = true;
                _unitOfWork.Users.Update(user);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("TOTP enabled for user {UserId}", userId);
            return Result.Success("Two-factor authentication has been enabled.");
        }

        // Check backup codes
        if (!string.IsNullOrEmpty(userAuth.TotpBackupCodes))
        {
            var backupCodes = JsonSerializer.Deserialize<List<string>>(userAuth.TotpBackupCodes) ?? [];
            if (backupCodes.Contains(code))
            {
                backupCodes.Remove(code);
                userAuth.TotpBackupCodes = JsonSerializer.Serialize(backupCodes);
                userAuth.TotpEnabled = true;
                userAuth.Failed2faAttempts = 0;
                userAuth.TwoFactorLockoutEndTime = null;
                _unitOfWork.Repository<UserAuth>().Update(userAuth);

                var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
                if (user != null)
                {
                    user.TotpEnabled = true;
                    _unitOfWork.Users.Update(user);
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("TOTP enabled for user {UserId} using backup code", userId);
                return Result.Success("Two-factor authentication has been enabled (backup code consumed).");
            }
        }

        // Invalid code — track failed attempt
        userAuth.Failed2faAttempts++;
        if (userAuth.Failed2faAttempts >= Max2faAttempts)
        {
            userAuth.TwoFactorLockoutEndTime = DateTime.UtcNow.Add(TwoFactorLockoutDuration);
            _logger.LogWarning("2FA locked out for user {UserId} after {Attempts} failed attempts",
                userId, userAuth.Failed2faAttempts);
        }

        _unitOfWork.Repository<UserAuth>().Update(userAuth);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Failure("Invalid verification code.");
    }

    public async Task<Result> DisableTotpAsync(string userId, CancellationToken cancellationToken = default)
    {
        var userAuth = await GetUserAuthAsync(userId, cancellationToken);
        if (userAuth == null)
            return Result.Failure("User authentication record not found");

        userAuth.TotpSecret = null;
        userAuth.TotpBackupCodes = null;
        userAuth.TotpEnabled = false;
        userAuth.Failed2faAttempts = 0;
        userAuth.TwoFactorLockoutEndTime = null;
        _unitOfWork.Repository<UserAuth>().Update(userAuth);

        // Sync to User entity
        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
        if (user != null)
        {
            user.TotpEnabled = false;
            _unitOfWork.Users.Update(user);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("TOTP disabled for user {UserId}", userId);

        return Result.Success("Two-factor authentication has been disabled.");
    }

    private static string GeneratePasswordResetEmail(string name, string resetUrl)
    {
        var encodedName = System.Net.WebUtility.HtmlEncode(name);
        return $@"<!DOCTYPE html>
<html><head><meta charset='utf-8'><meta name='viewport' content='width=device-width,initial-scale=1'></head>
<body style='font-family:-apple-system,BlinkMacSystemFont,""Segoe UI"",Roboto,sans-serif;line-height:1.6;color:#333;margin:0;padding:0;background:#f4f4f4;'>
<div style='max-width:600px;margin:40px auto;background:white;border-radius:8px;box-shadow:0 2px 4px rgba(0,0,0,0.1);overflow:hidden;'>
  <div style='background:linear-gradient(135deg,#667eea 0%,#764ba2 100%);padding:40px 30px;text-align:center;'>
    <h1 style='color:white;margin:0;font-size:28px;'>Reset Your Password</h1>
    <p style='color:rgba(255,255,255,0.9);margin:10px 0 0;font-size:16px;'>SpeakHalo</p>
  </div>
  <div style='padding:40px 30px;'>
    <p style='font-size:16px;'>Hi {encodedName},</p>
    <p style='font-size:16px;'>We received a request to reset your password. Click the button below to set a new password:</p>
    <div style='text-align:center;margin:30px 0;'>
      <a href='{resetUrl}' style='display:inline-block;background:linear-gradient(135deg,#667eea 0%,#764ba2 100%);color:white;padding:14px 32px;text-decoration:none;border-radius:6px;font-weight:600;font-size:16px;'>Reset Password</a>
    </div>
    <p style='font-size:14px;color:#666;'>This link will expire in <strong>1 hour</strong>.</p>
    <p style='font-size:14px;color:#666;'>If you didn't request this, you can safely ignore this email. Your password won't be changed.</p>
    <hr style='border:none;border-top:1px solid #eee;margin:30px 0;'>
    <p style='font-size:12px;color:#999;text-align:center;'>SpeakHalo — AI-Powered Cleaning Service Receptionist</p>
  </div>
</div>
</body></html>";
    }

    private static UserDto MapToUserDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            Avatar = user.Avatar,
            Role = user.Role.ToString(),
            BusinessName = user.BusinessName,
            Industry = user.Industry.ToString(),
            Plan = user.Plan,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            LastLogin = user.LastLogin,
            TotpEnabled = user.TotpEnabled,
            OnboardingCompleted = user.OnboardingCompleted,
            OnboardingStep = user.OnboardingStep
        };
    }
}

#pragma warning restore CS0618
