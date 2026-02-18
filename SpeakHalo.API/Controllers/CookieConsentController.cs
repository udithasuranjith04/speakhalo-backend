using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;
using SpeakHalo.Application.DTOs;
using SpeakHalo.Application.Services;
using SpeakHalo.Domain.Entities;
using SpeakHalo.Domain.Enums;
using SpeakHalo.Domain.Interfaces.Repositories;
using System.Security.Claims;

namespace SpeakHalo.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/cookies")]
[Authorize]
public class CookieConsentController : ControllerBase
{
    private readonly CookieManagementService _cookieService;
    private readonly ICookieDefinitionRepository _definitionRepo;
    private readonly ICookiePolicyRepository _policyRepo;
    private readonly IUserCookieConsentRepository _consentRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CookieConsentController> _logger;

    public CookieConsentController(
        CookieManagementService cookieService,
        ICookieDefinitionRepository definitionRepo,
        ICookiePolicyRepository policyRepo,
        IUserCookieConsentRepository consentRepo,
        IUnitOfWork unitOfWork,
        ILogger<CookieConsentController> logger)
    {
        _cookieService = cookieService;
        _definitionRepo = definitionRepo;
        _policyRepo = policyRepo;
        _consentRepo = consentRepo;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    [HttpGet("banner")]
    [AllowAnonymous]
    public async Task<IActionResult> GetConsentBanner(
        [FromQuery] string lang = "en",
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = User.Identity?.IsAuthenticated == true
                ? User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                : null;

            var bannerData = await _cookieService.GetConsentBannerDataAsync(userId, lang, cancellationToken);
            return Ok(bannerData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load consent banner");
            return StatusCode(500, new { message = "Failed to load consent banner", error = "An unexpected error occurred. Please try again." });
        }
    }

    [HttpGet("definitions")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCookieDefinitions(CancellationToken cancellationToken = default)
    {
        try
        {
            var definitions = await _cookieService.GetCookieDefinitionsByCategoryAsync(cancellationToken);

            var response = definitions.Select(cat => new
            {
                Category = cat.Key.ToString(),
                Cookies = cat.Value.Select(c => new CookieDefinitionDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    DisplayName = c.DisplayName,
                    Description = c.Description,
                    Category = c.Category.ToString(),
                    Domain = c.Domain,
                    ExpirationDays = c.ExpirationDays,
                    IsEssential = c.IsEssential,
                    Provider = c.Provider,
                    PrivacyPolicyUrl = c.PrivacyPolicyUrl
                }).ToList()
            });

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load cookie definitions");
            return StatusCode(500, new { message = "Failed to load cookie definitions", error = "An unexpected error occurred. Please try again." });
        }
    }

    [HttpGet("consent")]
    [AllowAnonymous]
    public async Task<IActionResult> GetUserConsents(CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = User.Identity?.IsAuthenticated == true
                ? User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                : null;

            var consents = await _cookieService.GetUserConsentsAsync(userId, cancellationToken);

            var response = new UserConsentsResponse
            {
                Essential = consents.GetValueOrDefault(CookieCategory.Essential, true),
                Functional = consents.GetValueOrDefault(CookieCategory.Functional, false),
                Analytics = consents.GetValueOrDefault(CookieCategory.Analytics, false),
                Performance = consents.GetValueOrDefault(CookieCategory.Performance, false),
                Marketing = consents.GetValueOrDefault(CookieCategory.Marketing, false),
                Advertising = consents.GetValueOrDefault(CookieCategory.Advertising, false),
                SocialMedia = consents.GetValueOrDefault(CookieCategory.SocialMedia, false),
                LastUpdated = DateTime.UtcNow,
                ConsentToken = userId == null ? _cookieService.GetOrCreateConsentToken() : null
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load user consents");
            return StatusCode(500, new { message = "Failed to load consents", error = "An unexpected error occurred. Please try again." });
        }
    }

    [HttpPost("consent")]
    [AllowAnonymous]
    public async Task<IActionResult> SaveConsent(
        [FromBody] SaveConsentRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = User.Identity?.IsAuthenticated == true
                ? User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                : null;

            var consents = new Dictionary<CookieCategory, bool>
            {
                { CookieCategory.Essential, true }, // Always true
                { CookieCategory.Functional, request.Functional },
                { CookieCategory.Analytics, request.Analytics },
                { CookieCategory.Performance, request.Performance },
                { CookieCategory.Marketing, request.Marketing },
                { CookieCategory.Advertising, request.Advertising },
                { CookieCategory.SocialMedia, request.SocialMedia }
            };

            var success = await _cookieService.SaveConsentAsync(userId, consents, cancellationToken);

            if (success)
            {
                return Ok(new
                {
                    message = "Consent preferences saved successfully",
                    consents = consents.ToDictionary(c => c.Key.ToString(), c => c.Value)
                });
            }

            return BadRequest(new { message = "Failed to save consent preferences" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save user consents");
            return StatusCode(500, new { message = "Failed to save consents", error = "An unexpected error occurred. Please try again." });
        }
    }

    [HttpGet("consent/{category}")]
    [AllowAnonymous]
    public async Task<IActionResult> CheckConsent(
        string category,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!Enum.TryParse<CookieCategory>(category, true, out var cookieCategory))
            {
                return BadRequest(new { message = "Invalid category" });
            }

            var userId = User.Identity?.IsAuthenticated == true
                ? User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                : null;

            var hasConsent = await _cookieService.HasConsentAsync(userId, cookieCategory, cancellationToken);

            return Ok(new { category, hasConsent });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check consent for category {Category}", category);
            return StatusCode(500, new { message = "Failed to check consent", error = "An unexpected error occurred. Please try again." });
        }
    }

