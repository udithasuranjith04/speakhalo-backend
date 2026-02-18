using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpeakHalo.API.Filters;
using SpeakHalo.Application.DTOs;
using SpeakHalo.Application.Interfaces;
using SpeakHalo.Application.Services;
using SpeakHalo.Domain.Enums;

namespace SpeakHalo.API.Controllers;

/// <summary>
/// Knowledge Base API
/// Manages industry-specific technical knowledge for AI training and technician reference
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/knowledge")]
[Authorize]
[RequireFeature(PlanFeature.KnowledgeBase)]
public class KnowledgeBaseController : ControllerBase
{
    private readonly IKnowledgeBaseService _knowledgeBaseService;

    public KnowledgeBaseController(IKnowledgeBaseService knowledgeBaseService)
    {
        _knowledgeBaseService = knowledgeBaseService;
    }

    /// <summary>
    /// Create a new knowledge base article (Admin only)
    /// </summary>
    [HttpPost("articles")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(KnowledgeArticleResponse), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> CreateArticle(
        [FromBody] CreateKnowledgeArticleRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _knowledgeBaseService.CreateArticleAsync(request, cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }

        return CreatedAtAction(
            nameof(GetArticle),
            new { articleId = result.Data?.Id },
            result
        );
    }

    /// <summary>
    /// Get a specific knowledge base article
    /// </summary>
    [HttpGet("articles/{articleId}")]
    [ProducesResponseType(typeof(KnowledgeArticleResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetArticle(
        string articleId,
        CancellationToken cancellationToken)
    {
        var result = await _knowledgeBaseService.GetArticleAsync(articleId, cancellationToken);

        if (!result.IsSuccess)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Search knowledge base articles
    /// </summary>
    [HttpGet("articles/search")]
    [ProducesResponseType(typeof(List<KnowledgeArticleResponse>), 200)]
    public async Task<IActionResult> SearchArticles(
        [FromQuery] string query,
        [FromQuery] ServiceType? industry = null,
        [FromQuery] string? category = null,
        CancellationToken cancellationToken = default)
    {
        var searchRequest = new KnowledgeSearchRequest
        {
            SearchQuery = query,
            Industry = industry ?? ServiceType.Cleaning,
            Category = category
        };

        var result = await _knowledgeBaseService.SearchArticlesAsync(searchRequest, cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Update an existing knowledge base article (Admin only)
    /// </summary>
    [HttpPut("articles/{articleId}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(KnowledgeArticleResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdateArticle(
        string articleId,
        [FromBody] UpdateKnowledgeArticleRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _knowledgeBaseService.UpdateArticleAsync(articleId, request, cancellationToken);

        if (!result.IsSuccess)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Delete a knowledge base article (soft delete, Admin only)
    /// </summary>
    [HttpDelete("articles/{articleId}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> DeleteArticle(
        string articleId,
        CancellationToken cancellationToken)
    {
        var result = await _knowledgeBaseService.DeleteArticleAsync(articleId, cancellationToken);

        if (!result.IsSuccess)
        {
            return NotFound(result);
        }

        return NoContent();
    }

    /// <summary>
    /// Get AI recommendations for a specific issue
    /// Useful for technicians and AI assistants to find relevant knowledge
    /// </summary>
    [HttpPost("recommendations")]
    [ProducesResponseType(typeof(List<KnowledgeRecommendation>), 200)]
    public async Task<IActionResult> GetRecommendations(
        [FromQuery] string issueDescription,
        [FromQuery] ServiceType industry = ServiceType.Cleaning,
        [FromQuery] string? equipmentType = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _knowledgeBaseService.GetRecommendationsForIssueAsync(
            issueDescription,
            industry,
            equipmentType,
            cancellationToken
        );

        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Find similar issues based on customer description
    /// Helps identify patterns and common problems
    /// </summary>
    [HttpPost("similar-issues")]
    [ProducesResponseType(typeof(List<CommonIssueResponse>), 200)]
    public async Task<IActionResult> FindSimilarIssues(
        [FromQuery] string customerDescription,
        [FromQuery] ServiceType industry = ServiceType.Cleaning,
        CancellationToken cancellationToken = default)
    {
        var result = await _knowledgeBaseService.FindSimilarIssuesAsync(
            customerDescription,
            industry,
            cancellationToken
        );

        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Export training data for AI fine-tuning
    /// Admin only endpoint for exporting knowledge base to train AI models
    /// </summary>
    [HttpGet("export/training-data")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(KnowledgeBaseExport), 200)]
    public async Task<IActionResult> ExportTrainingData(
        [FromQuery] ServiceType industry = ServiceType.Cleaning,
        CancellationToken cancellationToken = default)
    {
        var result = await _knowledgeBaseService.ExportTrainingDataAsync(
            industry,
            cancellationToken
        );

        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }
}
