using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpeakHalo.Application.DTOs.Payment;
using SpeakHalo.Application.Interfaces;
using SpeakHalo.Domain.Entities;
using SpeakHalo.Domain.Interfaces.Repositories;
using System.Security.Claims;
using System.Text.Json;

namespace SpeakHalo.API.Controllers;

/// <summary>
/// Endpoints for subscription management, billing info, Paddle checkout, and public plans.
/// </summary>
[Authorize]
[ApiController]
[Route("api/v1/subscriptions")]
public class SubscriptionController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPaddleService _paddleService;
    private readonly ITrialInviteService _trialInviteService;
    private readonly IAuthService _authService;
    private readonly IAdminNotificationService _adminNotifications;
    private readonly IEmailService _emailService;
    private readonly ILogger<SubscriptionController> _logger;

    public SubscriptionController(
        IUnitOfWork unitOfWork,
        IPaddleService paddleService,
        ITrialInviteService trialInviteService,
        IAuthService authService,
        IAdminNotificationService adminNotifications,
        IEmailService emailService,
        ILogger<SubscriptionController> logger)
    {
        _unitOfWork = unitOfWork;
        _paddleService = paddleService;
        _trialInviteService = trialInviteService;
        _authService = authService;
        _adminNotifications = adminNotifications;
        _emailService = emailService;
        _logger = logger;
    }

    /// <summary>
    /// Returns the current user's subscription including enabled features, limits, and usage.
    /// </summary>
    [HttpGet("current")]
    public async Task<IActionResult> GetCurrentSubscription(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var subscriptions = await _unitOfWork.Repository<UserSubscription>()
            .FindAsync(s => s.UserId == userId, cancellationToken);
        var sub = subscriptions.FirstOrDefault();

        if (sub == null)
            return NotFound(new { error = "No subscription found" });

        Dictionary<string, bool>? enabledFeatures = null;
        if (!string.IsNullOrWhiteSpace(sub.EnabledFeatures))
        {
            try
            {
                enabledFeatures = JsonSerializer.Deserialize<Dictionary<string, bool>>(sub.EnabledFeatures);
            }
            catch { /* malformed JSON, return null */ }
        }

        return Ok(new
        {
            plan = sub.Plan,
            pricingPlanId = sub.PricingPlanId,
            enabledFeatures,
            hasActivePayment = sub.HasActivePayment,
            isTemporarilySuspended = sub.IsTemporarilySuspended,
            isOnTrial = sub.IsOnTrial,
            trialEndsAt = sub.TrialEndsAt,
            limits = new
            {
                aiMinutesIncluded = sub.AIMinutesIncluded,
                whatsAppMessagesIncluded = sub.WhatsAppMessagesIncluded,
                smsMessagesIncluded = sub.SMSMessagesIncluded,
                imagesIncluded = sub.ImagesIncluded,
                enforceHardLimits = sub.EnforceHardLimits,
            },
            usage = new
            {
                aiMinutesUsed = sub.AIMinutesUsedThisMonth,
                whatsAppMessagesUsed = sub.WhatsAppMessagesUsedThisMonth,
                smsMessagesUsed = sub.SMSMessagesUsedThisMonth,
                imagesUsed = sub.ImagesUsedThisMonth,
            },
            billingCycleStartDate = sub.BillingCycleStartDate,
            nextBillingDate = sub.NextBillingDate,
            paddleSubscriptionStatus = sub.PaddleSubscriptionStatus,
        });
    }

    /// <summary>
    /// Create a Paddle checkout transaction for an authenticated user.
    /// </summary>
    [HttpPost("checkout")]
    public async Task<IActionResult> CreateCheckout(
        [FromBody] CreateCheckoutRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var plan = await _unitOfWork.Repository<PricingPlan>()
            .GetByIdAsync(request.PricingPlanId, cancellationToken);
        if (plan == null || plan.IsDeleted || !plan.IsActive || string.IsNullOrEmpty(plan.PaddlePriceId))
            return BadRequest(new { error = "Invalid pricing plan" });

        var subs = await _unitOfWork.Repository<UserSubscription>()
            .FindAsync(s => s.UserId == userId, cancellationToken);
        var sub = subs.FirstOrDefault();
        if (sub == null)
            return NotFound(new { error = "No subscription found" });

        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
        if (user == null)
            return NotFound(new { error = "User not found" });

        if (string.IsNullOrEmpty(sub.PaddleCustomerId))
        {
            var customerResult = await _paddleService.CreateCustomerAsync(user.Email, user.Name, cancellationToken);
            if (!customerResult.IsSuccess || customerResult.Data == null)
                return StatusCode(502, new { error = "Failed to create payment customer" });

            sub.PaddleCustomerId = customerResult.Data.Id;
            _unitOfWork.Repository<UserSubscription>().Update(sub);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        var txResult = await _paddleService.CreateCheckoutTransactionAsync(
            plan.PaddlePriceId, sub.PaddleCustomerId, null, cancellationToken);

        if (!txResult.IsSuccess || txResult.Data == null)
            return StatusCode(502, new { error = "Failed to create checkout" });

        return Ok(new CreateCheckoutResponse
        {
            TransactionId = txResult.Data.Id,
            CheckoutUrl = txResult.Data.Checkout?.Url
        });
    }

    /// <summary>
    /// Public registration: create a pending user account for admin review.
    /// User is created with IsActive=false. Admin activates via admin panel.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("checkout-public")]
    public async Task<IActionResult> CreatePublicCheckout(
        [FromBody] PublicCheckoutRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password) ||
            string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.BusinessName))
            return BadRequest(new { error = "All fields are required" });

        if (string.IsNullOrWhiteSpace(request.Country))
            return BadRequest(new { error = "Country is required" });

        if (string.IsNullOrWhiteSpace(request.ForwardingNumber))
            return BadRequest(new { error = "Forwarding number is required" });

        var plan = await _unitOfWork.Repository<PricingPlan>()
            .GetByIdAsync(request.PricingPlanId, cancellationToken);
        if (plan == null || plan.IsDeleted || !plan.IsActive)
            return BadRequest(new { error = "Invalid pricing plan" });

        var existing = await _unitOfWork.Repository<User>()
            .FindAsync(u => u.Email == request.Email.Trim().ToLowerInvariant(), cancellationToken);
        if (existing.Any())
            return BadRequest(new { error = "An account with this email already exists" });

        // Create user as INACTIVE (pending admin approval)
        var user = new User
        {
            Name = request.Name.Trim(),
            Email = request.Email.Trim().ToLowerInvariant(),
            HashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password, workFactor: 12),
            Role = Domain.Enums.UserRole.Owner,
            BusinessName = request.BusinessName.Trim(),
            Industry = Domain.Enums.ServiceType.Cleaning,
            IsActive = false,
            PasswordMustChange = false,
            TemporaryPassword = false
        };
        await _unitOfWork.Repository<User>().AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var userAuth = new UserAuth
        {
            UserId = user.Id,
            HashedPassword = user.HashedPassword,
            EmailVerified = false,
            PasswordMustChange = false,
            TemporaryPassword = false
        };
        await _unitOfWork.Repository<UserAuth>().AddAsync(userAuth, cancellationToken);

        var userProfile = new UserProfile
        {
            UserId = user.Id,
            BusinessName = request.BusinessName.Trim(),
            Industry = Domain.Enums.ServiceType.Cleaning,
            Country = request.Country.Trim().ToUpperInvariant()
        };
        await _unitOfWork.Repository<UserProfile>().AddAsync(userProfile, cancellationToken);

        var subscription = new UserSubscription
        {
            UserId = user.Id,
            Plan = plan.TierSlug ?? plan.Name.ToLower(),
            PricingPlanId = plan.Id,
            RequiresPayment = true,
            HasActivePayment = false,
            SelectedPlanAtRegistration = plan.Name,
            AIMinutesIncluded = plan.AIMinutesIncluded,
            SMSMessagesIncluded = plan.SMSMessagesIncluded,
            WhatsAppMessagesIncluded = plan.WhatsAppMessagesIncluded,
            ImagesIncluded = plan.ImagesIncluded,
            MaxPhoneNumbers = plan.MaxPhoneNumbers,
            AIOverageRate = plan.AIOverageRate,
            SMSOverageRate = plan.SMSOverageRate,
            WhatsAppOverageRate = plan.WhatsAppOverageRate,
            ImageOverageRate = plan.ImageOverageRate,
            EnabledFeatures = plan.EnabledFeatures,
            EnforceHardLimits = plan.EnforceHardLimits,
            BillingCycleStartDate = DateTime.UtcNow,
            NextBillingDate = DateTime.UtcNow.AddMonths(1)
        };
        await _unitOfWork.Repository<UserSubscription>().AddAsync(subscription, cancellationToken);

        var userTrial = new UserTrial { UserId = user.Id, IsTrial = false };
        await _unitOfWork.Repository<UserTrial>().AddAsync(userTrial, cancellationToken);

        var voiceAgent = new VoiceAgent
        {
            UserId = user.Id,
            Name = "AI Receptionist",
            VoiceName = "Kore",
            Language = "en-US",
            SystemPrompt = $"You are an AI receptionist for {request.BusinessName.Trim()}. Help callers with scheduling, quotes, and general questions. Be friendly and professional.",
            MaxCallDurationMinutes = 30,
            IsActive = true
        };
        await _unitOfWork.Repository<VoiceAgent>().AddAsync(voiceAgent, cancellationToken);

        var agentProfile = new AgentProfile
        {
            UserId = user.Id,
            GreetingMessage = $"Hi, thanks for calling {request.BusinessName.Trim()}. How can I help you today?",
            ClosingMessage = "Thank you for calling! Have a great day.",
            Language = "en",
            Tone = "professional",
            TransferOnComplexIssues = true,
            EnableTroubleshooting = true,
            HumanBackupNumber = request.ForwardingNumber.Trim()
        };
        await _unitOfWork.Repository<AgentProfile>().AddAsync(agentProfile, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Pending user {UserId} ({Email}) registered for plan {Plan}, awaiting admin approval",
            user.Id, user.Email, plan.Name);

        // Notify admin of new registration via SignalR
        _ = _adminNotifications.NotifyNewUserSignupAsync(user.Id, user.Email, request.Name);

        // Send confirmation email to admin
        var adminEmailHtml = $@"
            <div style='font-family: sans-serif; max-width: 600px; margin: 0 auto;'>
                <h2 style='color: #1e293b;'>New Registration Pending Approval</h2>
                <table style='width: 100%; border-collapse: collapse;'>
                    <tr><td style='padding: 8px; font-weight: bold;'>Name:</td><td style='padding: 8px;'>{user.Name}</td></tr>
                    <tr><td style='padding: 8px; font-weight: bold;'>Email:</td><td style='padding: 8px;'>{user.Email}</td></tr>
                    <tr><td style='padding: 8px; font-weight: bold;'>Business:</td><td style='padding: 8px;'>{request.BusinessName.Trim()}</td></tr>
                    <tr><td style='padding: 8px; font-weight: bold;'>Country:</td><td style='padding: 8px;'>{request.Country.Trim().ToUpperInvariant()}</td></tr>
                    <tr><td style='padding: 8px; font-weight: bold;'>Forwarding #:</td><td style='padding: 8px;'>{request.ForwardingNumber.Trim()}</td></tr>
                    <tr><td style='padding: 8px; font-weight: bold;'>Selected Plan:</td><td style='padding: 8px;'>{plan.Name} (${plan.DisplayPrice}/mo)</td></tr>
                </table>
                <p style='margin-top: 16px;'>
                    <a href='https://speakhalo.com/admin/users/{user.Id}' style='background: #2563eb; color: white; padding: 10px 20px; text-decoration: none; border-radius: 6px;'>Review in Admin Panel</a>
                </p>
            </div>";
        _ = _emailService.SendEmailAsync("support@speakhalo.com", $"New Registration: {user.Name} - {plan.Name}", adminEmailHtml);

        // Send confirmation email to user
        var userEmailHtml = $@"
            <div style='font-family: sans-serif; max-width: 600px; margin: 0 auto;'>
                <h2 style='color: #1e293b;'>Thanks for Signing Up!</h2>
                <p>Hi {user.Name},</p>
                <p>We've received your registration for the <strong>{plan.Name}</strong> plan. Our team is reviewing your application and you'll hear back from us shortly.</p>
                <p>Once approved, you'll receive an email with instructions to sign in and get started.</p>
                <p style='color: #64748b; font-size: 14px; margin-top: 24px;'>If you have any questions, email us at support@speakhalo.com.</p>
                <p style='color: #64748b; font-size: 14px;'>The SpeakHalo Team</p>
            </div>";
        _ = _emailService.SendEmailAsync(user.Email, "SpeakHalo - Registration Received", userEmailHtml);

        return Ok(new
        {
            message = "Registration submitted successfully. Our team will review your application and you'll receive an email once approved.",
            status = "pending_review"
        });
    }

    /// <summary>
    /// Get full billing info for the current user.
    /// </summary>
    [HttpGet("billing")]
    public async Task<IActionResult> GetBillingInfo(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var subs = await _unitOfWork.Repository<UserSubscription>()
            .FindAsync(s => s.UserId == userId, cancellationToken);
        var sub = subs.FirstOrDefault();
        if (sub == null)
            return NotFound(new { error = "No subscription found" });

        string? planName = null;
        decimal? planPrice = null;
        string? tierSlug = null;
        if (!string.IsNullOrEmpty(sub.PricingPlanId))
        {
            var plan = await _unitOfWork.Repository<PricingPlan>()
                .GetByIdAsync(sub.PricingPlanId, cancellationToken);
            if (plan != null)
            {
                planName = plan.Name;
                planPrice = plan.DisplayPrice;
                tierSlug = plan.TierSlug;
            }
        }

        string status;
        if (sub.RefundedAt.HasValue)
            status = "Refunded";
        else if (sub.PaddleSubscriptionStatus == "canceled" || sub.PaddleCanceledAt.HasValue)
            status = "Canceled";
        else if (sub.PaddleSubscriptionStatus == "past_due")
            status = "Past Due";
        else if (sub.IsOnTrial)
            status = "Trial";
        else if (sub.HasActivePayment)
            status = "Active";
        else
            status = "Inactive";

        var refundEligible = sub.FirstPaymentAt.HasValue
            && !sub.RefundedAt.HasValue
            && (DateTime.UtcNow - sub.FirstPaymentAt.Value).TotalDays <= 14;

        int? trialDaysRemaining = null;
        if (sub.IsOnTrial && sub.TrialEndsAt.HasValue)
            trialDaysRemaining = Math.Max(0, (int)Math.Ceiling((sub.TrialEndsAt.Value - DateTime.UtcNow).TotalDays));

        return Ok(new BillingInfoResponse
        {
            PlanName = planName,
            PlanPrice = planPrice,
            PlanTierSlug = tierSlug,
            Status = status,
            IsOnTrial = sub.IsOnTrial,
            TrialEndsAt = sub.TrialEndsAt,
            TrialDaysRemaining = trialDaysRemaining,
            NextBillingDate = sub.PaddleCurrentPeriodEnd ?? sub.NextBillingDate,
            FirstPaymentAt = sub.FirstPaymentAt,
            RefundEligible = refundEligible,
            PaddleSubscriptionStatus = sub.PaddleSubscriptionStatus,
            UpdatePaymentMethodUrl = sub.PaddleUpdatePaymentMethodUrl,
            CancelUrl = sub.PaddleCancelUrl,
            CanceledAt = sub.PaddleCanceledAt,
            RefundedAt = sub.RefundedAt
        });
    }

    /// <summary>
    /// Cancel subscription via Paddle API.
    /// </summary>
    [HttpPost("cancel")]
    public async Task<IActionResult> CancelSubscription(
        [FromBody] CancelSubscriptionRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var subs = await _unitOfWork.Repository<UserSubscription>()
            .FindAsync(s => s.UserId == userId, cancellationToken);
        var sub = subs.FirstOrDefault();
        if (sub == null || string.IsNullOrEmpty(sub.PaddleSubscriptionId))
            return BadRequest(new { error = "No active Paddle subscription found" });

        var result = await _paddleService.CancelSubscriptionAsync(
            sub.PaddleSubscriptionId, request.EffectiveImmediately, cancellationToken);

        if (!result.IsSuccess)
            return StatusCode(502, new { error = "Failed to cancel subscription" });

        return Ok(new { message = "Subscription cancellation initiated" });
    }

    /// <summary>
    /// List active plans with Paddle price IDs for public display.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("plans")]
    public async Task<IActionResult> ListPlans(CancellationToken cancellationToken)
    {
        var plans = await _unitOfWork.Repository<PricingPlan>()
            .FindAsync(p => p.IsActive && !p.IsDeleted, cancellationToken);

        var result = plans.OrderBy(p => p.SortOrder).ThenBy(p => p.DisplayPrice).Select(p =>
        {
            object? enabledFeatures = null;
            if (!string.IsNullOrEmpty(p.EnabledFeatures))
            {
                try { enabledFeatures = JsonSerializer.Deserialize<JsonElement>(p.EnabledFeatures); }
                catch { enabledFeatures = null; }
            }

            return new PublicPlanResponse
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                DisplayPrice = p.DisplayPrice,
                TierSlug = p.TierSlug,
                PaddlePriceId = p.PaddlePriceId,
                AIMinutesIncluded = p.AIMinutesIncluded,
                SMSMessagesIncluded = p.SMSMessagesIncluded,
                WhatsAppMessagesIncluded = p.WhatsAppMessagesIncluded,
                ImagesIncluded = p.ImagesIncluded,
                MaxPhoneNumbers = p.MaxPhoneNumbers,
                AIOverageRate = p.AIOverageRate,
                SMSOverageRate = p.SMSOverageRate,
                WhatsAppOverageRate = p.WhatsAppOverageRate,
                ImageOverageRate = p.ImageOverageRate,
                EnabledFeatures = enabledFeatures,
                SortOrder = p.SortOrder
            };
        }).ToList();

        return Ok(result);
    }

    /// <summary>
    /// Validate a trial invite token (public endpoint).
    /// </summary>
    [AllowAnonymous]
    [HttpGet("trial-invite/validate")]
    public async Task<IActionResult> ValidateTrialInvite(
        [FromQuery] string token,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token))
            return BadRequest(new { error = "Token is required" });

        var result = await _trialInviteService.ValidateTokenAsync(token, cancellationToken);
        if (!result.IsSuccess)
            return BadRequest(result);

        Response.Headers["Referrer-Policy"] = "no-referrer";
        return Ok(result.Data);
    }

    /// <summary>
    /// Request a refund (user-initiated, within 30-day window).
    /// </summary>
    [HttpPost("refund")]
    public async Task<IActionResult> RequestRefund(
        [FromBody] RefundRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var subs = await _unitOfWork.Repository<UserSubscription>()
            .FindAsync(s => s.UserId == userId, cancellationToken);
        var sub = subs.FirstOrDefault();
        if (sub == null)
            return NotFound(new { error = "No subscription found" });

        if (!sub.FirstPaymentAt.HasValue)
            return BadRequest(new { error = "No payment found to refund" });

        if (sub.RefundedAt.HasValue)
            return BadRequest(new { error = "Already refunded" });

        if ((DateTime.UtcNow - sub.FirstPaymentAt.Value).TotalDays > 30)
            return BadRequest(new { error = "Refund window (30 days) has expired" });

        sub.RefundedAt = DateTime.UtcNow;
        sub.RefundReason = request.Reason;
        _unitOfWork.Repository<UserSubscription>().Update(sub);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User {UserId} requested refund: {Reason}", userId, request.Reason);

        return Ok(new { message = "Refund request submitted. Our team will process it shortly." });
    }
}
