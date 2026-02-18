using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SpeakHalo.Application.DTOs;
using SpeakHalo.Domain.Enums;
using SpeakHalo.Infrastructure.Data;
using System.Text.Json;

namespace SpeakHalo.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class IndustriesController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<IndustriesController> _logger;

    public IndustriesController(ApplicationDbContext context, ILogger<IndustriesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get available industries (Cleaning only)
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetIndustries(CancellationToken cancellationToken)
    {
        try
        {
            var industries = await _context.Industries
                .Where(i => i.IsActive && i.ServiceType == ServiceType.Cleaning)
                .OrderBy(i => i.DisplayOrder)
                .ToListAsync(cancellationToken);

            // Then deserialize JSON in memory (not in expression tree)
            var industryDtos = industries.Select(i => new IndustryDto
            {
                Id = i.Id,
                ServiceType = i.ServiceType,
                Name = i.Name,
                Code = i.Code,
                Description = i.Description,
                Keywords = !string.IsNullOrEmpty(i.Keywords)
                    ? JsonSerializer.Deserialize<List<string>>(i.Keywords) ?? new List<string>()
                    : new List<string>(),
                CommonServices = !string.IsNullOrEmpty(i.CommonServices)
                    ? JsonSerializer.Deserialize<List<string>>(i.CommonServices) ?? new List<string>()
                    : new List<string>(),
                CommonEquipment = !string.IsNullOrEmpty(i.CommonEquipment)
                    ? JsonSerializer.Deserialize<List<string>>(i.CommonEquipment) ?? new List<string>()
                    : new List<string>(),
                AverageServiceDuration = i.AverageServiceDuration,
                TypicalPriceRange = i.TypicalPriceRange,
                DisplayOrder = i.DisplayOrder
            }).ToList();

            return Ok(new
            {
                isSuccess = true,
                data = industryDtos
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving industries");
            return StatusCode(500, new
            {
                isSuccess = false,
                errors = new[] { "Failed to retrieve industries" },
                message = "An error occurred while fetching industries"
            });
        }
    }

    /// <summary>
    /// Get industry by ID with full AI training metadata
    /// </summary>
    [HttpGet("{id}")]
    [Authorize]
    public async Task<IActionResult> GetIndustry(string id, CancellationToken cancellationToken)
    {
        try
        {
            var industry = await _context.Industries
                .Where(i => i.Id == id && i.IsActive)
                .FirstOrDefaultAsync(cancellationToken);

            if (industry == null)
            {
                return NotFound(new
                {
                    isSuccess = false,
                    errors = new[] { "Industry not found" },
                    message = "The requested industry does not exist"
                });
            }

            var dto = new
            {
                id = industry.Id,
                serviceType = industry.ServiceType,
                name = industry.Name,
                code = industry.Code,
                description = industry.Description,
                keywords = !string.IsNullOrEmpty(industry.Keywords)
                    ? JsonSerializer.Deserialize<List<string>>(industry.Keywords)
                    : new List<string>(),
                commonServices = !string.IsNullOrEmpty(industry.CommonServices)
                    ? JsonSerializer.Deserialize<List<string>>(industry.CommonServices)
                    : new List<string>(),
                commonEquipment = !string.IsNullOrEmpty(industry.CommonEquipment)
                    ? JsonSerializer.Deserialize<List<string>>(industry.CommonEquipment)
                    : new List<string>(),
                typicalIssues = !string.IsNullOrEmpty(industry.TypicalIssues)
                    ? JsonSerializer.Deserialize<List<string>>(industry.TypicalIssues)
                    : new List<string>(),
                technicalTerms = !string.IsNullOrEmpty(industry.TechnicalTerms)
                    ? JsonSerializer.Deserialize<List<string>>(industry.TechnicalTerms)
                    : new List<string>(),
                seasonalConsiderations = !string.IsNullOrEmpty(industry.SeasonalConsiderations)
                    ? JsonSerializer.Deserialize<List<string>>(industry.SeasonalConsiderations)
                    : new List<string>(),
                emergencyIndicators = !string.IsNullOrEmpty(industry.EmergencyIndicators)
                    ? JsonSerializer.Deserialize<List<string>>(industry.EmergencyIndicators)
                    : new List<string>(),
                averageServiceDuration = industry.AverageServiceDuration,
                typicalPriceRange = industry.TypicalPriceRange
            };

            return Ok(new
            {
                isSuccess = true,
                data = dto
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving industry {IndustryId}", id);
            return StatusCode(500, new
            {
                isSuccess = false,
                errors = new[] { "Failed to retrieve industry" },
                message = "An error occurred while fetching industry details"
            });
        }
    }
}
