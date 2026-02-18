using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SpeakHalo.Application.Interfaces;
using SpeakHalo.Domain.Entities;
using SpeakHalo.Domain.Interfaces.Repositories;
using SpeakHalo.Shared.Models;

namespace SpeakHalo.Application.Services;

public class EmailVerificationService : IEmailVerificationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailVerificationService> _logger;

    public EmailVerificationService(
        IUnitOfWork unitOfWork,
        IEmailService emailService,
        IConfiguration configuration,
        ILogger<EmailVerificationService> logger)
    {
        _unitOfWork = unitOfWork;
        _emailService = emailService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<Result> SendVerificationEmailAsync(
        string userId,
        string email,
        string name,
        CancellationToken cancellationToken = default)
    {
        var userAuths = await _unitOfWork.Repository<UserAuth>()
            .FindAsync(ua => ua.UserId == userId, cancellationToken);

        var userAuth = userAuths.FirstOrDefault();

        if (userAuth == null)
        {
            return Result.Failure("User authentication record not found");
        }

        if (userAuth.EmailVerified)
        {
            return Result.Failure("Email already verified");
        }

        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        // Store SHA-256 hash — never store plaintext tokens in the database
        userAuth.EmailVerificationToken = HashToken(token);
        userAuth.EmailVerificationTokenExpiresAt = DateTime.UtcNow.AddDays(1);

        _unitOfWork.Repository<UserAuth>().Update(userAuth);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var frontendUrl = _configuration["Frontend:BaseUrl"]
            ?? _configuration["Application:FrontendUrl"]
            ?? throw new InvalidOperationException("Frontend:BaseUrl or Application:FrontendUrl must be configured");
        var verificationLink = $"{frontendUrl}/verify-email?token={token}";

        var subject = "Verify Your SpeakHalo Account";
        var body = $@"
            <h2>Welcome to SpeakHalo, {System.Net.WebUtility.HtmlEncode(name)}!</h2>
            <p>Thank you for registering. Please verify your email address by clicking the link below:</p>
            <p><a href='{verificationLink}' style='padding: 10px 20px; background-color: #4CAF50; color: white; text-decoration: none; border-radius: 5px;'>Verify Email</a></p>
            <p>Or copy and paste this link in your browser:</p>
            <p>{verificationLink}</p>
            <p>This link will expire in 24 hours.</p>
            <p>If you didn't create an account, please ignore this email.</p>
            <br>
            <p>Best regards,<br>The SpeakHalo Team</p>
        ";

        try
        {
            await _emailService.SendEmailAsync(email, subject, body, cancellationToken: cancellationToken);
            _logger.LogInformation("Verification email sent to {Email}", email);
            return Result.Success("Verification email sent successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send verification email to {Email}", email);
            return Result.Failure("Failed to send verification email. Please try again.");
        }
    }

    public async Task<Result> VerifyEmailAsync(string token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return Result.Failure("Verification token is required");
        }

        var hashedToken = HashToken(token);
        var userAuths = await _unitOfWork.Repository<UserAuth>()
            .FindAsync(ua => ua.EmailVerificationToken == hashedToken, cancellationToken);

        var userAuth = userAuths.FirstOrDefault();

        if (userAuth == null)
        {
            return Result.Failure("Invalid verification token");
        }

        if (userAuth.EmailVerified)
        {
            return Result.Success("Email already verified");
        }

        if (userAuth.EmailVerificationTokenExpiresAt.HasValue &&
            userAuth.EmailVerificationTokenExpiresAt.Value < DateTime.UtcNow)
        {
            return Result.Failure("Verification token has expired. Please request a new one.");
        }

        userAuth.EmailVerified = true;
        userAuth.EmailVerifiedAt = DateTime.UtcNow;
        userAuth.EmailVerificationToken = null;
        userAuth.EmailVerificationTokenExpiresAt = null;

        _unitOfWork.Repository<UserAuth>().Update(userAuth);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Email verified for user {UserId}", userAuth.UserId);

        return Result.Success("Email verified successfully! You can now log in.");
    }

    public async Task<Result> ResendVerificationEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByEmailAsync(email, cancellationToken);

        if (user == null)
        {
            return Result.Failure("User not found");
        }

        var userAuths = await _unitOfWork.Repository<UserAuth>()
            .FindAsync(ua => ua.UserId == user.Id, cancellationToken);

        var userAuth = userAuths.FirstOrDefault();

        if (userAuth == null)
        {
            return Result.Failure("User authentication record not found");
        }

        if (userAuth.EmailVerified)
        {
            return Result.Failure("Email already verified");
        }

        return await SendVerificationEmailAsync(user.Id, email, user.Name, cancellationToken);
    }

    public async Task<bool> IsEmailVerifiedAsync(string userId, CancellationToken cancellationToken = default)
    {
        var userAuths = await _unitOfWork.Repository<UserAuth>()
            .FindAsync(ua => ua.UserId == userId, cancellationToken);

        var userAuth = userAuths.FirstOrDefault();

        return userAuth?.EmailVerified ?? false;
    }

    private static string HashToken(string token)
    {
        var hashBytes = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
