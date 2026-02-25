using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpeakHalo.Application.DTOs;
using SpeakHalo.Application.DTOs.Auth;
using SpeakHalo.Application.DTOs.Payment;
using SpeakHalo.Application.Interfaces;
using SpeakHalo.Domain.Entities;
using SpeakHalo.Domain.Enums;
using SpeakHalo.Domain.Interfaces.Repositories;

namespace SpeakHalo.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ITrialInviteService _trialInviteService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAdminNotificationService _adminNotifications;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IAuthService authService,
        ITrialInviteService trialInviteService,
        IUnitOfWork unitOfWork,
        IAdminNotificationService adminNotifications,
        ILogger<AuthController> logger)
    {
        _authService = authService;
        _trialInviteService = trialInviteService;
        _unitOfWork = unitOfWork;
        _adminNotifications = adminNotifications;
        _logger = logger;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Login attempt for email: {Email}", request.Email);

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _authService.LoginAsync(request, ipAddress, cancellationToken);

        if (!result.IsSuccess)
        {
            _logger.LogWarning("Login failed for {Email}", request.Email);
            return Unauthorized(result);
        }

        _logger.LogInformation("User logged in successfully: {Email}", request.Email);
        return Ok(result);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Token refresh attempt");

        var result = await _authService.RefreshTokenAsync(request.RefreshToken, cancellationToken);

        if (!result.IsSuccess)
        {
            _logger.LogWarning("Token refresh failed");
            return Unauthorized(result);
        }

        _logger.LogInformation("Token refreshed successfully");
        return Ok(result);
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        _logger.LogInformation("Logout attempt for user: {UserId}", userId);

        var result = await _authService.LogoutAsync(userId, cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }

        _logger.LogInformation("User logged out successfully: {UserId}", userId);
        return Ok(result);
    }

    [HttpPost("revoke-token")]
    [Authorize]
    public async Task<IActionResult> RevokeToken([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Token revocation attempt");

        var result = await _authService.RevokeTokenAsync(request.RefreshToken, cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }

        _logger.LogInformation("Token revoked successfully");
        return Ok(result);
    }

    /// <summary>
    /// Force change password for admin-created accounts with temporary passwords.
    /// Called after login returns RequiresPasswordChange = true.
    /// </summary>
    [HttpPost("force-change-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForceChangePassword([FromBody] ForceChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _authService.ForceChangePasswordAsync(request, ipAddress, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// Request a password reset email. Always returns success to prevent email enumeration.
    /// </summary>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        var result = await _authService.ForgotPasswordAsync(request.Email, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Reset password using token from email. User must log in again after reset.
    /// </summary>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        var result = await _authService.ResetPasswordAsync(request, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPost("totp/login-verify")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyLoginTotp([FromBody] TotpLoginVerifyRequest request, CancellationToken cancellationToken)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _authService.VerifyLoginTotpAsync(request.TwoFactorToken, request.Code, ipAddress, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPost("totp/setup")]
    [Authorize]
    public async Task<IActionResult> SetupTotp(CancellationToken cancellationToken)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var result = await _authService.SetupTotpAsync(userId, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPost("totp/verify")]
    [Authorize]
    public async Task<IActionResult> VerifyTotp([FromBody] TotpVerifyRequest request, CancellationToken cancellationToken)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var result = await _authService.VerifyTotpAsync(userId, request.Code, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPost("totp/disable")]
    [Authorize]
    public async Task<IActionResult> DisableTotp(CancellationToken cancellationToken)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var result = await _authService.DisableTotpAsync(userId, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// Register a new account using a trial invite token (no payment required).
    /// </summary>
    [HttpPost("register-trial")]
    [AllowAnonymous]
    public async Task<IActionResult> RegisterTrial(
        [FromBody] RegisterTrialRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password) ||
            string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Token))
            return BadRequest(new { error = "Email, name, password, and token are required" });

        // Validate the trial invite token
        var validation = await _trialInviteService.ValidateTokenAsync(request.Token, cancellationToken);
        if (!validation.IsSuccess || validation.Data == null || !validation.Data.IsValid)
            return BadRequest(new { error = validation.Data?.Error ?? "Invalid trial invite" });

        // Check if email matches invite restriction
        if (!string.IsNullOrEmpty(validation.Data.Email) &&
            !string.Equals(validation.Data.Email, request.Email.Trim(), StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { error = "This invite is restricted to a different email address" });

        // Check if user already exists
        var existing = await _unitOfWork.Repository<User>()
            .FindAsync(u => u.Email == request.Email.Trim().ToLowerInvariant(), cancellationToken);
        if (existing.Any())
            return BadRequest(new { error = "An account with this email already exists" });

        // Create user
        var user = new User
        {
            Name = request.Name.Trim(),
            Email = request.Email.Trim().ToLowerInvariant(),
            HashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password, workFactor: 12),
            Role = UserRole.Owner,
            BusinessName = request.BusinessName?.Trim() ?? "",
            IsActive = true,
            PasswordMustChange = false,
            TemporaryPassword = false
        };
        await _unitOfWork.Repository<User>().AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Create UserAuth
        var userAuth = new UserAuth
        {
            UserId = user.Id,
            HashedPassword = user.HashedPassword,
            EmailVerified = false,
            PasswordMustChange = false,
            TemporaryPassword = false
        };
        await _unitOfWork.Repository<UserAuth>().AddAsync(userAuth, cancellationToken);

        // Create UserProfile
        var userProfile = new UserProfile
        {
            UserId = user.Id,
            BusinessName = request.BusinessName?.Trim() ?? "",
            Industry = ServiceType.Cleaning
        };
        await _unitOfWork.Repository<UserProfile>().AddAsync(userProfile, cancellationToken);

        // Create UserSubscription (will be updated by RedeemTrialAsync)
        var subscription = new UserSubscription
        {
            UserId = user.Id,
            Plan = "trial",
            RequiresPayment = false,
            HasActivePayment = false,
            BillingCycleStartDate = DateTime.UtcNow,
            NextBillingDate = DateTime.UtcNow.AddMonths(1)
        };
        await _unitOfWork.Repository<UserSubscription>().AddAsync(subscription, cancellationToken);

        // Create UserTrial
        var userTrial = new UserTrial { UserId = user.Id, IsTrial = true };
        await _unitOfWork.Repository<UserTrial>().AddAsync(userTrial, cancellationToken);

        // Create default VoiceAgent
        var voiceAgent = new VoiceAgent
        {
            UserId = user.Id,
            Name = "AI Receptionist",
            VoiceName = "Kore",
            Language = "en-US",
            SystemPrompt = $"You are an AI receptionist for {(request.BusinessName?.Trim() ?? "the business")}. Help callers with scheduling, quotes, and general questions. Be friendly and professional.",
            MaxCallDurationMinutes = 30,
            IsActive = true
        };
        await _unitOfWork.Repository<VoiceAgent>().AddAsync(voiceAgent, cancellationToken);

        // Create default AgentProfile
        var agentProfile = new AgentProfile
        {
            UserId = user.Id,
            GreetingMessage = $"Hi, thanks for calling {(request.BusinessName?.Trim() ?? "us")}. How can I help you today?",
            ClosingMessage = "Thank you for calling! Have a great day.",
            Language = "en",
            Tone = "professional",
            TransferOnComplexIssues = true,
            EnableTroubleshooting = true
        };
        await _unitOfWork.Repository<AgentProfile>().AddAsync(agentProfile, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Redeem the trial invite — sets plan limits and trial dates
        var redeemResult = await _trialInviteService.RedeemTrialAsync(request.Token, user.Id, cancellationToken);
        if (!redeemResult.IsSuccess)
        {
            _logger.LogWarning("Trial redemption failed for user {UserId}: {Error}", user.Id, redeemResult.Errors.FirstOrDefault());
        }

        // Generate auth tokens for auto-login
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var loginResult = await _authService.GenerateTokensForUserAsync(user.Id, ipAddress, cancellationToken);

        if (!loginResult.IsSuccess)
            return StatusCode(500, new { error = "Account created but auto-login failed" });

        _logger.LogInformation("User {UserId} registered via trial invite", user.Id);

        // Notify admins of new signup
        _ = _adminNotifications.NotifyNewUserSignupAsync(user.Id, user.Email, request.Name);

        return Ok(loginResult);
    }
}
