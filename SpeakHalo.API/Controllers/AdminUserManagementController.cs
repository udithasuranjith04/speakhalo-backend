#pragma warning disable CS0618

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpeakHalo.Application.DTOs.Admin;
using SpeakHalo.Application.Interfaces;
using SpeakHalo.Domain.Entities;
using SpeakHalo.Domain.Enums;
using SpeakHalo.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Caching.Distributed;
using SpeakHalo.Shared.Constants;
using System.Security.Claims;
using System.Text.Json;

namespace SpeakHalo.API.Controllers;

/// <summary>
/// Admin endpoints for user management: create users, set limits, reset passwords,
/// configure AI agents, manage features, and full account control.
/// </summary>
[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/v1/admin/users")]
public class AdminUserManagementController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AdminUserManagementController> _logger;
    private readonly IEmailService _emailService;
    private readonly IAuditLogService _auditLogService;
    private readonly IAdminNotificationService _adminNotifications;
    private readonly IDistributedCache _cache;
    private readonly string _frontendBaseUrl;
    private readonly string _supportEmail;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private static readonly string[] ValidVoiceNames =
        { "Kore", "Puck", "Charon", "Fenrir", "Aoede", "Leda", "Orus", "Zephyr" };

    private static readonly string[] ValidPersonalityTones =
        { "professional", "friendly", "casual", "warm", "formal", "empathetic" };

    public AdminUserManagementController(
        IUnitOfWork unitOfWork,
        ILogger<AdminUserManagementController> logger,
        IEmailService emailService,
        IAuditLogService auditLogService,
        IAdminNotificationService adminNotifications,
        IDistributedCache cache,
        IConfiguration configuration)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _emailService = emailService;
        _auditLogService = auditLogService;
        _adminNotifications = adminNotifications;
        _cache = cache;
        _frontendBaseUrl = configuration["Frontend:BaseUrl"]?.TrimEnd('/') ?? "https://app.speakhalo.com";
        _supportEmail = configuration["SendGrid:SupportEmail"] ?? "support@speakhalo.com";
    }

    /// <summary>
    /// List all users with subscription and usage info. Supports search and pagination.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ListUsers(
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? role = null,
        [FromQuery] string? status = null,
        CancellationToken cancellationToken = default)
    {
        var users = await _unitOfWork.Repository<User>()
            .FindAsync(u => !u.IsDeleted, cancellationToken);

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(search))
        {
            var q = search.Trim().ToLower();
            users = users.Where(u =>
                (u.Email != null && u.Email.ToLower().Contains(q)) ||
                (u.Name != null && u.Name.ToLower().Contains(q))
            ).ToList();
        }

        // Apply role filter
        if (!string.IsNullOrWhiteSpace(role) && Enum.TryParse<UserRole>(role, true, out var roleEnum))
        {
            users = users.Where(u => u.Role == roleEnum).ToList();
        }

        var userIds = users.Select(u => u.Id).ToList();

        // Batch load all subscriptions and auth records upfront (avoids N+1)
        var allSubs = await _unitOfWork.Repository<UserSubscription>()
            .FindAsync(s => userIds.Contains(s.UserId), cancellationToken);
        var subsByUserId = allSubs.ToDictionary(s => s.UserId);

        var allAuths = await _unitOfWork.Repository<UserAuth>()
            .FindAsync(a => userIds.Contains(a.UserId), cancellationToken);
        var authsByUserId = allAuths.ToDictionary(a => a.UserId);

        var orderedUsers = users.OrderByDescending(u => u.CreatedAt).ToList();

        // Apply status filter (requires subscription data)
        if (!string.IsNullOrWhiteSpace(status))
        {
            orderedUsers = status.ToLower() switch
            {
                "active" => orderedUsers.Where(u => u.IsActive && (!subsByUserId.TryGetValue(u.Id, out var s) || !s.IsTemporarilySuspended)).ToList(),
                "suspended" => orderedUsers.Where(u => subsByUserId.TryGetValue(u.Id, out var s) && s.IsTemporarilySuspended).ToList(),
                "trial" => orderedUsers.Where(u => subsByUserId.TryGetValue(u.Id, out var s) && s.IsOnTrial).ToList(),
                "inactive" => orderedUsers.Where(u => !u.IsActive).ToList(),
                _ => orderedUsers
            };
        }

        var totalCount = orderedUsers.Count;

        // Paginate
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 50;
        if (pageSize > 200) pageSize = 200;

        var paginatedUsers = orderedUsers
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var items = paginatedUsers.Select(user =>
        {
            subsByUserId.TryGetValue(user.Id, out var sub);
            authsByUserId.TryGetValue(user.Id, out var auth);

            return new
            {
                user.Id,
                user.Email,
                user.Name,
                user.IsActive,
                user.Role,
                user.CreatedAt,
                passwordMustChange = auth?.PasswordMustChange ?? false,
                lastLogin = auth?.LastLogin,
                subscription = sub == null ? null : new
                {
                    sub.Plan,
                    sub.PricingPlanId,
                    sub.AIMinutesIncluded,
                    sub.AIMinutesUsedThisMonth,
                    sub.WhatsAppMessagesIncluded,
                    sub.WhatsAppMessagesUsedThisMonth,
                    sub.SMSMessagesIncluded,
                    sub.SMSMessagesUsedThisMonth,
                    sub.ImagesIncluded,
                    sub.ImagesUsedThisMonth,
                    sub.EnforceHardLimits,
                    sub.IsTemporarilySuspended,
                    sub.SuspendedReason,
                    sub.BillingCycleStartDate,
                    sub.HasActivePayment,
                    sub.IsOnTrial,
                    sub.TrialEndsAt
                }
            };
        }).ToList();

        return Ok(new
        {
            items,
            totalCount,
            page,
            pageSize,
            totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
        });
    }

    /// <summary>
    /// Get full user details including subscription, profile, agent config, voice agents, and all settings
    /// </summary>
    [HttpGet("{userId}/details")]
    public async Task<IActionResult> GetUserDetails(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
        if (user == null || user.IsDeleted)
            return NotFound(new { error = "User not found" });

        // Load all related entities sequentially (DbContext is not thread-safe)
        var sub = (await _unitOfWork.Repository<UserSubscription>()
            .FindAsync(s => s.UserId == userId, cancellationToken)).FirstOrDefault();
        var profile = (await _unitOfWork.Repository<UserProfile>()
            .FindAsync(p => p.UserId == userId, cancellationToken)).FirstOrDefault();
        var auth = (await _unitOfWork.Repository<UserAuth>()
            .FindAsync(a => a.UserId == userId, cancellationToken)).FirstOrDefault();
        var admin = (await _unitOfWork.Repository<UserAdmin>()
            .FindAsync(a => a.UserId == userId, cancellationToken)).FirstOrDefault();
        var voiceAgents = await _unitOfWork.Repository<VoiceAgent>()
            .FindAsync(v => v.UserId == userId && !v.IsDeleted, cancellationToken);
        var agentProfile = (await _unitOfWork.Repository<AgentProfile>()
            .FindAsync(a => a.UserId == userId && !a.IsDeleted, cancellationToken)).FirstOrDefault();
        var phoneNumbers = await _unitOfWork.PhoneNumbers
            .FindAsync(p => p.UserId == userId && !p.IsDeleted, cancellationToken);

        // Parse AI agent config if present
        object? aiAgentConfig = null;
        if (!string.IsNullOrEmpty(profile?.AiAgentConfig))
        {
            try
            {
                aiAgentConfig = JsonSerializer.Deserialize<JsonElement>(profile.AiAgentConfig);
            }
            catch
            {
                aiAgentConfig = profile.AiAgentConfig;
            }
        }

        // Parse enabled features if present
        object? enabledFeatures = null;
        if (!string.IsNullOrEmpty(sub?.EnabledFeatures))
        {
            try
            {
                enabledFeatures = JsonSerializer.Deserialize<JsonElement>(sub.EnabledFeatures);
            }
            catch
            {
                enabledFeatures = sub.EnabledFeatures;
            }
        }

        return Ok(new
        {
            user = new
            {
                user.Id,
                user.Email,
                user.Name,
                user.IsActive,
                user.Role,
                user.CreatedAt,
                user.UpdatedAt
            },
            auth = auth == null ? null : new
            {
                auth.PasswordMustChange,
                auth.EmailVerified,
                auth.LastLogin,
                auth.TotpEnabled,
                auth.FailedLoginAttempts,
                auth.LockoutEndTime
            },
            profile = profile == null ? null : new
            {
                profile.BusinessName,
                profile.BusinessPhone,
                profile.BusinessAddress,
                profile.BusinessWebsite,
                profile.Industry,
                profile.IndustrySpecialization,
                profile.OnboardingCompleted,
                profile.OnboardingStep,
                profile.GoogleCalendarConnected,
                profile.CallerRecognitionEnabled,
                aiAgentConfig,
                profile.ConfigUpdatedAt,
                profile.ConfigUpdatedByAdminId
            },
            subscription = sub == null ? null : new
            {
                sub.Plan,
                sub.PricingPlanId,
                enabledFeatures,
                sub.MaxPhoneNumbers,
                sub.HasActivePayment,
                sub.RequiresPayment,
                limits = new
                {
                    aiMinutes = sub.AIMinutesIncluded,
                    whatsAppMessages = sub.WhatsAppMessagesIncluded,
                    smsMessages = sub.SMSMessagesIncluded,
                    images = sub.ImagesIncluded,
                    enforceHardLimits = sub.EnforceHardLimits
                },
                usage = new
                {
                    aiMinutesUsed = sub.AIMinutesUsedThisMonth,
                    whatsAppMessagesUsed = sub.WhatsAppMessagesUsedThisMonth,
                    smsMessagesUsed = sub.SMSMessagesUsedThisMonth,
                    imagesUsed = sub.ImagesUsedThisMonth
                },
                sub.BillingCycleStartDate,
                sub.NextBillingDate,
                trial = new
                {
                    sub.IsOnTrial,
                    sub.TrialEndsAt
                },
                suspension = new
                {
                    sub.IsTemporarilySuspended,
                    sub.SuspendedReason,
                    sub.SuspendedAt,
                    sub.SuspendedByAdminId
                }
            },
            admin = admin == null ? null : new
            {
                admin.CreatedByAdminId,
                admin.AssignedSalesRepId,
                admin.AdminNotes,
                admin.AccountManagerNotes,
                admin.BlockReason,
                admin.BlockedAt,
                admin.BlockedByAdminId
            },
            voiceAgents = voiceAgents.Select(v => new
            {
                v.Id,
                v.Name,
                v.VoiceName,
                v.Language,
                v.SystemPrompt,
                v.MaxCallDurationMinutes,
                v.IsActive,
                v.LocationName,
                v.LocationAddress,
                v.LocationPhone,
                v.CreatedAt,
                v.UpdatedAt
            }),
            agentProfile = agentProfile == null ? null : new
            {
                agentProfile.Id,
                agentProfile.GreetingMessage,
                agentProfile.ClosingMessage,
                agentProfile.ServicesList,
                agentProfile.BusinessHours,
                agentProfile.VoicemailMessage,
                agentProfile.Language,
                agentProfile.Tone,
                agentProfile.HumanBackupNumber,
                agentProfile.TransferOnComplexIssues,
                agentProfile.CustomInstructions,
                agentProfile.BrandName,
                agentProfile.BrandTagline,
                agentProfile.BrandColor,
                agentProfile.LogoUrl,
                agentProfile.EnableTroubleshooting
            },
            phoneNumbers = phoneNumbers.Select(p => new
            {
                p.Id,
                p.Number,
                p.IsActive,
                p.CreatedAt
            })
        });
    }

    /// <summary>
    /// Create a new user with email, temp password, usage limits, and optional voice agent setup
    /// </summary>
    [HttpPost("create")]
    public async Task<IActionResult> CreateUser(
        [FromBody] AdminCreateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        var adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(adminId))
            return Unauthorized();

        // Validate email
        if (string.IsNullOrWhiteSpace(request.Email))
            return BadRequest(new { error = "Email is required" });

        // Check if user already exists
        var existing = await _unitOfWork.Repository<User>()
            .FindAsync(u => u.Email == request.Email, cancellationToken);
        if (existing.Any())
            return BadRequest(new { error = "User with this email already exists" });

        // Validate voice name if provided
        if (!string.IsNullOrEmpty(request.VoiceName) &&
            !ValidVoiceNames.Contains(request.VoiceName, StringComparer.OrdinalIgnoreCase))
            return BadRequest(new { error = $"Invalid voice name. Valid options: {string.Join(", ", ValidVoiceNames)}" });

        // Generate temp password
        var tempPassword = GenerateRandomPassword();

        // Create User
        var user = new User
        {
            Name = request.Name ?? request.Email,
            Email = request.Email,
            HashedPassword = BCrypt.Net.BCrypt.HashPassword(tempPassword, workFactor: 12),
            Role = UserRole.Owner,
            BusinessName = request.BusinessName ?? "",
            Industry = ServiceType.Cleaning,
            IsActive = true,
            PasswordMustChange = true,
            TemporaryPassword = true,
            CreatedAt = DateTime.UtcNow,
            CreatedByAdminId = adminId
        };

        await _unitOfWork.Repository<User>().AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Create UserAuth
        var userAuth = new UserAuth
        {
            UserId = user.Id,
            HashedPassword = user.HashedPassword,
            EmailVerified = true,
            PasswordMustChange = true,
            TemporaryPassword = true
        };
        await _unitOfWork.Repository<UserAuth>().AddAsync(userAuth, cancellationToken);

        // Create UserProfile with business contact info
        var userProfile = new UserProfile
        {
            UserId = user.Id,
            BusinessName = request.BusinessName ?? "",
            BusinessPhone = request.BusinessPhone,
            BusinessAddress = request.BusinessAddress,
            BusinessWebsite = request.BusinessWebsite,
            Industry = ServiceType.Cleaning
        };
        await _unitOfWork.Repository<UserProfile>().AddAsync(userProfile, cancellationToken);

        // Resolve limits from pricing plan or request defaults
        PricingPlan? pricingPlan = null;
        if (!string.IsNullOrEmpty(request.PricingPlanId))
        {
            pricingPlan = await _unitOfWork.Repository<PricingPlan>()
                .GetByIdAsync(request.PricingPlanId, cancellationToken);
            if (pricingPlan == null || pricingPlan.IsDeleted || !pricingPlan.IsActive)
                return BadRequest(new { error = "Pricing plan not found or inactive" });
        }

        // Plan provides defaults; explicit request values override plan values
        var aiMinutes = request.AIMinutes ?? pricingPlan?.AIMinutesIncluded ?? 100;
        var whatsAppMessages = request.WhatsAppMessages ?? pricingPlan?.WhatsAppMessagesIncluded ?? 500;
        var smsMessages = request.SMSMessages ?? pricingPlan?.SMSMessagesIncluded ?? 200;
        var images = request.Images ?? pricingPlan?.ImagesIncluded ?? 50;
        var maxPhoneNumbers = pricingPlan?.MaxPhoneNumbers ?? 1;
        var enforceHardLimits = pricingPlan?.EnforceHardLimits ?? true;
        var enabledFeatures = pricingPlan?.EnabledFeatures;

        // Trial handling
        var isOnTrial = pricingPlan != null && pricingPlan.TrialDays > 0;
        DateTime? trialEndsAt = isOnTrial ? DateTime.UtcNow.AddDays(pricingPlan!.TrialDays) : null;

        // Create UserSubscription with resolved limits
        var subscription = new UserSubscription
        {
            UserId = user.Id,
            Plan = "cleaning",
            RequiresPayment = false,
            HasActivePayment = true,
            PricingPlanId = pricingPlan?.Id,
            AIMinutesIncluded = aiMinutes,
            WhatsAppMessagesIncluded = whatsAppMessages,
            SMSMessagesIncluded = smsMessages,
            ImagesIncluded = images,
            MaxPhoneNumbers = maxPhoneNumbers,
            EnforceHardLimits = enforceHardLimits,
            EnabledFeatures = enabledFeatures,
            IsOnTrial = isOnTrial,
            TrialEndsAt = trialEndsAt,
            BillingCycleStartDate = DateTime.UtcNow,
            NextBillingDate = DateTime.UtcNow.AddMonths(1)
        };
        await _unitOfWork.Repository<UserSubscription>().AddAsync(subscription, cancellationToken);

        // Create UserTrial
        var userTrial = new UserTrial
        {
            UserId = user.Id,
            IsTrial = isOnTrial,
            TrialEndsAt = trialEndsAt
        };
        await _unitOfWork.Repository<UserTrial>().AddAsync(userTrial, cancellationToken);

        // Create UserAdmin
        var userAdmin = new UserAdmin
        {
            UserId = user.Id,
            CreatedByAdminId = adminId,
            AdminNotes = request.AdminNotes
        };
        await _unitOfWork.Repository<UserAdmin>().AddAsync(userAdmin, cancellationToken);

        // Create default VoiceAgent for the user
        var agentName = request.AgentName ?? "AI Receptionist";
        var voiceName = request.VoiceName ?? "Kore";
        var greeting = request.Greeting ?? $"Hi, thanks for calling {request.BusinessName ?? "us"}. How can I help you today?";

        var voiceAgent = new VoiceAgent
        {
            UserId = user.Id,
            Name = agentName,
            VoiceName = voiceName,
            Language = "en-US",
            SystemPrompt = $"You are {agentName}, a professional AI receptionist for a cleaning service business" +
                           (!string.IsNullOrEmpty(request.BusinessName) ? $" called {request.BusinessName}" : "") +
                           ". Help callers with scheduling, quotes, service inquiries, and general questions. " +
                           "Be friendly, professional, and helpful. If you cannot help with something, offer to transfer to a human.",
            MaxCallDurationMinutes = 30,
            IsActive = true
        };
        await _unitOfWork.Repository<VoiceAgent>().AddAsync(voiceAgent, cancellationToken);

        // Create default AgentProfile
        var agentProfile = new AgentProfile
        {
            UserId = user.Id,
            GreetingMessage = greeting,
            ClosingMessage = "Thank you for calling! Have a great day.",
            ServicesList = "Residential Cleaning, Deep Cleaning, Move-in/Move-out Cleaning, Commercial Cleaning",
            BusinessHours = "Monday-Friday 8AM-6PM",
            VoicemailMessage = "Sorry we missed your call. Please leave a message and we'll get back to you shortly.",
            Language = "en",
            Tone = "professional",
            TransferOnComplexIssues = true,
            EnableTroubleshooting = true
        };
        await _unitOfWork.Repository<AgentProfile>().AddAsync(agentProfile, cancellationToken);

        // Store initial AI agent config in profile
        var aiConfig = new
        {
            agentName,
            voiceName,
            greeting,
            personalityTone = "professional",
            language = "en-US",
            configuredByAdminId = adminId,
            configuredAt = DateTime.UtcNow
        };
        userProfile.AiAgentConfig = JsonSerializer.Serialize(aiConfig, JsonOptions);
        userProfile.ConfigUpdatedAt = DateTime.UtcNow;
        userProfile.ConfigUpdatedByAdminId = adminId;
        _unitOfWork.Repository<UserProfile>().Update(userProfile);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Admin {AdminId} created user {UserId} ({Email}) with voice agent {AgentName}",
            adminId, user.Id, user.Email, agentName);

        // Audit log
        var planInfo = pricingPlan != null ? $",\"pricingPlan\":\"{pricingPlan.Name}\"" : "";
        var trialInfo = isOnTrial ? $",\"trialDays\":{pricingPlan!.TrialDays}" : "";
        await _auditLogService.LogActionAsync(
            userId: adminId,
            action: "USER_CREATED",
            entityType: "User",
            entityId: user.Id,
            oldValues: null,
            newValues: $"{{\"email\":\"{user.Email}\",\"aiMinutes\":{aiMinutes},\"whatsApp\":{whatsAppMessages},\"voiceAgent\":\"{agentName}\"{planInfo}{trialInfo}}}",
            ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
            userAgent: HttpContext.Request.Headers["User-Agent"].ToString());

        // Send welcome email with temp password
        try
        {
            await _emailService.SendEmailAsync(
                user.Email,
                "Welcome to SpeakHalo - Your Account is Ready",
                GenerateWelcomeEmail(user.Name, user.Email, tempPassword),
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send welcome email to {Email}", user.Email);
        }

        // Notify admins in real-time
        _ = _adminNotifications.NotifyNewUserSignupAsync(user.Id, user.Email, request.Name);

        return Ok(new
        {
            message = "User created successfully. Temporary password sent via email.",
            userId = user.Id,
            email = user.Email,
            voiceAgentId = voiceAgent.Id,
            pricingPlan = pricingPlan != null ? new { pricingPlan.Id, pricingPlan.Name } : null,
            isOnTrial,
            trialEndsAt,
            limits = new
            {
                aiMinutes,
                whatsAppMessages,
                smsMessages,
                images
            }
        });
    }

    /// <summary>
    /// Admin updates user profile (business name, contact info, specialization)
    /// </summary>
    [HttpPut("{userId}/profile")]
    public async Task<IActionResult> UpdateProfile(
        string userId,
        [FromBody] AdminUpdateProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        var adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(adminId))
            return Unauthorized();

        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
        if (user == null || user.IsDeleted)
            return NotFound(new { error = "User not found" });

        var profiles = await _unitOfWork.Repository<UserProfile>()
            .FindAsync(p => p.UserId == userId, cancellationToken);
        var profile = profiles.FirstOrDefault();

        if (profile == null)
        {
            profile = new UserProfile
            {
                UserId = userId,
                BusinessName = user.BusinessName ?? "",
                Industry = user.Industry
            };
            await _unitOfWork.Repository<UserProfile>().AddAsync(profile, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        var oldValues = new
        {
            profile.BusinessName,
            profile.BusinessPhone,
            profile.BusinessAddress,
            profile.BusinessWebsite,
            profile.IndustrySpecialization,
            userName = user.Name
        };

        // Update profile fields (only non-null values)
        if (request.Name != null)
        {
            user.Name = request.Name;
            _unitOfWork.Repository<User>().Update(user);
        }
        if (request.BusinessName != null)
        {
            profile.BusinessName = request.BusinessName;
            user.BusinessName = request.BusinessName; // Keep legacy field in sync
            _unitOfWork.Repository<User>().Update(user);
        }
        if (request.BusinessPhone != null)
            profile.BusinessPhone = request.BusinessPhone;
        if (request.BusinessAddress != null)
            profile.BusinessAddress = request.BusinessAddress;
        if (request.BusinessWebsite != null)
            profile.BusinessWebsite = request.BusinessWebsite;
        if (request.IndustrySpecialization != null)
            profile.IndustrySpecialization = request.IndustrySpecialization;

        profile.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Repository<UserProfile>().Update(profile);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Admin {AdminId} updated profile for user {UserId}", adminId, userId);

        await _auditLogService.LogActionAsync(
            userId: adminId,
            action: "PROFILE_UPDATED",
            entityType: "UserProfile",
            entityId: userId,
            oldValues: JsonSerializer.Serialize(oldValues, JsonOptions),
            newValues: JsonSerializer.Serialize(new
            {
                profile.BusinessName,
                profile.BusinessPhone,
                profile.BusinessAddress,
                profile.BusinessWebsite,
                profile.IndustrySpecialization,
                userName = user.Name
            }, JsonOptions),
            ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
            userAgent: HttpContext.Request.Headers["User-Agent"].ToString());

        return Ok(new
        {
            message = "Profile updated successfully",
            userId,
            profile = new
            {
                name = user.Name,
                profile.BusinessName,
                profile.BusinessPhone,
                profile.BusinessAddress,
                profile.BusinessWebsite,
                profile.IndustrySpecialization
            }
        });
    }

    /// <summary>
    /// Admin enables/disables specific features for a user. Stored as JSON in UserSubscription.EnabledFeatures.
    /// </summary>
    [HttpPut("{userId}/features")]
    public async Task<IActionResult> UpdateFeatures(
        string userId,
        [FromBody] AdminUpdateFeaturesRequest request,
        CancellationToken cancellationToken = default)
    {
        var adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(adminId))
            return Unauthorized();

        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
        if (user == null || user.IsDeleted)
            return NotFound(new { error = "User not found" });

        var subs = await _unitOfWork.Repository<UserSubscription>()
            .FindAsync(s => s.UserId == userId, cancellationToken);
        var sub = subs.FirstOrDefault();

        if (sub == null)
            return NotFound(new { error = "User subscription not found" });

        // Parse existing features (or start with defaults — all enabled)
        var currentFeatures = new Dictionary<string, bool>();
        if (!string.IsNullOrEmpty(sub.EnabledFeatures))
        {
            try
            {
                currentFeatures = JsonSerializer.Deserialize<Dictionary<string, bool>>(sub.EnabledFeatures) ?? new();
            }
            catch
            {
                currentFeatures = new Dictionary<string, bool>();
            }
        }

        var oldFeatures = JsonSerializer.Serialize(currentFeatures, JsonOptions);

        // Apply requested feature changes (only non-null values)
        if (request.VoiceAI.HasValue) currentFeatures["voiceAI"] = request.VoiceAI.Value;
        if (request.WhatsApp.HasValue) currentFeatures["whatsApp"] = request.WhatsApp.Value;
        if (request.SMS.HasValue) currentFeatures["sms"] = request.SMS.Value;
        if (request.AIPhotoQuotes.HasValue) currentFeatures["aiPhotoQuotes"] = request.AIPhotoQuotes.Value;
        if (request.KnowledgeBase.HasValue) currentFeatures["knowledgeBase"] = request.KnowledgeBase.Value;
        if (request.Calendar.HasValue) currentFeatures["calendar"] = request.Calendar.Value;
        if (request.CustomBranding.HasValue) currentFeatures["customBranding"] = request.CustomBranding.Value;
        if (request.MultipleLocations.HasValue) currentFeatures["multipleLocations"] = request.MultipleLocations.Value;
        if (request.TeamMembers.HasValue) currentFeatures["teamMembers"] = request.TeamMembers.Value;
        if (request.AdvancedAnalytics.HasValue) currentFeatures["advancedAnalytics"] = request.AdvancedAnalytics.Value;

        sub.EnabledFeatures = JsonSerializer.Serialize(currentFeatures, JsonOptions);
        sub.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Repository<UserSubscription>().Update(sub);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Invalidate cached subscription state so RequireFeature picks up changes immediately
        try { await _cache.RemoveAsync($"user_sub:{userId}", cancellationToken); } catch { /* best-effort */ }

        _logger.LogInformation("Admin {AdminId} updated features for user {UserId}", adminId, userId);

        await _auditLogService.LogActionAsync(
            userId: adminId,
            action: "FEATURES_UPDATED",
            entityType: "UserSubscription",
            entityId: userId,
            oldValues: oldFeatures,
            newValues: sub.EnabledFeatures,
            ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
            userAgent: HttpContext.Request.Headers["User-Agent"].ToString());

        return Ok(new
        {
            message = "Features updated successfully",
            userId,
            enabledFeatures = currentFeatures
        });
    }

    /// <summary>
    /// Admin configures the user's AI agent (prompt, voice, personality, greeting).
    /// Stored in UserProfile.AiAgentConfig (JSONB). Also updates VoiceAgent and AgentProfile.
    /// </summary>
    [HttpPut("{userId}/ai-config")]
    public async Task<IActionResult> UpdateAiConfig(
        string userId,
        [FromBody] AdminUpdateAiConfigRequest request,
        CancellationToken cancellationToken = default)
    {
        var adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(adminId))
            return Unauthorized();

        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
        if (user == null || user.IsDeleted)
            return NotFound(new { error = "User not found" });

        // Validate voice name
        if (!string.IsNullOrEmpty(request.VoiceName) &&
            !ValidVoiceNames.Contains(request.VoiceName, StringComparer.OrdinalIgnoreCase))
            return BadRequest(new { error = $"Invalid voice name. Valid options: {string.Join(", ", ValidVoiceNames)}" });

        // Validate personality tone
        if (!string.IsNullOrEmpty(request.PersonalityTone) &&
            !ValidPersonalityTones.Contains(request.PersonalityTone, StringComparer.OrdinalIgnoreCase))
            return BadRequest(new { error = $"Invalid personality tone. Valid options: {string.Join(", ", ValidPersonalityTones)}" });

        var profiles = await _unitOfWork.Repository<UserProfile>()
            .FindAsync(p => p.UserId == userId, cancellationToken);
        var profile = profiles.FirstOrDefault();

        if (profile == null)
        {
            profile = new UserProfile
            {
                UserId = userId,
                BusinessName = user.BusinessName ?? "",
                Industry = user.Industry
            };
            await _unitOfWork.Repository<UserProfile>().AddAsync(profile, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        var oldConfig = profile.AiAgentConfig;

        // Build the AI agent config JSON
        var existingConfig = new Dictionary<string, object?>();
        if (!string.IsNullOrEmpty(profile.AiAgentConfig))
        {
            try
            {
                existingConfig = JsonSerializer.Deserialize<Dictionary<string, object?>>(profile.AiAgentConfig) ?? new();
            }
            catch
            {
                existingConfig = new Dictionary<string, object?>();
            }
        }

        // Apply requested changes
        if (request.AgentName != null) existingConfig["agentName"] = request.AgentName;
        if (request.VoiceName != null) existingConfig["voiceName"] = request.VoiceName;
        if (request.Greeting != null) existingConfig["greeting"] = request.Greeting;
        if (request.SystemPrompt != null) existingConfig["systemPrompt"] = request.SystemPrompt;
        if (request.PersonalityTone != null) existingConfig["personalityTone"] = request.PersonalityTone;
        if (request.Language != null) existingConfig["language"] = request.Language;
        if (request.SpecialInstructions != null) existingConfig["specialInstructions"] = request.SpecialInstructions;
        if (request.BusinessHoursMessage != null) existingConfig["businessHoursMessage"] = request.BusinessHoursMessage;
        if (request.AfterHoursMessage != null) existingConfig["afterHoursMessage"] = request.AfterHoursMessage;

        existingConfig["configuredByAdminId"] = adminId;
        existingConfig["configuredAt"] = DateTime.UtcNow.ToString("O");

        profile.AiAgentConfig = JsonSerializer.Serialize(existingConfig, JsonOptions);
        profile.ConfigUpdatedAt = DateTime.UtcNow;
        profile.ConfigUpdatedByAdminId = adminId;
        profile.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Repository<UserProfile>().Update(profile);

        // Also update the user's active VoiceAgent(s) if voice/prompt settings changed
        var voiceAgents = await _unitOfWork.Repository<VoiceAgent>()
            .FindAsync(v => v.UserId == userId && v.IsActive && !v.IsDeleted, cancellationToken);

        foreach (var va in voiceAgents)
        {
            var updated = false;
            if (request.AgentName != null) { va.Name = request.AgentName; updated = true; }
            if (request.VoiceName != null) { va.VoiceName = request.VoiceName; updated = true; }
            if (request.Language != null) { va.Language = request.Language; updated = true; }
            if (request.SystemPrompt != null) { va.SystemPrompt = request.SystemPrompt; updated = true; }
            if (updated)
            {
                va.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.Repository<VoiceAgent>().Update(va);
            }
        }

        // Also update the user's AgentProfile if relevant fields changed
        var agentProfiles = await _unitOfWork.Repository<AgentProfile>()
            .FindAsync(a => a.UserId == userId && !a.IsDeleted, cancellationToken);
        var agentProfile = agentProfiles.FirstOrDefault();

        if (agentProfile != null)
        {
            var apUpdated = false;
            if (request.Greeting != null) { agentProfile.GreetingMessage = request.Greeting; apUpdated = true; }
            if (request.PersonalityTone != null) { agentProfile.Tone = request.PersonalityTone; apUpdated = true; }
            if (request.Language != null) { agentProfile.Language = request.Language.Split('-').FirstOrDefault() ?? "en"; apUpdated = true; }
            if (request.SpecialInstructions != null)
            {
                agentProfile.CustomInstructions = string.Join("\n", request.SpecialInstructions);
                apUpdated = true;
            }
            if (request.BusinessHoursMessage != null) { agentProfile.BusinessHours = request.BusinessHoursMessage; apUpdated = true; }
            if (request.AfterHoursMessage != null) { agentProfile.VoicemailMessage = request.AfterHoursMessage; apUpdated = true; }
            if (apUpdated)
            {
                agentProfile.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.Repository<AgentProfile>().Update(agentProfile);
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Admin {AdminId} updated AI config for user {UserId}", adminId, userId);

        await _auditLogService.LogActionAsync(
            userId: adminId,
            action: "AI_CONFIG_UPDATED",
            entityType: "UserProfile",
            entityId: userId,
            oldValues: oldConfig,
            newValues: profile.AiAgentConfig,
            ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
            userAgent: HttpContext.Request.Headers["User-Agent"].ToString());

        return Ok(new
        {
            message = "AI configuration updated successfully",
            userId,
            aiConfig = existingConfig,
            voiceAgentsUpdated = voiceAgents.Count(),
            agentProfileUpdated = agentProfile != null
        });
    }

    /// <summary>
    /// Get detailed usage breakdown for a user
    /// </summary>
    [HttpGet("{userId}/usage")]
    public async Task<IActionResult> GetUserUsage(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
        if (user == null || user.IsDeleted)
            return NotFound(new { error = "User not found" });

        var subs = await _unitOfWork.Repository<UserSubscription>()
            .FindAsync(s => s.UserId == userId, cancellationToken);
        var sub = subs.FirstOrDefault();

        if (sub == null)
            return NotFound(new { error = "User subscription not found" });

        // Get call stats for this billing cycle
        var calls = await _unitOfWork.Calls
            .FindAsync(c => c.UserId == userId && c.CreatedAt >= sub.BillingCycleStartDate, cancellationToken);
        var callList = calls.ToList();

        // Get WhatsApp message count for this billing cycle
        var whatsAppConversations = await _unitOfWork.WhatsAppConversations
            .FindAsync(w => w.UserId == userId, cancellationToken);
        var whatsAppMessages = await _unitOfWork.WhatsAppMessages
            .FindAsync(m => whatsAppConversations.Select(c => c.Id).Contains(m.ConversationId)
                           && m.CreatedAt >= sub.BillingCycleStartDate, cancellationToken);

        // Get SMS message count for this billing cycle
        var smsConversations = await _unitOfWork.SMSConversations
            .FindAsync(s => s.UserId == userId, cancellationToken);
        var smsMessages = await _unitOfWork.SMSMessages
            .FindAsync(m => smsConversations.Select(c => c.Id).Contains(m.ConversationId)
                           && m.CreatedAt >= sub.BillingCycleStartDate, cancellationToken);

        return Ok(new
        {
            userId,
            billingCycle = new
            {
                startDate = sub.BillingCycleStartDate,
                endDate = sub.NextBillingDate,
                daysRemaining = Math.Max(0, (sub.NextBillingDate - DateTime.UtcNow).Days)
            },
            aiMinutes = new
            {
                used = sub.AIMinutesUsedThisMonth,
                limit = sub.AIMinutesIncluded,
                remaining = Math.Max(0, sub.AIMinutesIncluded - sub.AIMinutesUsedThisMonth),
                percentUsed = sub.AIMinutesIncluded > 0
                    ? Math.Round((double)sub.AIMinutesUsedThisMonth / sub.AIMinutesIncluded * 100, 1) : 0
            },
            whatsApp = new
            {
                used = sub.WhatsAppMessagesUsedThisMonth,
                limit = sub.WhatsAppMessagesIncluded,
                remaining = Math.Max(0, sub.WhatsAppMessagesIncluded - sub.WhatsAppMessagesUsedThisMonth),
                percentUsed = sub.WhatsAppMessagesIncluded > 0
                    ? Math.Round((double)sub.WhatsAppMessagesUsedThisMonth / sub.WhatsAppMessagesIncluded * 100, 1) : 0
            },
            sms = new
            {
                used = sub.SMSMessagesUsedThisMonth,
                limit = sub.SMSMessagesIncluded,
                remaining = Math.Max(0, sub.SMSMessagesIncluded - sub.SMSMessagesUsedThisMonth),
                percentUsed = sub.SMSMessagesIncluded > 0
                    ? Math.Round((double)sub.SMSMessagesUsedThisMonth / sub.SMSMessagesIncluded * 100, 1) : 0
            },
            images = new
            {
                used = sub.ImagesUsedThisMonth,
                limit = sub.ImagesIncluded,
                remaining = Math.Max(0, sub.ImagesIncluded - sub.ImagesUsedThisMonth),
                percentUsed = sub.ImagesIncluded > 0
                    ? Math.Round((double)sub.ImagesUsedThisMonth / sub.ImagesIncluded * 100, 1) : 0
            },
            callStats = new
            {
                totalCalls = callList.Count,
                totalDurationSeconds = callList.Sum(c => c.DurationSeconds),
                averageDurationSeconds = callList.Count > 0
                    ? Math.Round(callList.Average(c => c.DurationSeconds), 0) : 0,
                appointmentsScheduled = callList.Count(c => c.AppointmentScheduled),
                quotesRequested = callList.Count(c => c.QuoteRequested),
                transferred = callList.Count(c => c.WasTransferred)
            },
            messagingStats = new
            {
                whatsAppConversations = whatsAppConversations.Count(),
                whatsAppMessagesThisCycle = whatsAppMessages.Count(),
                smsConversations = smsConversations.Count(),
                smsMessagesThisCycle = smsMessages.Count()
            },
            enforceHardLimits = sub.EnforceHardLimits
        });
    }

    /// <summary>
    /// Soft-delete a user (set IsDeleted=true, IsActive=false)
    /// </summary>
    [HttpDelete("{userId}")]
    public async Task<IActionResult> DeleteUser(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(adminId))
            return Unauthorized();

        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
        if (user == null)
            return NotFound(new { error = "User not found" });

        if (user.IsDeleted)
            return BadRequest(new { error = "User is already deleted" });

        // Prevent deleting admin accounts
        if (user.Role == UserRole.Admin)
            return BadRequest(new { error = "Cannot delete admin accounts" });

        // Soft-delete the user
        user.IsDeleted = true;
        user.IsActive = false;
        user.DeletedAt = DateTime.UtcNow;
        user.DeletedBy = adminId;
        user.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Repository<User>().Update(user);

        // Also suspend their subscription
        var subs = await _unitOfWork.Repository<UserSubscription>()
            .FindAsync(s => s.UserId == userId, cancellationToken);
        var sub = subs.FirstOrDefault();
        if (sub != null)
        {
            sub.HasActivePayment = false;
            sub.IsTemporarilySuspended = true;
            sub.SuspendedReason = "Account deleted by admin";
            sub.SuspendedAt = DateTime.UtcNow;
            sub.SuspendedByAdminId = adminId;
            sub.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Repository<UserSubscription>().Update(sub);
        }

        // Deactivate voice agents
        var voiceAgents = await _unitOfWork.Repository<VoiceAgent>()
            .FindAsync(v => v.UserId == userId && !v.IsDeleted, cancellationToken);
        foreach (var va in voiceAgents)
        {
            va.IsActive = false;
            va.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Repository<VoiceAgent>().Update(va);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Admin {AdminId} soft-deleted user {UserId} ({Email})", adminId, userId, user.Email);

        await _auditLogService.LogActionAsync(
            userId: adminId,
            action: "USER_DELETED",
            entityType: "User",
            entityId: userId,
            oldValues: $"{{\"isActive\":true,\"isDeleted\":false}}",
            newValues: $"{{\"isActive\":false,\"isDeleted\":true,\"deletedBy\":\"{adminId}\"}}",
            ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
            userAgent: HttpContext.Request.Headers["User-Agent"].ToString());

        // Notify admins in real-time
        _ = _adminNotifications.NotifyUserDeletedAsync(userId, user.Email, adminId);

        return Ok(new
        {
            message = "User deleted successfully",
            userId,
            email = user.Email
        });
    }

    /// <summary>
    /// Update usage limits for a user
    /// </summary>
    [HttpPut("{userId}/limits")]
    public async Task<IActionResult> UpdateLimits(
        string userId,
        [FromBody] AdminUpdateLimitsRequest request,
        CancellationToken cancellationToken = default)
    {
        var adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(adminId))
            return Unauthorized();

        var subs = await _unitOfWork.Repository<UserSubscription>()
            .FindAsync(s => s.UserId == userId, cancellationToken);
        var sub = subs.FirstOrDefault();

        if (sub == null)
            return NotFound(new { error = "User subscription not found" });

        var oldLimits = $"AI:{sub.AIMinutesIncluded},WA:{sub.WhatsAppMessagesIncluded},SMS:{sub.SMSMessagesIncluded},Img:{sub.ImagesIncluded}";

        if (request.AIMinutes.HasValue)
            sub.AIMinutesIncluded = request.AIMinutes.Value;
        if (request.WhatsAppMessages.HasValue)
            sub.WhatsAppMessagesIncluded = request.WhatsAppMessages.Value;
        if (request.SMSMessages.HasValue)
            sub.SMSMessagesIncluded = request.SMSMessages.Value;
        if (request.Images.HasValue)
            sub.ImagesIncluded = request.Images.Value;
        if (request.EnforceHardLimits.HasValue)
            sub.EnforceHardLimits = request.EnforceHardLimits.Value;

        _unitOfWork.Repository<UserSubscription>().Update(sub);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var newLimits = $"AI:{sub.AIMinutesIncluded},WA:{sub.WhatsAppMessagesIncluded},SMS:{sub.SMSMessagesIncluded},Img:{sub.ImagesIncluded}";

        _logger.LogInformation("Admin {AdminId} updated limits for user {UserId}: {OldLimits} -> {NewLimits}",
            adminId, userId, oldLimits, newLimits);

        await _auditLogService.LogActionAsync(
            userId: adminId,
            action: "LIMITS_UPDATED",
            entityType: "UserSubscription",
            entityId: userId,
            oldValues: oldLimits,
            newValues: newLimits,
            ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
            userAgent: HttpContext.Request.Headers["User-Agent"].ToString());

        return Ok(new
        {
            message = "Limits updated successfully",
            userId,
            limits = new
            {
                aiMinutes = sub.AIMinutesIncluded,
                whatsAppMessages = sub.WhatsAppMessagesIncluded,
                smsMessages = sub.SMSMessagesIncluded,
                images = sub.ImagesIncluded,
                enforceHardLimits = sub.EnforceHardLimits
            }
        });
    }

    /// <summary>
    /// Trigger password reset for a user — sends reset link email
    /// </summary>
    [HttpPost("{userId}/reset-password")]
    public async Task<IActionResult> TriggerPasswordReset(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(adminId))
            return Unauthorized();

        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
        if (user == null)
            return NotFound(new { error = "User not found" });

        var auths = await _unitOfWork.Repository<UserAuth>()
            .FindAsync(a => a.UserId == userId, cancellationToken);
        var auth = auths.FirstOrDefault();

        if (auth == null)
            return NotFound(new { error = "User auth not found" });

        // Generate reset token
        var resetToken = Convert.ToHexString(
            System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        auth.PasswordResetToken = BCrypt.Net.BCrypt.HashPassword(resetToken, workFactor: 10);
        auth.PasswordResetExpires = DateTime.UtcNow.AddHours(AppConstants.PasswordResetTokenExpirationHours);

        _unitOfWork.Repository<UserAuth>().Update(auth);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Admin {AdminId} triggered password reset for user {UserId}", adminId, userId);

        // Send password reset email
        try
        {
            var resetUrl = $"{_frontendBaseUrl}/reset-password?token={resetToken}&email={Uri.EscapeDataString(user.Email)}";
            await _emailService.SendEmailAsync(
                user.Email,
                "SpeakHalo - Password Reset",
                GeneratePasswordResetEmail(user.Name, resetUrl),
                cancellationToken: cancellationToken);

            _logger.LogInformation("Password reset email sent to {Email}", user.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send password reset email to {Email}", user.Email);
            return StatusCode(500, new { error = "Password reset initiated but email failed to send" });
        }

        await _auditLogService.LogActionAsync(
            userId: adminId,
            action: "PASSWORD_RESET_TRIGGERED",
            entityType: "User",
            entityId: userId,
            oldValues: null,
            newValues: $"{{\"triggeredBy\":\"admin\",\"expiresAt\":\"{auth.PasswordResetExpires:O}\"}}",
            ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
            userAgent: HttpContext.Request.Headers["User-Agent"].ToString());

        return Ok(new { message = "Password reset email sent successfully", email = user.Email });
    }

    /// <summary>
    /// Suspend or unsuspend a user account
    /// </summary>
    [HttpPut("{userId}/suspend")]
    public async Task<IActionResult> SuspendUser(
        string userId,
        [FromBody] AdminSuspendRequest request,
        CancellationToken cancellationToken = default)
    {
        var adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(adminId))
            return Unauthorized();

        var subs = await _unitOfWork.Repository<UserSubscription>()
            .FindAsync(s => s.UserId == userId, cancellationToken);
        var sub = subs.FirstOrDefault();

        if (sub == null)
            return NotFound(new { error = "User subscription not found" });

        sub.IsTemporarilySuspended = request.Suspend;
        sub.SuspendedReason = request.Suspend ? request.Reason : null;
        sub.SuspendedAt = request.Suspend ? DateTime.UtcNow : null;
        sub.SuspendedByAdminId = request.Suspend ? adminId : null;

        _unitOfWork.Repository<UserSubscription>().Update(sub);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var action = request.Suspend ? "suspended" : "unsuspended";
        _logger.LogInformation("Admin {AdminId} {Action} user {UserId}", adminId, action, userId);

        // Notify admins in real-time
        var targetUser = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
        if (request.Suspend)
            _ = _adminNotifications.NotifyUserSuspendedAsync(userId, targetUser?.Email ?? userId, request.Reason, adminId);
        else
            _ = _adminNotifications.NotifyUserActivatedAsync(userId, targetUser?.Email ?? userId, adminId);

        return Ok(new { message = $"User {action} successfully", userId });
    }

    /// <summary>
    /// Activate a user account (set HasActivePayment = true)
    /// </summary>
    [HttpPut("{userId}/activate")]
    public async Task<IActionResult> ActivateUser(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(adminId))
            return Unauthorized();

        var subs = await _unitOfWork.Repository<UserSubscription>()
            .FindAsync(s => s.UserId == userId, cancellationToken);
        var sub = subs.FirstOrDefault();

        if (sub == null)
            return NotFound(new { error = "User subscription not found" });

        sub.HasActivePayment = true;
        sub.RequiresPayment = false;
        sub.IsTemporarilySuspended = false;
        sub.SuspendedReason = null;

        _unitOfWork.Repository<UserSubscription>().Update(sub);

        // Also ensure the User account itself is active
        var activatedUser = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
        if (activatedUser != null && !activatedUser.IsActive)
        {
            activatedUser.IsActive = true;
            _unitOfWork.Users.Update(activatedUser);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Admin {AdminId} activated user {UserId}", adminId, userId);

        // Notify admins in real-time
        _ = _adminNotifications.NotifyUserActivatedAsync(userId, activatedUser?.Email ?? userId, adminId);

        // Send activation email to user
        if (activatedUser != null)
        {
            var planName = sub.Plan ?? "your selected plan";
            var emailHtml = $@"
                <div style='font-family: sans-serif; max-width: 600px; margin: 0 auto;'>
                    <h2 style='color: #1e293b;'>Your SpeakHalo Account is Approved!</h2>
                    <p>Hi {activatedUser.Name},</p>
                    <p>Great news! Your SpeakHalo account has been reviewed and approved. You can now sign in and start using your <strong>{planName}</strong> plan.</p>
                    <p style='margin: 24px 0;'>
                        <a href='https://speakhalo.com/signin' style='background: #2563eb; color: white; padding: 12px 24px; text-decoration: none; border-radius: 6px; font-weight: 600;'>Sign In to SpeakHalo</a>
                    </p>
                    <p style='color: #64748b; font-size: 14px;'>If you have any questions, reply to this email or contact us at support@speakhalo.com.</p>
                    <p style='color: #64748b; font-size: 14px;'>Welcome aboard!</p>
                    <p style='color: #64748b; font-size: 14px;'>The SpeakHalo Team</p>
                </div>";
            _ = _emailService.SendEmailAsync(activatedUser.Email, "Your SpeakHalo Account is Approved!", emailHtml);
        }

        return Ok(new { message = "User activated successfully", userId });
    }

    /// <summary>
    /// Reset usage counters for a user (start fresh billing cycle)
    /// </summary>
    [HttpPost("{userId}/reset-usage")]
    public async Task<IActionResult> ResetUsage(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(adminId))
            return Unauthorized();

        var subs = await _unitOfWork.Repository<UserSubscription>()
            .FindAsync(s => s.UserId == userId, cancellationToken);
        var sub = subs.FirstOrDefault();

        if (sub == null)
            return NotFound(new { error = "User subscription not found" });

        sub.AIMinutesUsedThisMonth = 0;
        sub.WhatsAppMessagesUsedThisMonth = 0;
        sub.SMSMessagesUsedThisMonth = 0;
        sub.ImagesUsedThisMonth = 0;
        sub.BillingCycleStartDate = DateTime.UtcNow;
        sub.NextBillingDate = DateTime.UtcNow.AddMonths(1);

        _unitOfWork.Repository<UserSubscription>().Update(sub);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Admin {AdminId} reset usage for user {UserId}", adminId, userId);

        return Ok(new { message = "Usage counters reset successfully", userId });
    }

    /// <summary>
    /// Get a user's billing details including Paddle info.
    /// </summary>
    [HttpGet("{userId}/billing")]
    public async Task<IActionResult> GetUserBilling(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
        if (user == null || user.IsDeleted)
            return NotFound(new { error = "User not found" });

        var subs = await _unitOfWork.Repository<UserSubscription>()
            .FindAsync(s => s.UserId == userId, cancellationToken);
        var sub = subs.FirstOrDefault();
        if (sub == null)
            return NotFound(new { error = "User subscription not found" });

        string? planName = null;
        decimal? planPrice = null;
        if (!string.IsNullOrEmpty(sub.PricingPlanId))
        {
            var plan = await _unitOfWork.Repository<PricingPlan>()
                .GetByIdAsync(sub.PricingPlanId, cancellationToken);
            if (plan != null)
            {
                planName = plan.Name;
                planPrice = plan.DisplayPrice;
            }
        }

        var refundEligible = sub.FirstPaymentAt.HasValue
            && !sub.RefundedAt.HasValue
            && (DateTime.UtcNow - sub.FirstPaymentAt.Value).TotalDays <= 14;

        return Ok(new
        {
            userId,
            planName,
            planPrice,
            sub.PaddleCustomerId,
            sub.PaddleSubscriptionId,
            sub.PaddleSubscriptionStatus,
            sub.PaddlePriceId,
            sub.HasActivePayment,
            sub.IsOnTrial,
            sub.TrialEndsAt,
            sub.FirstPaymentAt,
            sub.PaddleSubscriptionCreatedAt,
            sub.PaddleCanceledAt,
            sub.PaddleCurrentPeriodEnd,
            sub.RefundedAt,
            sub.RefundReason,
            refundEligible,
            sub.BillingCycleStartDate,
            sub.NextBillingDate
        });
    }

    /// <summary>
    /// Admin cancels a user's Paddle subscription.
    /// </summary>
    [HttpPost("{userId}/cancel-subscription")]
    public async Task<IActionResult> AdminCancelSubscription(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(adminId))
            return Unauthorized();

        var subs = await _unitOfWork.Repository<UserSubscription>()
            .FindAsync(s => s.UserId == userId, cancellationToken);
        var sub = subs.FirstOrDefault();
        if (sub == null)
            return NotFound(new { error = "User subscription not found" });

        if (!string.IsNullOrEmpty(sub.PaddleSubscriptionId))
        {
            var paddleService = HttpContext.RequestServices.GetRequiredService<IPaddleService>();
            var cancelResult = await paddleService.CancelSubscriptionAsync(sub.PaddleSubscriptionId, true, cancellationToken);
            if (!cancelResult.IsSuccess)
            {
                _logger.LogWarning("Failed to cancel Paddle subscription for user {UserId}: {Error}",
                    userId, cancelResult.Errors.FirstOrDefault());
            }
        }

        sub.HasActivePayment = false;
        sub.PaddleSubscriptionStatus = "canceled";
        sub.PaddleCanceledAt = DateTime.UtcNow;
        _unitOfWork.Repository<UserSubscription>().Update(sub);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Admin {AdminId} canceled subscription for user {UserId}", adminId, userId);

        await _auditLogService.LogActionAsync(
            userId: adminId,
            action: "SUBSCRIPTION_CANCELED",
            entityType: "UserSubscription",
            entityId: userId,
            oldValues: null,
            newValues: $"{{\"canceledBy\":\"admin\",\"adminId\":\"{adminId}\"}}",
            ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
            userAgent: HttpContext.Request.Headers["User-Agent"].ToString());

        return Ok(new { message = "Subscription canceled successfully" });
    }

    /// <summary>
    /// Admin issues a refund for a user (within 30-day window).
    /// </summary>
    [HttpPost("{userId}/refund")]
    public async Task<IActionResult> AdminRefundUser(
        string userId,
        [FromBody] SpeakHalo.Application.DTOs.Payment.AdminRefundRequest request,
        CancellationToken cancellationToken = default)
    {
        var adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(adminId))
            return Unauthorized();

        var subs = await _unitOfWork.Repository<UserSubscription>()
            .FindAsync(s => s.UserId == userId, cancellationToken);
        var sub = subs.FirstOrDefault();
        if (sub == null)
            return NotFound(new { error = "User subscription not found" });

        if (!sub.FirstPaymentAt.HasValue)
            return BadRequest(new { error = "No payment found to refund" });

        if (sub.RefundedAt.HasValue)
            return BadRequest(new { error = "Already refunded" });

        if ((DateTime.UtcNow - sub.FirstPaymentAt.Value).TotalDays > 30)
            return BadRequest(new { error = "Refund window (30 days) has expired" });

        // Mark as refunded and cancel
        sub.RefundedAt = DateTime.UtcNow;
        sub.RefundReason = request.Reason;
        sub.HasActivePayment = false;
        sub.PaddleSubscriptionStatus = "canceled";
        sub.PaddleCanceledAt = DateTime.UtcNow;
        _unitOfWork.Repository<UserSubscription>().Update(sub);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Cancel Paddle subscription if exists
        if (!string.IsNullOrEmpty(sub.PaddleSubscriptionId))
        {
            var paddleService = HttpContext.RequestServices.GetRequiredService<IPaddleService>();
            await paddleService.CancelSubscriptionAsync(sub.PaddleSubscriptionId, true, cancellationToken);
        }

        _logger.LogInformation("Admin {AdminId} issued refund for user {UserId}: {Reason}", adminId, userId, request.Reason);

        await _auditLogService.LogActionAsync(
            userId: adminId,
            action: "REFUND_ISSUED",
            entityType: "UserSubscription",
            entityId: userId,
            oldValues: null,
            newValues: $"{{\"reason\":\"{request.Reason}\",\"adminId\":\"{adminId}\"}}",
            ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
            userAgent: HttpContext.Request.Headers["User-Agent"].ToString());

        return Ok(new { message = "Refund processed successfully" });
    }

    private static string GenerateRandomPassword()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";
        var bytes = System.Security.Cryptography.RandomNumberGenerator.GetBytes(12);
        return new string(bytes.Select(b => chars[b % chars.Length]).ToArray());
    }

    private string GenerateWelcomeEmail(string name, string email, string tempPassword)
    {
        return $@"<!DOCTYPE html>
<html><head><meta charset='utf-8'><meta name='viewport' content='width=device-width,initial-scale=1'></head>
<body style='font-family:-apple-system,BlinkMacSystemFont,""Segoe UI"",Roboto,sans-serif;line-height:1.6;color:#333;margin:0;padding:0;background:#f4f4f4;'>
<div style='max-width:600px;margin:40px auto;background:white;border-radius:8px;box-shadow:0 2px 4px rgba(0,0,0,0.1);overflow:hidden;'>
  <div style='background:linear-gradient(135deg,#667eea 0%,#764ba2 100%);padding:40px 30px;text-align:center;'>
    <h1 style='color:white;margin:0;font-size:28px;'>Welcome to SpeakHalo!</h1>
    <p style='color:rgba(255,255,255,0.9);margin:10px 0 0;font-size:16px;'>Your AI-powered cleaning service receptionist</p>
  </div>
  <div style='padding:40px 30px;'>
    <p style='font-size:16px;'>Hi {System.Net.WebUtility.HtmlEncode(name)},</p>
    <p style='font-size:16px;'>Your SpeakHalo account has been created. Here are your login credentials:</p>
    <div style='background:#f8f9fa;border-left:4px solid #28a745;padding:16px;margin:20px 0;border-radius:4px;'>
      <h3 style='margin:0 0 10px;color:#28a745;'>Your Login Credentials</h3>
      <p style='margin:5px 0;'><strong>Email:</strong> {System.Net.WebUtility.HtmlEncode(email)}</p>
      <p style='margin:5px 0;'><strong>Temporary Password:</strong> <code style='background:#e9ecef;padding:4px 8px;border-radius:3px;'>{System.Net.WebUtility.HtmlEncode(tempPassword)}</code></p>
      <p style='margin:10px 0 0;font-size:13px;color:#6c757d;'>You will be required to change this password on your first login.</p>
    </div>
    <div style='text-align:center;margin:30px 0;'>
      <a href='{_frontendBaseUrl}/login' style='display:inline-block;background:linear-gradient(135deg,#667eea 0%,#764ba2 100%);color:white;padding:14px 32px;text-decoration:none;border-radius:6px;font-weight:600;font-size:16px;'>Log In to Your Dashboard</a>
    </div>
    <p style='font-size:14px;color:#666;'>Questions? Contact us at <a href='mailto:{_supportEmail}' style='color:#667eea;'>{_supportEmail}</a></p>
  </div>
  <div style='background:#f8f9fa;padding:20px 30px;text-align:center;border-top:1px solid #e9ecef;'>
    <p style='margin:0;font-size:13px;color:#6c757d;'>© 2026 SpeakHalo. All rights reserved.</p>
  </div>
</div>
</body></html>";
    }

    private static string GeneratePasswordResetEmail(string name, string resetUrl)
    {
        return $@"<!DOCTYPE html>
<html><head><meta charset='utf-8'><meta name='viewport' content='width=device-width,initial-scale=1'></head>
<body style='font-family:-apple-system,BlinkMacSystemFont,""Segoe UI"",Roboto,sans-serif;line-height:1.6;color:#333;margin:0;padding:0;background:#f4f4f4;'>
<div style='max-width:600px;margin:40px auto;background:white;border-radius:8px;box-shadow:0 2px 4px rgba(0,0,0,0.1);overflow:hidden;'>
  <div style='background:linear-gradient(135deg,#667eea 0%,#764ba2 100%);padding:40px 30px;text-align:center;'>
    <h1 style='color:white;margin:0;font-size:28px;'>Password Reset</h1>
  </div>
  <div style='padding:40px 30px;'>
    <p style='font-size:16px;'>Hi {System.Net.WebUtility.HtmlEncode(name)},</p>
    <p style='font-size:16px;'>Your administrator has requested a password reset for your SpeakHalo account. Click the button below to set a new password:</p>
    <div style='text-align:center;margin:30px 0;'>
      <a href='{resetUrl}' style='display:inline-block;background:linear-gradient(135deg,#667eea 0%,#764ba2 100%);color:white;padding:14px 32px;text-decoration:none;border-radius:6px;font-weight:600;font-size:16px;'>Reset Password</a>
    </div>
    <p style='font-size:14px;color:#666;'>This link expires in 24 hours. If you didn't expect this, contact your administrator.</p>
  </div>
  <div style='background:#f8f9fa;padding:20px 30px;text-align:center;border-top:1px solid #e9ecef;'>
    <p style='margin:0;font-size:13px;color:#6c757d;'>© 2026 SpeakHalo. All rights reserved.</p>
  </div>
</div>
</body></html>";
    }
}

