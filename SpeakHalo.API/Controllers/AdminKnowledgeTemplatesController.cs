using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpeakHalo.Application.DTOs;
using SpeakHalo.Application.Interfaces;
using SpeakHalo.Domain.Entities;
using SpeakHalo.Domain.Enums;
using SpeakHalo.Domain.Interfaces.Repositories;
using System.Security.Claims;
using System.Text;

namespace SpeakHalo.API.Controllers;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/v1/admin/knowledge-templates")]
public class AdminKnowledgeTemplatesController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cacheService;
    private readonly ILogger<AdminKnowledgeTemplatesController> _logger;

    public AdminKnowledgeTemplatesController(
        IUnitOfWork unitOfWork,
        ICacheService cacheService,
        ILogger<AdminKnowledgeTemplatesController> logger)
    {
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? industry = null,
        [FromQuery] string? category = null,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        var templates = await _unitOfWork.Repository<IndustryKnowledgeTemplate>()
            .FindAsync(t => !t.IsDeleted, ct);

        IEnumerable<IndustryKnowledgeTemplate> filtered = templates;

        if (!string.IsNullOrWhiteSpace(industry) && Enum.TryParse<ServiceType>(industry, true, out var industryEnum))
            filtered = filtered.Where(t => t.Industry == industryEnum);

        if (!string.IsNullOrWhiteSpace(category))
            filtered = filtered.Where(t => t.Category.Equals(category, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(search))
            filtered = filtered.Where(t =>
                t.Title.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                t.Content.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                t.Category.Contains(search, StringComparison.OrdinalIgnoreCase));

        var result = filtered
            .OrderBy(t => t.Industry)
            .ThenBy(t => t.SortOrder)
            .ThenBy(t => t.Category)
            .Select(MapToResponse)
            .ToList();

        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken ct = default)
    {
        var template = await _unitOfWork.Repository<IndustryKnowledgeTemplate>()
            .GetByIdAsync(id, ct);

        if (template == null || template.IsDeleted)
            return NotFound(new { error = "Template not found" });

        return Ok(MapToResponse(template));
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateKnowledgeTemplateRequest request,
        CancellationToken ct = default)
    {
        var adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(adminId))
            return Unauthorized();

        if (!Enum.TryParse<ServiceType>(request.Industry, true, out var industryEnum))
            return BadRequest(new { error = "Invalid industry value" });

        var template = new IndustryKnowledgeTemplate
        {
            Industry = industryEnum,
            Category = request.Category.Trim(),
            Title = request.Title.Trim(),
            Content = request.Content.Trim(),
            SortOrder = request.SortOrder,
            IsActive = true,
            LastEditedBy = adminId,
            LastEditedAt = DateTime.UtcNow,
            Version = 1
        };

        await _unitOfWork.Repository<IndustryKnowledgeTemplate>().AddAsync(template, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        await InvalidateCacheAsync(industryEnum, ct);

        _logger.LogInformation("Admin {AdminId} created knowledge template {TemplateId} for {Industry}",
            adminId, template.Id, industryEnum);

        return Ok(MapToResponse(template));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(
        string id,
        [FromBody] UpdateKnowledgeTemplateRequest request,
        CancellationToken ct = default)
    {
        var adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(adminId))
            return Unauthorized();

        var template = await _unitOfWork.Repository<IndustryKnowledgeTemplate>()
            .GetByIdAsync(id, ct);

        if (template == null || template.IsDeleted)
            return NotFound(new { error = "Template not found" });

        if (request.Category != null) template.Category = request.Category.Trim();
        if (request.Title != null) template.Title = request.Title.Trim();
        if (request.Content != null) template.Content = request.Content.Trim();
        if (request.SortOrder.HasValue) template.SortOrder = request.SortOrder.Value;
        if (request.IsActive.HasValue) template.IsActive = request.IsActive.Value;

        template.Version++;
        template.LastEditedBy = adminId;
        template.LastEditedAt = DateTime.UtcNow;

        _unitOfWork.Repository<IndustryKnowledgeTemplate>().Update(template);
        await _unitOfWork.SaveChangesAsync(ct);

        await InvalidateCacheAsync(template.Industry, ct);

        _logger.LogInformation("Admin {AdminId} updated knowledge template {TemplateId} (v{Version})",
            adminId, id, template.Version);

        return Ok(MapToResponse(template));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken ct = default)
    {
        var adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(adminId))
            return Unauthorized();

        var template = await _unitOfWork.Repository<IndustryKnowledgeTemplate>()
            .GetByIdAsync(id, ct);

        if (template == null || template.IsDeleted)
            return NotFound(new { error = "Template not found" });

        template.IsDeleted = true;
        template.DeletedAt = DateTime.UtcNow;
        template.DeletedBy = adminId;

        _unitOfWork.Repository<IndustryKnowledgeTemplate>().Update(template);
        await _unitOfWork.SaveChangesAsync(ct);

        await InvalidateCacheAsync(template.Industry, ct);

        _logger.LogInformation("Admin {AdminId} deleted knowledge template {TemplateId}", adminId, id);

        return Ok(new { message = "Template deleted successfully" });
    }

    [HttpPut("{id}/toggle")]
    public async Task<IActionResult> Toggle(string id, CancellationToken ct = default)
    {
        var adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(adminId))
            return Unauthorized();

        var template = await _unitOfWork.Repository<IndustryKnowledgeTemplate>()
            .GetByIdAsync(id, ct);

        if (template == null || template.IsDeleted)
            return NotFound(new { error = "Template not found" });

        template.IsActive = !template.IsActive;
        template.LastEditedBy = adminId;
        template.LastEditedAt = DateTime.UtcNow;

        _unitOfWork.Repository<IndustryKnowledgeTemplate>().Update(template);
        await _unitOfWork.SaveChangesAsync(ct);

        await InvalidateCacheAsync(template.Industry, ct);

        return Ok(new { message = $"Template {(template.IsActive ? "activated" : "deactivated")}", isActive = template.IsActive });
    }

    [HttpGet("preview/{industry}")]
    public async Task<IActionResult> Preview(string industry, CancellationToken ct = default)
    {
        if (!Enum.TryParse<ServiceType>(industry, true, out var industryEnum))
            return BadRequest(new { error = "Invalid industry value" });

        var templates = await _unitOfWork.Repository<IndustryKnowledgeTemplate>()
            .FindAsync(t => t.Industry == industryEnum && t.IsActive && !t.IsDeleted, ct);

        var ordered = templates.OrderBy(t => t.SortOrder).ThenBy(t => t.Category).ToList();

        if (ordered.Count == 0)
            return Ok(new { industry = industryEnum.ToString(), content = "", templateCount = 0 });

        var sb = new StringBuilder();
        string? currentCategory = null;
        foreach (var t in ordered)
        {
            if (t.Category != currentCategory)
            {
                currentCategory = t.Category;
                sb.AppendLine($"\n═══════════════════════════════════════════");
                sb.AppendLine(currentCategory.ToUpperInvariant());
                sb.AppendLine("═══════════════════════════════════════════");
            }
            sb.AppendLine(t.Content);
        }

        return Ok(new { industry = industryEnum.ToString(), content = sb.ToString(), templateCount = ordered.Count });
    }

    private async Task InvalidateCacheAsync(ServiceType industry, CancellationToken ct)
    {
        try
        {
            await _cacheService.RemoveAsync($"voice:expertise:{(int)industry}", ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to invalidate expertise cache for {Industry}", industry);
        }
    }

    private static KnowledgeTemplateResponse MapToResponse(IndustryKnowledgeTemplate t) => new()
    {
        Id = t.Id,
        Industry = t.Industry.ToString(),
        Category = t.Category,
        Title = t.Title,
        Content = t.Content,
        SortOrder = t.SortOrder,
        IsActive = t.IsActive,
        LastEditedBy = t.LastEditedBy,
        LastEditedAt = t.LastEditedAt,
        Version = t.Version,
        CreatedAt = t.CreatedAt,
        UpdatedAt = t.UpdatedAt
    };
}