    [HttpDelete("consent")]
    [AllowAnonymous]
    public async Task<IActionResult> WithdrawConsent(
        [FromBody] WithdrawConsentRequest? request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = User.Identity?.IsAuthenticated == true
                ? User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                : null;

            var success = await _cookieService.WithdrawAllConsentsAsync(
                userId,
                request?.Reason,
                cancellationToken);

            if (success)
            {
                return Ok(new { message = "All consents withdrawn successfully" });
            }

            return BadRequest(new { message = "Failed to withdraw consents" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to withdraw consents");
            return StatusCode(500, new { message = "Failed to withdraw consents", error = "An unexpected error occurred. Please try again." });
        }
    }

    [HttpGet("policy")]
    [AllowAnonymous]
    public async Task<IActionResult> GetActivePolicy(
        [FromQuery] string lang = "en",
        CancellationToken cancellationToken = default)
    {
        try
        {
            var policy = await _cookieService.GetActivePolicyAsync(lang, cancellationToken);

            if (policy == null)
            {
                return NotFound(new { message = "No active policy found" });
            }

            var response = new CookiePolicyDto
            {
                Id = policy.Id,
                Version = policy.Version,
                Title = policy.Title,
                Content = policy.Content,
                Summary = policy.Summary,
                EffectiveDate = policy.EffectiveDate,
                Language = policy.Language,
                PolicyUrl = policy.PolicyUrl
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load active cookie policy");
            return StatusCode(500, new { message = "Failed to load policy", error = "An unexpected error occurred. Please try again." });
        }
    }

    [HttpGet("policy/{policyVersion}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPolicyByVersion(
        string policyVersion,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var policy = await _policyRepo.GetByVersionAsync(policyVersion, cancellationToken);

            if (policy == null)
            {
                return NotFound(new { message = "Policy version not found" });
            }

            var response = new CookiePolicyDto
            {
                Id = policy.Id,
                Version = policy.Version,
                Title = policy.Title,
                Content = policy.Content,
                Summary = policy.Summary,
                EffectiveDate = policy.EffectiveDate,
                Language = policy.Language,
                PolicyUrl = policy.PolicyUrl
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load policy version {PolicyVersion}", policyVersion);
            return StatusCode(500, new { message = "Failed to load policy", error = "An unexpected error occurred. Please try again." });
        }
    }

    [HttpGet("policy/history")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPolicyHistory(
        [FromQuery] string lang = "en",
        CancellationToken cancellationToken = default)
    {
        try
        {
            var policies = await _policyRepo.GetPolicyHistoryAsync(lang, cancellationToken);

            var response = policies.Select(p => new CookiePolicyDto
            {
                Id = p.Id,
                Version = p.Version,
                Title = p.Title,
                Content = p.Content,
                Summary = p.Summary,
                EffectiveDate = p.EffectiveDate,
                Language = p.Language,
                PolicyUrl = p.PolicyUrl
            });

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load policy history");
            return StatusCode(500, new { message = "Failed to load policy history", error = "An unexpected error occurred. Please try again." });
        }
    }

    [HttpPost("admin/definitions")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateCookieDefinition(
        [FromBody] CreateCookieDefinitionRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var definition = new CookieDefinition
            {
                Name = request.Name,
                DisplayName = request.DisplayName,
                Description = request.Description,
                Category = request.Category,
                Domain = request.Domain,
                Path = request.Path,
                ExpirationDays = request.ExpirationDays,
                IsHttpOnly = request.IsHttpOnly,
                IsSecure = request.IsSecure,
                SameSite = request.SameSite,
                IsActive = request.IsActive,
                IsEssential = request.IsEssential,
                Provider = request.Provider,
                PrivacyPolicyUrl = request.PrivacyPolicyUrl,
                DisplayOrder = request.DisplayOrder
            };

            await _definitionRepo.AddAsync(definition, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return CreatedAtAction(nameof(GetCookieDefinitions), new { id = definition.Id }, definition);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create cookie definition {Name}", request.Name);
            return StatusCode(500, new { message = "Failed to create cookie definition", error = "An unexpected error occurred. Please try again." });
        }
    }

    [HttpPost("admin/policies")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateCookiePolicy(
        [FromBody] CreateCookiePolicyRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var policy = new CookiePolicy
            {
                Version = request.Version,
                Title = request.Title,
                Content = request.Content,
                Summary = request.Summary,
                EffectiveDate = request.EffectiveDate,
                RequiresConsent = request.RequiresConsent,
                Language = request.Language,
                PolicyUrl = request.PolicyUrl,
                ChangeNotes = request.ChangeNotes,
                PublishedBy = User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                PublishedAt = DateTime.UtcNow
            };

            await _policyRepo.AddAsync(policy, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return CreatedAtAction(nameof(GetPolicyByVersion), new { policyVersion = policy.Version }, policy);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create cookie policy version {Version}", request.Version);
            return StatusCode(500, new { message = "Failed to create cookie policy", error = "An unexpected error occurred. Please try again." });
        }
    }

    [HttpPut("admin/policies/{policyId}/activate")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ActivatePolicy(
        string policyId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _policyRepo.SetActivePolicyAsync(policyId, cancellationToken);
            return Ok(new { message = "Policy activated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to activate policy {PolicyId}", policyId);
            return StatusCode(500, new { message = "Failed to activate policy", error = "An unexpected error occurred. Please try again." });
        }
    }

    [HttpGet("admin/statistics")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetConsentStatistics(CancellationToken cancellationToken = default)
    {
        try
        {
            var allConsents = await _consentRepo.GetAllAsync(cancellationToken);

            var stats = allConsents
                .Where(c => c.IsActive)
                .GroupBy(c => c.Category)
                .Select(g => new ConsentStatisticsResponse
                {
                    Category = g.Key.ToString(),
                    TotalConsents = g.Count(),
                    Accepted = g.Count(c => c.Consented),
                    Declined = g.Count(c => !c.Consented),
                    AcceptanceRate = g.Any() ? (double)g.Count(c => c.Consented) / g.Count() * 100 : 0
                })
                .ToList();

            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load consent statistics");
            return StatusCode(500, new { message = "Failed to load statistics", error = "An unexpected error occurred. Please try again." });
        }
    }
}
