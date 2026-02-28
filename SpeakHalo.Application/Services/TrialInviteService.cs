using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using SpeakHalo.Application.DTOs.Payment;
using SpeakHalo.Application.Interfaces;
using SpeakHalo.Domain.Entities;
using SpeakHalo.Domain.Interfaces.Repositories;
using SpeakHalo.Shared.Models;

namespace SpeakHalo.Application.Services;

public class TrialInviteService : ITrialInviteService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailService _emailService;
    private readonly ILogger<TrialInviteService> _logger;

    public TrialInviteService(IUnitOfWork unitOfWork, IEmailService emailService, ILogger<TrialInviteService> logger)
    {
        _unitOfWork = unitOfWork;
        _emailService = emailService;
        _logger = logger;
    }

    private static string ShortTokenHash(string token) =>
        Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(token)))[..8];

    public async Task<Result<TrialInviteCreatedResponse>> CreateInviteAsync(
        CreateTrialInviteRequest request, string adminId, string frontendBaseUrl, CancellationToken cancellationToken = default)
    {
        // Validate pricing plan exists
        var plan = await _unitOfWork.Repository<PricingPlan>()
            .GetByIdAsync(request.PricingPlanId, cancellationToken);
        if (plan == null || plan.IsDeleted || !plan.IsActive)
            return Result<TrialInviteCreatedResponse>.Failure("Pricing plan not found or inactive");

        if (request.TrialDays < 1 || request.TrialDays > 90)
            return Result<TrialInviteCreatedResponse>.Failure("Trial days must be between 1 and 90");

        // Generate crypto-random token
        var tokenBytes = RandomNumberGenerator.GetBytes(32);
        var token = Convert.ToHexString(tokenBytes).ToLowerInvariant();

        var invite = new TrialInvite
        {
            Token = token,
            Email = request.Email?.Trim().ToLowerInvariant(),
            PricingPlanId = request.PricingPlanId,
            TrialDays = request.TrialDays,
            ExpiresAt = DateTime.UtcNow.AddDays(30), // Invite valid for 30 days
            CreatedByAdminId = adminId,
            Notes = request.Notes?.Trim()
        };

        await _unitOfWork.Repository<TrialInvite>().AddAsync(invite, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var inviteUrl = $"{frontendBaseUrl.TrimEnd('/')}/signup?trial={token}";

        _logger.LogInformation("Admin {AdminId} created trial invite {InviteId} for plan {PlanId}",
            adminId, invite.Id, request.PricingPlanId);

        if (!string.IsNullOrEmpty(invite.Email))
        {
            var emailResult = await _emailService.SendEmailAsync(
                invite.Email,
                $"You're invited to try SpeakHalo free for {invite.TrialDays} days",
                BuildInviteEmailHtml(invite, inviteUrl, plan.Name),
                cancellationToken: cancellationToken);

            if (!emailResult.IsSuccess)
            {
                _logger.LogWarning("Failed to send invite email to {Email}: {Errors}",
                    invite.Email, string.Join(", ", emailResult.Errors));
            }
        }

        return Result<TrialInviteCreatedResponse>.Success(new TrialInviteCreatedResponse
        {
            Id = invite.Id,
            Token = token,
            InviteUrl = inviteUrl
        });
    }

    public async Task<Result<TrialInviteValidationResponse>> ValidateTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        var invites = await _unitOfWork.Repository<TrialInvite>()
            .FindAsync(i => i.Token == token && !i.IsDeleted, cancellationToken);
        var invite = invites.FirstOrDefault();

        if (invite == null)
        {
            _logger.LogWarning("Trial invite validation failed: {Reason} for token hash {TokenHash}",
                "Token not found", ShortTokenHash(token));
            return Result<TrialInviteValidationResponse>.Success(new TrialInviteValidationResponse
            {
                IsValid = false,
                Error = "Invalid invite token"
            });
        }

        if (invite.IsRedeemed)
        {
            _logger.LogWarning("Trial invite validation failed: {Reason} for token hash {TokenHash}",
                "Already redeemed", ShortTokenHash(token));
            return Result<TrialInviteValidationResponse>.Success(new TrialInviteValidationResponse
            {
                IsValid = false,
                Error = "This invite has already been used"
            });
        }

        if (invite.ExpiresAt < DateTime.UtcNow)
        {
            _logger.LogWarning("Trial invite validation failed: {Reason} for token hash {TokenHash}",
                "Expired", ShortTokenHash(token));
            return Result<TrialInviteValidationResponse>.Success(new TrialInviteValidationResponse
            {
                IsValid = false,
                Error = "This invite has expired"
            });
        }

        // Get plan info
        var plan = await _unitOfWork.Repository<PricingPlan>()
            .GetByIdAsync(invite.PricingPlanId, cancellationToken);

        return Result<TrialInviteValidationResponse>.Success(new TrialInviteValidationResponse
        {
            IsValid = true,
            PricingPlanName = plan?.Name,
            PlanPrice = plan?.DisplayPrice,
            TrialDays = invite.TrialDays,
            Email = invite.Email
        });
    }

    public async Task<Result> RedeemTrialAsync(string token, string userId, CancellationToken cancellationToken = default)
    {
        var invites = await _unitOfWork.Repository<TrialInvite>()
            .FindAsync(i => i.Token == token && !i.IsDeleted, cancellationToken);
        var invite = invites.FirstOrDefault();

        if (invite == null)
        {
            _logger.LogWarning("Trial invite redemption failed: {Reason} for token hash {TokenHash} by user {UserId}",
                "Token not found", ShortTokenHash(token), userId);
            return Result.Failure("Invalid invite token");
        }
        if (invite.IsRedeemed)
        {
            _logger.LogWarning("Trial invite redemption failed: {Reason} for token hash {TokenHash} by user {UserId}",
                "Already redeemed", ShortTokenHash(token), userId);
            return Result.Failure("This invite has already been used");
        }
        if (invite.ExpiresAt < DateTime.UtcNow)
        {
            _logger.LogWarning("Trial invite redemption failed: {Reason} for token hash {TokenHash} by user {UserId}",
                "Expired", ShortTokenHash(token), userId);
            return Result.Failure("This invite has expired");
        }

        // Mark invite as redeemed
        invite.IsRedeemed = true;
        invite.RedeemedAt = DateTime.UtcNow;
        invite.RedeemedByUserId = userId;

        // Get the pricing plan
        var plan = await _unitOfWork.Repository<PricingPlan>()
            .GetByIdAsync(invite.PricingPlanId, cancellationToken);
        if (plan == null)
            return Result.Failure("Pricing plan no longer exists");

        // Set up user subscription in trial mode
        var subs = await _unitOfWork.Repository<UserSubscription>()
            .FindAsync(s => s.UserId == userId, cancellationToken);
        var sub = subs.FirstOrDefault();

        if (sub == null)
            return Result.Failure("User subscription not found");

        sub.PricingPlanId = plan.Id;
        sub.Plan = plan.TierSlug ?? plan.Name.ToLower();
        sub.IsOnTrial = true;
        sub.TrialEndsAt = DateTime.UtcNow.AddDays(invite.TrialDays);
        sub.HasActivePayment = true; // Trial users get access
        sub.RequiresPayment = false;
        sub.AIMinutesIncluded = plan.AIMinutesIncluded;
        sub.SMSMessagesIncluded = plan.SMSMessagesIncluded;
        sub.WhatsAppMessagesIncluded = plan.WhatsAppMessagesIncluded;
        sub.ImagesIncluded = plan.ImagesIncluded;
        sub.MaxPhoneNumbers = plan.MaxPhoneNumbers;
        sub.AIOverageRate = plan.AIOverageRate;
        sub.SMSOverageRate = plan.SMSOverageRate;
        sub.WhatsAppOverageRate = plan.WhatsAppOverageRate;
        sub.ImageOverageRate = plan.ImageOverageRate;
        sub.EnabledFeatures = plan.EnabledFeatures;
        sub.EnforceHardLimits = plan.EnforceHardLimits;
        sub.BillingCycleStartDate = DateTime.UtcNow;
        sub.NextBillingDate = DateTime.UtcNow.AddDays(invite.TrialDays);

        // Update UserTrial record
        var trials = await _unitOfWork.Repository<UserTrial>()
            .FindAsync(t => t.UserId == userId, cancellationToken);
        var trial = trials.FirstOrDefault();
        if (trial != null)
        {
            trial.IsTrial = true;
            trial.TrialEndsAt = sub.TrialEndsAt;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Trial invite {InviteId} redeemed by user {UserId}", invite.Id, userId);

        return Result.Success("Trial activated successfully");
    }

    public async Task<Result<List<TrialInviteResponse>>> ListInvitesAsync(string frontendBaseUrl, CancellationToken cancellationToken = default)
    {
        var invites = await _unitOfWork.Repository<TrialInvite>()
            .FindAsync(i => !i.IsDeleted, cancellationToken);

        // Load all referenced plans in one query
        var planIds = invites.Select(i => i.PricingPlanId).Distinct().ToList();
        var plans = await _unitOfWork.Repository<PricingPlan>()
            .FindAsync(p => planIds.Contains(p.Id), cancellationToken);
        var planLookup = plans.ToDictionary(p => p.Id, p => p.Name);

        var result = invites.OrderByDescending(i => i.CreatedAt).Select(i => new TrialInviteResponse
        {
            Id = i.Id,
            Token = i.Token,
            Email = i.Email,
            PricingPlanId = i.PricingPlanId,
            PricingPlanName = planLookup.GetValueOrDefault(i.PricingPlanId),
            TrialDays = i.TrialDays,
            ExpiresAt = i.ExpiresAt,
            IsRedeemed = i.IsRedeemed,
            RedeemedAt = i.RedeemedAt,
            RedeemedByUserId = i.RedeemedByUserId,
            Notes = i.Notes,
            InviteUrl = $"{frontendBaseUrl.TrimEnd('/')}/signup?trial={i.Token}",
            CreatedAt = i.CreatedAt
        }).ToList();

        return Result<List<TrialInviteResponse>>.Success(result);
    }

    public async Task<Result> RevokeInviteAsync(string inviteId, string adminId, CancellationToken cancellationToken = default)
    {
        var invite = await _unitOfWork.Repository<TrialInvite>()
            .GetByIdAsync(inviteId, cancellationToken);

        if (invite == null || invite.IsDeleted)
            return Result.Failure("Invite not found");

        if (invite.IsRedeemed)
            return Result.Failure("Cannot revoke an already redeemed invite");

        invite.IsDeleted = true;
        invite.DeletedAt = DateTime.UtcNow;
        invite.DeletedBy = adminId;
        _unitOfWork.Repository<TrialInvite>().Update(invite);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Admin {AdminId} revoked trial invite {InviteId}", adminId, inviteId);

        return Result.Success("Invite revoked successfully");
    }

    private static string BuildInviteEmailHtml(TrialInvite invite, string inviteUrl, string planName)
    {
        var expiryNote = invite.ExpiresAt.ToString("MMMM d, yyyy");
        return $"""
            <html>
            <body style="font-family: Arial, sans-serif; line-height: 1.6; color: #333; background: #f5f5f5;">
                <div style="max-width: 600px; margin: 0 auto; padding: 20px;">
                    <div style="background: white; border-radius: 12px; padding: 40px; box-shadow: 0 2px 8px rgba(0,0,0,0.08);">
                        <h2 style="color: #1a73e8; margin-top: 0;">You're invited to try SpeakHalo free for {invite.TrialDays} days</h2>
                        <p>You've been given exclusive access to SpeakHalo, an AI-powered voice assistant for home service businesses.</p>
                        <table style="width: 100%; border-collapse: collapse; margin: 24px 0; border-radius: 8px; overflow: hidden;">
                            <tr style="background: #f0f7ff;">
                                <td style="padding: 12px 16px; font-size: 13px; color: #555; font-weight: 600;">Plan</td>
                                <td style="padding: 12px 16px; font-size: 14px; font-weight: 700; color: #1a1a1a;">{planName}</td>
                            </tr>
                            <tr style="background: #f7f7f7;">
                                <td style="padding: 12px 16px; font-size: 13px; color: #555; font-weight: 600;">Trial Duration</td>
                                <td style="padding: 12px 16px; font-size: 14px; font-weight: 700; color: #1a1a1a;">{invite.TrialDays} days</td>
                            </tr>
                            <tr style="background: #f0f7ff;">
                                <td style="padding: 12px 16px; font-size: 13px; color: #555; font-weight: 600;">Link Expires</td>
                                <td style="padding: 12px 16px; font-size: 14px; font-weight: 700; color: #1a1a1a;">{expiryNote} (one-time use)</td>
                            </tr>
                        </table>
                        <p style="text-align: center; margin: 32px 0;">
                            <a href="{inviteUrl}" style="display: inline-block; background: #1a73e8; color: white; padding: 14px 32px; text-decoration: none; border-radius: 8px; font-weight: 700; font-size: 16px;">Start Your Free Trial</a>
                        </p>
                        <p style="font-size: 13px; color: #888; text-align: center;">
                            This link is for your use only and expires on {expiryNote}. It can only be redeemed once.
                        </p>
                    </div>
                </div>
            </body>
            </html>
            """;
    }
}
