using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpeakHalo.Domain.Entities;
using SpeakHalo.Domain.Interfaces.Repositories;
using System.Text.Json;

namespace SpeakHalo.API.Controllers;

/// <summary>
/// Admin read-only endpoints for viewing pricing plans.
/// Plans are defined in code (DatabaseSeeder) and Paddle IDs are configured via environment / admin settings.
/// Admins cannot create, update, or delete plans through this API.
/// </summary>
[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/v1/admin/pricing-plans")]
public class AdminPricingPlansController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public AdminPricingPlansController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
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

    private static object MapPlanResponse(PricingPlan plan)
    {
        object? enabledFeatures = null;
        if (!string.IsNullOrEmpty(plan.EnabledFeatures))
        {
            try { enabledFeatures = JsonSerializer.Deserialize<JsonElement>(plan.EnabledFeatures); }
            catch { enabledFeatures = plan.EnabledFeatures; }
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
            plan.PaddleProductId,
            plan.PaddlePriceId,
            plan.TierSlug,
            plan.AIOverageRate,
            plan.SMSOverageRate,
            plan.WhatsAppOverageRate,
            plan.ImageOverageRate,
            plan.CreatedAt,
            plan.UpdatedAt
        };
    }
}
