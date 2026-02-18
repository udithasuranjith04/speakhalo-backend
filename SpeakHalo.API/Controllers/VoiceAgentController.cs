using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpeakHalo.API.Filters;
using SpeakHalo.Application.Interfaces;
using SpeakHalo.Domain.Entities;
using SpeakHalo.Domain.Enums;
using SpeakHalo.Domain.Interfaces.Repositories;
using System.Net;
using System.Net.Sockets;
using System.Security.Claims;

namespace SpeakHalo.API.Controllers;

/// <summary>
/// Manages voice agent configurations and initiates outbound calls.
/// Uses Gemini for AI conversation via Twilio Media Streams.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/voice-agents")]
[Authorize]
[RequireFeature(PlanFeature.VoiceAI)]
public class VoiceAgentController : ControllerBase
{
    private readonly IAiChatService _chatService;
    private readonly ITwilioService _twilioService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _configuration;
    private readonly ILogger<VoiceAgentController> _logger;

    public VoiceAgentController(
        IAiChatService chatService,
        ITwilioService twilioService,
        IUnitOfWork unitOfWork,
        IConfiguration configuration,
        ILogger<VoiceAgentController> logger)
    {
        _chatService = chatService;
        _twilioService = twilioService;
        _unitOfWork = unitOfWork;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Create a new voice agent configuration
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateAgent(
        [FromBody] CreateVoiceAgentRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        // All active users have all features including multi-location (admin controls limits)

        var agent = new VoiceAgent
        {
            UserId = userId,
            Name = request.Name,
            SystemPrompt = request.SystemPrompt ?? BuildDefaultSystemPrompt(request.BusinessName, request.Industry),
            VoiceName = request.VoiceName ?? "Kore",
            Language = request.Language ?? "en-US",
            MaxCallDurationMinutes = request.MaxCallDurationMinutes ?? 30,
            IsActive = true,
            LocationName = request.LocationName,
            LocationAddress = request.LocationAddress,
            LocationPhone = request.LocationPhone
        };

        await _unitOfWork.Repository<VoiceAgent>().AddAsync(agent, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created voice agent {AgentId} ({AgentName}) for user {UserId}", agent.Id, agent.Name, userId);

        return Ok(new
        {
            agentId = agent.Id,
            name = agent.Name,
            voiceName = agent.VoiceName,
            language = agent.Language,
            maxCallDurationMinutes = agent.MaxCallDurationMinutes,
            isActive = agent.IsActive,
            locationName = agent.LocationName,
            locationAddress = agent.LocationAddress,
            locationPhone = agent.LocationPhone,
            createdAt = agent.CreatedAt
        });
    }

    /// <summary>
    /// Get voice agent configuration
    /// </summary>
    [HttpGet("{agentId}")]
    public async Task<IActionResult> GetAgent(string agentId, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var agent = await _unitOfWork.Repository<VoiceAgent>().GetByIdAsync(agentId, cancellationToken);
        if (agent == null || agent.UserId != userId)
            return NotFound(new { error = "Voice agent not found" });

        return Ok(new
        {
            agentId = agent.Id,
            name = agent.Name,
            voiceName = agent.VoiceName,
            language = agent.Language,
            maxCallDurationMinutes = agent.MaxCallDurationMinutes,
            isActive = agent.IsActive,
            locationName = agent.LocationName,
            locationAddress = agent.LocationAddress,
            locationPhone = agent.LocationPhone,
            createdAt = agent.CreatedAt
        });
    }

    /// <summary>
    /// List voice agents for the current user
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ListAgents(CancellationToken cancellationToken)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var agents = await _unitOfWork.Repository<VoiceAgent>()
            .FindAsync(a => a.UserId == userId, cancellationToken);

        return Ok(agents.Select(a => new
        {
            agentId = a.Id,
            name = a.Name,
            voiceName = a.VoiceName,
            language = a.Language,
            maxCallDurationMinutes = a.MaxCallDurationMinutes,
            isActive = a.IsActive,
            locationName = a.LocationName,
            locationAddress = a.LocationAddress,
            locationPhone = a.LocationPhone,
            createdAt = a.CreatedAt
        }));
    }

    /// <summary>
    /// Update voice agent configuration
    /// </summary>
    [HttpPatch("{agentId}")]
    public async Task<IActionResult> UpdateAgent(
        string agentId,
        [FromBody] UpdateVoiceAgentRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var agent = await _unitOfWork.Repository<VoiceAgent>().GetByIdAsync(agentId, cancellationToken);
        if (agent == null || agent.UserId != userId)
            return NotFound(new { error = "Voice agent not found" });

        if (request.Name != null) agent.Name = request.Name;
        if (request.SystemPrompt != null) agent.SystemPrompt = request.SystemPrompt;
        if (request.VoiceName != null) agent.VoiceName = request.VoiceName;
        if (request.IsActive.HasValue) agent.IsActive = request.IsActive.Value;
        if (request.LocationName != null) agent.LocationName = request.LocationName;
        if (request.LocationAddress != null) agent.LocationAddress = request.LocationAddress;
        if (request.LocationPhone != null) agent.LocationPhone = request.LocationPhone;
        agent.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Repository<VoiceAgent>().Update(agent);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated voice agent {AgentId}", agentId);

        return Ok(new
        {
            agentId = agent.Id,
            name = agent.Name,
            voiceName = agent.VoiceName,
            isActive = agent.IsActive,
            locationName = agent.LocationName,
            locationAddress = agent.LocationAddress,
            locationPhone = agent.LocationPhone,
            updatedAt = agent.UpdatedAt
        });
    }

    /// <summary>
    /// Delete voice agent configuration (soft delete)
    /// </summary>
    [HttpDelete("{agentId}")]
    public async Task<IActionResult> DeleteAgent(string agentId, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var agent = await _unitOfWork.Repository<VoiceAgent>().GetByIdAsync(agentId, cancellationToken);
        if (agent == null || agent.UserId != userId)
            return NotFound(new { error = "Voice agent not found" });
        agent.IsDeleted = true;
        agent.DeletedAt = DateTime.UtcNow;
        agent.DeletedBy = userId;
        _unitOfWork.Repository<VoiceAgent>().Update(agent);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted voice agent {AgentId}", agentId);
        return Ok(new { agentId, deleted = true });
    }

    /// <summary>
    /// Initiate an outbound call using the voice agent
    /// </summary>
    [HttpPost("{agentId}/call")]
    public async Task<IActionResult> InitiateOutboundCall(
        string agentId,
        [FromBody] OutboundCallRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        // Validate phone number format (E.164)
        if (string.IsNullOrWhiteSpace(request.ToNumber) ||
            !System.Text.RegularExpressions.Regex.IsMatch(request.ToNumber, @"^\+[1-9]\d{1,14}$"))
            return BadRequest(new { error = "Invalid phone number format. Must be E.164 format (e.g., +12025551234)" });

        var agent = await _unitOfWork.Repository<VoiceAgent>().GetByIdAsync(agentId, cancellationToken);
        if (agent == null || agent.UserId != userId)
            return NotFound(new { error = "Voice agent not found" });

        if (!agent.IsActive)
            return BadRequest(new { error = "Voice agent is not active" });

        try
        {
            var baseUrl = _configuration["Application:BaseUrl"] ?? "https://api.speakhalo.com";
            var callerPhone = Uri.EscapeDataString(request.ToNumber);
            var mediaStreamUrl = $"wss://{new Uri(baseUrl).Host}/api/v1/voice-stream/{agentId}?callerPhone={callerPhone}";

            // Recognize returning customer for outbound call
            string? customerId = null;
            var callerRecognition = HttpContext.RequestServices.GetService<ICallerRecognitionService>();
            if (callerRecognition != null)
            {
                var recognized = await callerRecognition.RecognizeCallerAsync(userId, request.ToNumber, "voice", cancellationToken);
                if (recognized != null)
                    customerId = recognized.CustomerId;
            }

            // Create call record
            var call = new Call
            {
                UserId = userId,
                CustomerId = customerId,
                CallerPhone = request.FromNumber ?? _configuration["Twilio:PhoneNumber"] ?? "",
                RecipientPhone = request.ToNumber,
                Direction = "outbound",
                Notes = $"Voice agent: {agentId}",
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Repository<Call>().AddAsync(call, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Initiated outbound call {CallId} to {ToNumber} via agent {AgentId}",
                call.Id, request.ToNumber, agentId);

            return Ok(new
            {
                callId = call.Id,
                status = "initiated",
                agentId,
                toNumber = request.ToNumber
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initiate outbound call");
            return StatusCode(500, new { error = "Failed to initiate call" });
        }
    }

    /// <summary>
    /// Get call details
    /// </summary>
    [HttpGet("calls/{callId}")]
    public async Task<IActionResult> GetCallDetails(
        string callId,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var call = await _unitOfWork.Repository<Call>().GetByIdAsync(callId, cancellationToken);
        if (call == null || call.UserId != userId)
            return NotFound(new { error = "Call not found" });

        return Ok(new
        {
            callId = call.Id,
            direction = call.Direction,
            callerPhone = call.CallerPhone,
            recipientPhone = call.RecipientPhone,
            durationSeconds = call.DurationSeconds,
            createdAt = call.CreatedAt,
            endedAt = call.EndedAt,
            notes = call.Notes
        });
    }

    /// <summary>
    /// Get custom branding for the current user's agent profile
    /// </summary>
    [HttpGet("branding")]
    [RequireFeature(PlanFeature.CustomBranding)]
    public async Task<IActionResult> GetBranding(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var profiles = await _unitOfWork.Repository<AgentProfile>()
            .FindAsync(p => p.UserId == userId && !p.IsDeleted, cancellationToken);
        var profile = profiles.FirstOrDefault();

        if (profile == null)
        {
            return Ok(new
            {
                brandName = (string?)null,
                brandTagline = (string?)null,
                brandColor = (string?)null,
                logoUrl = (string?)null
            });
        }

        return Ok(new
        {
            brandName = profile.BrandName,
            brandTagline = profile.BrandTagline,
            brandColor = profile.BrandColor,
            logoUrl = profile.LogoUrl
        });
    }

    /// <summary>
    /// Update custom branding for the current user's agent profile
    /// </summary>
    [HttpPut("branding")]
    [RequireFeature(PlanFeature.CustomBranding)]
    public async Task<IActionResult> UpdateBranding(
        [FromBody] UpdateBrandingRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var profiles = await _unitOfWork.Repository<AgentProfile>()
            .FindAsync(p => p.UserId == userId && !p.IsDeleted, cancellationToken);
        var profile = profiles.FirstOrDefault();

        if (profile == null)
        {
            // Create a new agent profile with branding
            profile = new AgentProfile
            {
                UserId = userId,
                GreetingMessage = "Welcome! How can I help you today?",
                ClosingMessage = "Thank you for calling!",
                ServicesList = "",
                BusinessHours = "",
                VoicemailMessage = "",
                BrandName = request.BrandName,
                BrandTagline = request.BrandTagline,
                BrandColor = request.BrandColor != null && IsValidHexColor(request.BrandColor) ? request.BrandColor : null,
                LogoUrl = request.LogoUrl != null && IsValidLogoUrl(request.LogoUrl) ? request.LogoUrl : null
            };
            await _unitOfWork.Repository<AgentProfile>().AddAsync(profile, cancellationToken);
        }
        else
        {
            if (request.BrandName != null) profile.BrandName = request.BrandName;
            if (request.BrandTagline != null) profile.BrandTagline = request.BrandTagline;
            if (request.BrandColor != null)
            {
                if (!IsValidHexColor(request.BrandColor))
                    return BadRequest("BrandColor must be a valid hex color (e.g., #FF5733 or #F53).");
                profile.BrandColor = request.BrandColor;
            }
            if (request.LogoUrl != null)
            {
                if (!IsValidLogoUrl(request.LogoUrl))
                    return BadRequest("LogoUrl must be a valid HTTPS URL. Private/internal URLs are not allowed.");
                profile.LogoUrl = request.LogoUrl;
            }
            profile.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Repository<AgentProfile>().Update(profile);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated branding for user {UserId}", userId);

        return Ok(new
        {
            brandName = profile.BrandName,
            brandTagline = profile.BrandTagline,
            brandColor = profile.BrandColor,
            logoUrl = profile.LogoUrl
        });
    }

    /// <summary>
    /// Get agent profile settings (troubleshooting toggle, custom instructions, etc.)
    /// </summary>
    [HttpGet("settings")]
    public async Task<IActionResult> GetAgentSettings(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var profiles = await _unitOfWork.Repository<AgentProfile>()
            .FindAsync(p => p.UserId == userId && !p.IsDeleted, cancellationToken);
        var profile = profiles.FirstOrDefault();

        return Ok(new
        {
            enableTroubleshooting = profile?.EnableTroubleshooting ?? true,
            customInstructions = profile?.CustomInstructions,
            greetingMessage = profile?.GreetingMessage,
            closingMessage = profile?.ClosingMessage,
            tone = profile?.Tone ?? "professional",
            transferOnComplexIssues = profile?.TransferOnComplexIssues ?? true,
            humanBackupNumber = profile?.HumanBackupNumber
        });
    }

    /// <summary>
    /// Update agent profile settings (troubleshooting toggle, custom instructions, etc.)
    /// </summary>
    [HttpPut("settings")]
    public async Task<IActionResult> UpdateAgentSettings(
        [FromBody] UpdateAgentSettingsRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var profiles = await _unitOfWork.Repository<AgentProfile>()
            .FindAsync(p => p.UserId == userId && !p.IsDeleted, cancellationToken);
        var profile = profiles.FirstOrDefault();

        if (profile == null)
        {
            profile = new AgentProfile
            {
                UserId = userId,
                GreetingMessage = request.GreetingMessage ?? "Welcome! How can I help you today?",
                ClosingMessage = request.ClosingMessage ?? "Thank you for calling!",
                ServicesList = "",
                BusinessHours = "",
                VoicemailMessage = "",
                Tone = request.Tone ?? "professional",
                EnableTroubleshooting = request.EnableTroubleshooting ?? true,
                CustomInstructions = request.CustomInstructions,
                TransferOnComplexIssues = request.TransferOnComplexIssues ?? true,
                HumanBackupNumber = request.HumanBackupNumber
            };
            await _unitOfWork.Repository<AgentProfile>().AddAsync(profile, cancellationToken);
        }
        else
        {
            if (request.EnableTroubleshooting.HasValue) profile.EnableTroubleshooting = request.EnableTroubleshooting.Value;
            if (request.CustomInstructions != null) profile.CustomInstructions = request.CustomInstructions;
            if (request.GreetingMessage != null) profile.GreetingMessage = request.GreetingMessage;
            if (request.ClosingMessage != null) profile.ClosingMessage = request.ClosingMessage;
            if (request.Tone != null) profile.Tone = request.Tone;
            if (request.TransferOnComplexIssues.HasValue) profile.TransferOnComplexIssues = request.TransferOnComplexIssues.Value;
            if (request.HumanBackupNumber != null) profile.HumanBackupNumber = request.HumanBackupNumber;
            profile.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Repository<AgentProfile>().Update(profile);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated agent settings for user {UserId} (troubleshooting={Troubleshooting})",
            userId, profile.EnableTroubleshooting);

        // Build warning if troubleshooting is disabled
        string? warning = null;
        if (!profile.EnableTroubleshooting)
        {
            warning = "Troubleshooting assistance is now OFF. Your AI agent will focus on booking appointments instead of diagnosing issues. "
                    + "This may reduce caller engagement — callers who receive helpful advice are 3x more likely to book. "
                    + "The AI will still handle emergencies with safety instructions.";
        }

        return Ok(new
        {
            enableTroubleshooting = profile.EnableTroubleshooting,
            customInstructions = profile.CustomInstructions,
            greetingMessage = profile.GreetingMessage,
            closingMessage = profile.ClosingMessage,
            tone = profile.Tone,
            transferOnComplexIssues = profile.TransferOnComplexIssues,
            humanBackupNumber = profile.HumanBackupNumber,
            warning
        });
    }

    /// <summary>
    /// List available Google Cloud TTS voices
    /// </summary>
    [HttpGet("voices")]
    [AllowAnonymous]
    public IActionResult GetAvailableVoices()
    {
        var voices = new[]
        {
            new { name = "Kore", gender = "Male", description = "Professional, neutral (Default)" },
            new { name = "Puck", gender = "Male", description = "Friendly, conversational" },
            new { name = "Charon", gender = "Male", description = "Deep, authoritative" },
            new { name = "Fenrir", gender = "Male", description = "Warm, approachable" },
            new { name = "Aoede", gender = "Female", description = "Melodic, expressive" },
            new { name = "Leda", gender = "Female", description = "Gentle, caring" },
            new { name = "Orus", gender = "Male", description = "Clear, articulate" },
            new { name = "Zephyr", gender = "Neutral", description = "Light, energetic" }
        };

        return Ok(new { voices });
    }

    private string BuildDefaultSystemPrompt(string? businessName, string? industry)
    {
        var business = businessName ?? "our company";
        var ind = industry ?? "cleaning services";

        return $@"You are a professional AI voice receptionist for {business}, a {ind} company.

Your responsibilities:
- Answer calls warmly and professionally, introducing yourself as the receptionist for {business}
- Collect caller information (name, phone number, address)
- Understand the service they need
- Help schedule appointments based on available times
- Provide general information about services offered
- Transfer to a human agent when requested or when you cannot help

Communication style:
- Warm, professional, and empathetic
- Concise responses suitable for phone conversation (1-3 sentences)
- Ask one question at a time
- Confirm information by repeating it back
- Be patient with callers

Important rules:
- Never make up specific prices or guarantees
- If unsure about something, offer to have someone call them back
- Prioritize emergency situations (gas leaks, flooding, electrical hazards)
- Always provide a callback option if wait times are long";
    }

    private static bool IsValidHexColor(string color)
    {
        return System.Text.RegularExpressions.Regex.IsMatch(
            color, @"^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$");
    }

    private static bool IsValidLogoUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return false;

        if (uri.Scheme != "https")
            return false;

        // Resolve hostname to IP addresses and validate none are private/internal
        try
        {
            var addresses = Dns.GetHostAddresses(uri.Host);
            foreach (var addr in addresses)
            {
                if (IPAddress.IsLoopback(addr))
                    return false;

                if (addr.AddressFamily == AddressFamily.InterNetwork)
                {
                    var bytes = addr.GetAddressBytes();
                    // 10.0.0.0/8
                    if (bytes[0] == 10) return false;
                    // 172.16.0.0/12
                    if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) return false;
                    // 192.168.0.0/16
                    if (bytes[0] == 192 && bytes[1] == 168) return false;
                    // 169.254.0.0/16 (link-local, cloud metadata)
                    if (bytes[0] == 169 && bytes[1] == 254) return false;
                    // 127.0.0.0/8
                    if (bytes[0] == 127) return false;
                }
                else if (addr.AddressFamily == AddressFamily.InterNetworkV6)
                {
                    // Block IPv6 loopback, link-local, site-local, unique-local
                    if (addr.IsIPv6LinkLocal || addr.IsIPv6SiteLocal || IPAddress.IPv6Loopback.Equals(addr))
                        return false;
                    var bytes = addr.GetAddressBytes();
                    // fc00::/7 (Unique Local Addresses)
                    if (bytes[0] >= 0xfc && bytes[0] <= 0xfd)
                        return false;
                }
            }
        }
        catch
        {
            // DNS resolution failure — reject
            return false;
        }

        // Block suspicious hostnames
        var host = uri.Host.ToLowerInvariant();
        if (host.EndsWith(".local") || host.EndsWith(".internal"))
            return false;

        return true;
    }
}

public class CreateVoiceAgentRequest
{
    public string Name { get; set; } = "Default Agent";
    public string? SystemPrompt { get; set; }
    public string? VoiceName { get; set; }
    public string? Language { get; set; }
    public int? MaxCallDurationMinutes { get; set; }
    public string? BusinessName { get; set; }
    public string? Industry { get; set; }

    // Multi-location
    public string? LocationName { get; set; }
    public string? LocationAddress { get; set; }
    public string? LocationPhone { get; set; }
}

public class UpdateVoiceAgentRequest
{
    public string? Name { get; set; }
    public string? SystemPrompt { get; set; }
    public string? VoiceName { get; set; }
    public bool? IsActive { get; set; }

    // Multi-location
    public string? LocationName { get; set; }
    public string? LocationAddress { get; set; }
    public string? LocationPhone { get; set; }
}

public class OutboundCallRequest
{
    public string ToNumber { get; set; } = string.Empty;
    public string? FromNumber { get; set; }
}

public class UpdateBrandingRequest
{
    public string? BrandName { get; set; }
    public string? BrandTagline { get; set; }
    public string? BrandColor { get; set; }
    public string? LogoUrl { get; set; }
}

public class UpdateAgentSettingsRequest
{
    public bool? EnableTroubleshooting { get; set; }
    public string? CustomInstructions { get; set; }
    public string? GreetingMessage { get; set; }
    public string? ClosingMessage { get; set; }
    public string? Tone { get; set; }
    public bool? TransferOnComplexIssues { get; set; }
    public string? HumanBackupNumber { get; set; }
}
