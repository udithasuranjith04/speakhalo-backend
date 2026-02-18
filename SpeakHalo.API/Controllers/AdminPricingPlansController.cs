using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpeakHalo.Application.Interfaces;
using SpeakHalo.Domain.Entities;
using SpeakHalo.Domain.Interfaces.Repositories;
using System.Security.Claims;
using System.Text.Json;

namespace SpeakHalo.API.Controllers;

/// <summary>
/// Admin endpoints for managing reusable pricing plans.
/// Plans define usage limits, trial days, and features that can be assigned to users during account creation.
/// </summary>
[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/v1/admin/pricing-plans")]
public class AdminPricingPlansController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AdminPricingPlansController> _logger;
    private readonly IAuditLogService _auditLogService;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public AdminPricingPlansController(
        IUnitOfWork unitOfWork,
        ILogger<AdminPricingPlansController> logger,
        IAuditLogService auditLogService)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _auditLogService = auditLogService;
    }

    [HttpGet]
    public async Task<IActionResult> ListPlans(CancellationToken cancellationToken = default)
    {
        var plans = await _unitOfWork.Repository<PricingPlan>()
            .FindAsync(p => !p.IsDeleted, cancellationToken);

        var result = plans.OrderBy(p => p.SortOrder).ThenBy(p => p.Name).Select(MapPlanResponse);

        return Ok(result);
    }

    [HttpGet("active")]
    public async Task<IActionResult> ListActivePlans(CancellationToken cancellationToken = default)
    {
        var plans = await _unitOfWork.Repository<PricingPlan>()
            .FindAsync(p => p.IsActive && !p.IsDeleted, cancellationToken);

        var result = plans.OrderBy(p => p.SortOrder).ThenBy(p => p.Name).Select(MapPlanResponse);

        return Ok(result);
    }

    [HttpGet("{planId}")]
    public async Task<IActionResult> GetPlan(string planId, CancellationToken cancellationToken = default)
    {
        var plan = await _unitOfWork.Repository<PricingPlan>()
            .GetByIdAsync(planId, cancellationToken);

        if (plan == null || plan.IsDeleted)
            return NotFound(new { error = "Pricing plan not found" });

        return Ok(MapPlanResponse(plan));
    }

    [HttpPost]
    public async Task<IActionResult> CreatePlan(
        [FromBody] CreatePricingPlanRequest request,
        CancellationToken cancellationToken = default)
    {
        var adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(adminId))
            return Unauthorized();

        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { error = "Plan name is required" });

        var existing = await _unitOfWork.Repository<PricingPlan>()
            .FindAsync(p => p.Name == request.Name && !p.IsDeleted, cancellationToken);
        if (existing.Any())
            return BadRequest(new { error = "A pricing plan with this name already exists" });

        var plan = new PricingPlan
        {
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            DisplayPrice = request.DisplayPrice,
            TrialDays = request.TrialDays,
            AIMinutesIncluded = request.AIMinutesIncluded,
            WhatsAppMessagesIncluded = request.WhatsAppMessagesIncluded,
            SMSMessagesIncluded = request.SMSMessagesIncluded,
            ImagesIncluded = request.ImagesIncluded,
            MaxPhoneNumbers = request.MaxPhoneNumbers,
            EnforceHardLimits = request.EnforceHardLimits,
            EnabledFeatures = request.EnabledFeatures != null
                ? JsonSerializer.Serialize(request.EnabledFeatures, JsonOptions)
                : null,
            IsActive = true,
            SortOrder = request.SortOrder
        };

        await _unitOfWork.Repository<PricingPlan>().AddAsync(plan, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Admin {AdminId} created pricing plan {PlanId} ({PlanName})",
            adminId, plan.Id, plan.Name);

        await _auditLogService.LogActionAsync(
            userId: adminId,
            action: "PRICING_PLAN_CREATED",
            entityType: "PricingPlan",
            entityId: plan.Id,
            oldValues: null,
            newValues: $"{{\"name\":\"{plan.Name}\",\"displayPrice\":{plan.DisplayPrice},\"trialDays\":{plan.TrialDays},\"aiMinutes\":{plan.AIMinutesIncluded}}}",
            ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
            userAgent: HttpContext.Request.Headers["User-Agent"].ToString());

        return Ok(new
        {
            message = "Pricing plan created successfully",
            plan = MapPlanResponse(plan)
        });
    }

    [HttpPut("{planId}")]
    public async Task<IActionResult> UpdatePlan(
        string planId,
        [FromBody] UpdatePricingPlanRequest request,
        CancellationToken cancellationToken = default)
    {
        var adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(adminId))
            return Unauthorized();

        var plan = await _unitOfWork.Repository<PricingPlan>()
            .GetByIdAsync(planId, cancellationToken);

        if (plan == null || plan.IsDeleted)
            return NotFound(new { error = "Pricing plan not found" });

        var oldName = plan.Name;

        if (request.Name != null && request.Name.Trim() != plan.Name)
        {
            var existing = await _unitOfWork.Repository<PricingPlan>()
                .FindAsync(p => p.Name == request.Name.Trim() && p.Id != planId && !p.IsDeleted, cancellationToken);
            if (existing.Any())
                return BadRequest(new { error = "A pricing plan with this name already exists" });
        }

        if (request.Name != null) plan.Name = request.Name.Trim();
        if (request.Description != null) plan.Description = request.Description.Trim();
        if (request.DisplayPrice.HasValue) plan.DisplayPrice = request.DisplayPrice.Value;
        if (request.TrialDays.HasValue) plan.TrialDays = request.TrialDays.Value;
        if (request.AIMinutesIncluded.HasValue) plan.AIMinutesIncluded = request.AIMinutesIncluded.Value;
        if (request.WhatsAppMessagesIncluded.HasValue) plan.WhatsAppMessagesIncluded = request.WhatsAppMessagesIncluded.Value;
        if (request.SMSMessagesIncluded.HasValue) plan.SMSMessagesIncluded = request.SMSMessagesIncluded.Value;
        if (request.ImagesIncluded.HasValue) plan.ImagesIncluded = request.ImagesIncluded.Value;
        if (request.MaxPhoneNumbers.HasValue) plan.MaxPhoneNumbers = request.MaxPhoneNumbers.Value;
        if (request.EnforceHardLimits.HasValue) plan.EnforceHardLimits = request.EnforceHardLimits.Value;
        if (request.EnabledFeatures != null)
            plan.EnabledFeatures = JsonSerializer.Serialize(request.EnabledFeatures, JsonOptions);
        if (request.SortOrder.HasValue) plan.SortOrder = request.SortOrder.Value;

        _unitOfWork.Repository<PricingPlan>().Update(plan);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Admin {AdminId} updated pricing plan {PlanId} ({PlanName})",
            adminId, plan.Id, plan.Name);

        await _auditLogService.LogActionAsync(
            userId: adminId,
            action: "PRICING_PLAN_UPDATED",
            entityType: "PricingPlan",
            entityId: plan.Id,
            oldValues: $"{{\"name\":\"{oldName}\"}}",
            newValues: $"{{\"name\":\"{plan.Name}\",\"displayPrice\":{plan.DisplayPrice},\"trialDays\":{plan.TrialDays},\"aiMinutes\":{plan.AIMinutesIncluded}}}",
            ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
            userAgent: HttpContext.Request.Headers["User-Agent"].ToString());

        return Ok(new
        {
            message = "Pricing plan updated successfully",
            plan = MapPlanResponse(plan)
        });
    }

    [HttpDelete("{planId}")]
    public async Task<IActionResult> DeletePlan(string planId, CancellationToken cancellationToken = default)
    {
        var adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(adminId))
            return Unauthorized();

        var plan = await _unitOfWork.Repository<PricingPlan>()
            .GetByIdAsync(planId, cancellationToken);

        if (plan == null || plan.IsDeleted)
            return NotFound(new { error = "Pricing plan not found" });

        plan.IsDeleted = true;
        plan.DeletedAt = DateTime.UtcNow;
        plan.DeletedBy = adminId;
        plan.IsActive = false;

        _unitOfWork.Repository<PricingPlan>().Update(plan);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Admin {AdminId} deleted pricing plan {PlanId} ({PlanName})",
            adminId, plan.Id, plan.Name);

        await _auditLogService.LogActionAsync(
            userId: adminId,
            action: "PRICING_PLAN_DELETED",
            entityType: "PricingPlan",
            entityId: plan.Id,
            oldValues: null,
            newValues: $"{{\"name\":\"{plan.Name}\",\"deletedBy\":\"{adminId}\"}}",
            ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
            userAgent: HttpContext.Request.Headers["User-Agent"].ToString());

        return Ok(new
        {
            message = "Pricing plan deleted successfully",
            planId = plan.Id,
            name = plan.Name
        });
    }

    [HttpPut("{planId}/toggle")]
    public async Task<IActionResult> TogglePlan(string planId, CancellationToken cancellationToken = default)
    {
        var adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(adminId))
            return Unauthorized();

        var plan = await _unitOfWork.Repository<PricingPlan>()
            .GetByIdAsync(planId, cancellationToken);

        if (plan == null || plan.IsDeleted)
            return NotFound(new { error = "Pricing plan not found" });

        plan.IsActive = !plan.IsActive;

        _unitOfWork.Repository<PricingPlan>().Update(plan);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var status = plan.IsActive ? "activated" : "deactivated";
        _logger.LogInformation("Admin {AdminId} {Status} pricing plan {PlanId} ({PlanName})",
            adminId, status, plan.Id, plan.Name);

        return Ok(new
        {
            message = $"Pricing plan {status} successfully",
            planId = plan.Id,
            name = plan.Name,
            isActive = plan.IsActive
        });
    }

    private static object MapPlanResponse(PricingPlan plan)
    {
        object? enabledFeatures = null;
        if (!string.IsNullOrEmpty(plan.EnabledFeatures))
        {
            try
            {
                enabledFeatures = JsonSerializer.Deserialize<JsonElement>(plan.EnabledFeatures);
            }
            catch
            {
                enabledFeatures = plan.EnabledFeatures;
            }
        }

        return new
        {
            plan.Id,
            plan.Name,
            plan.Description,
            plan.DisplayPrice,
            plan.TrialDays,
            plan.AIMinutesIncluded,
            plan.WhatsAppMessagesIncluded,
            plan.SMSMessagesIncluded,
            plan.ImagesIncluded,
            plan.MaxPhoneNumbers,
            plan.EnforceHardLimits,
            enabledFeatures,
            plan.IsActive,
            plan.SortOrder,
            plan.CreatedAt,
            plan.UpdatedAt
        };
    }
}

public class CreatePricingPlanRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal DisplayPrice { get; set; }
    public int TrialDays { get; set; } = 0;
    public int AIMinutesIncluded { get; set; }
    public int WhatsAppMessagesIncluded { get; set; }
    public int SMSMessagesIncluded { get; set; }
    public int ImagesIncluded { get; set; }
    public int MaxPhoneNumbers { get; set; } = 1;
    public bool EnforceHardLimits { get; set; } = true;
    public Dictionary<string, bool>? EnabledFeatures { get; set; }
    public int SortOrder { get; set; } = 0;
}

public class UpdatePricingPlanRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public decimal? DisplayPrice { get; set; }
    public int? TrialDays { get; set; }
    public int? AIMinutesIncluded { get; set; }
    public int? WhatsAppMessagesIncluded { get; set; }
    public int? SMSMessagesIncluded { get; set; }
    public int? ImagesIncluded { get; set; }
    public int? MaxPhoneNumbers { get; set; }
    public bool? EnforceHardLimits { get; set; }
    public Dictionary<string, bool>? EnabledFeatures { get; set; }
    public int? SortOrder { get; set; }
}
