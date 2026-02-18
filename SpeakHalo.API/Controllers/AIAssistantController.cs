using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpeakHalo.Application.DTOs;
using SpeakHalo.Application.Interfaces;
using SpeakHalo.Application.Services;
using SpeakHalo.Domain.Entities;
using SpeakHalo.API.Filters;
using SpeakHalo.Domain.Enums;
using SpeakHalo.Domain.Interfaces.Repositories;
using System.Security.Claims;

namespace SpeakHalo.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/ai-assistant")]
[Authorize]
[RequireFeature(PlanFeature.VoiceAI)]
public class AIAssistantController : ControllerBase
{
    private readonly IAIAssistantService _aiAssistantService;
    private readonly IUsageTrackingService _usageTracking;
    private readonly ILogger<AIAssistantController> _logger;
    private readonly IUnitOfWork _unitOfWork;

    public AIAssistantController(
        IAIAssistantService aiAssistantService,
        IUsageTrackingService usageTracking,
        ILogger<AIAssistantController> logger,
        IUnitOfWork unitOfWork)
    {
        _aiAssistantService = aiAssistantService;
        _usageTracking = usageTracking;
        _logger = logger;
        _unitOfWork = unitOfWork;
    }

    private async Task<bool> UserOwnsContextAsync(string contextId, string userId)
    {
        var items = await _unitOfWork.Repository<AIConversationContext>().FindAsync(c => c.Id == contextId && c.UserId == userId);
        return items.Any();
    }

    [HttpPost("chat")]
    [ProducesResponseType(typeof(AIResponse), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> GenerateResponse(
        [FromBody] AIRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        // Override UserId from claims to prevent IDOR
        var securedRequest = request with { UserId = userId };

        var result = await _aiAssistantService.GenerateResponseAsync(securedRequest, cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }

        await _usageTracking.TrackAIMinutesAsync(userId, 1);

        return Ok(result);
    }

    [HttpGet("context/{conversationId}")]
    [ProducesResponseType(typeof(AIConversationContextResponse), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> GetContext(
        string conversationId,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var result = await _aiAssistantService.GetOrCreateContextAsync(
            conversationId,
            userId,
            cancellationToken
        );

        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpPatch("context/{contextId}")]
    [ProducesResponseType(typeof(bool), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdateContext(
        string contextId,
        [FromBody] UpdateContextRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        if (!await UserOwnsContextAsync(contextId, userId))
            return NotFound(new { message = "Context not found." });

        var result = await _aiAssistantService.UpdateContextAsync(
            contextId,
            request,
            cancellationToken
        );

        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpGet("knowledge/search")]
    [ProducesResponseType(typeof(List<ProductKnowledgeResponse>), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> SearchKnowledge(
        [FromQuery] string query,
        CancellationToken cancellationToken)
    {
        var result = await _aiAssistantService.SearchKnowledgeAsync(query, cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }

        var responses = result.Data?.Select(k => new ProductKnowledgeResponse
        {
            Id = k.Id,
            Topic = k.Topic,
            Category = k.Category,
            Question = k.Question,
            Answer = k.Answer,
            ShortAnswer = k.ShortAnswer,
            Keywords = k.Keywords,
            RelatedTopics = k.RelatedTopics,
            Importance = k.Importance,
            IsCommonQuestion = k.IsCommonQuestion,
            TimesReferenced = k.TimesReferenced
        }).ToList() ?? new List<ProductKnowledgeResponse>();

        return Ok(responses);
    }

    [HttpPost("training")]
    [ProducesResponseType(typeof(AITrainingExampleResponse), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> RecordTraining(
        [FromBody] RecordTrainingRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _aiAssistantService.RecordTrainingExampleAsync(
            request,
            cancellationToken
        );

        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }

        var response = new AITrainingExampleResponse
        {
            Id = result.Data!.Id,
            UserMessage = result.Data.UserMessage,
            ExpectedResponse = result.Data.ExpectedResponse,
            ActualResponse = result.Data.ActualResponse,
            Category = result.Data.Category,
            Intent = result.Data.Intent,
            WasSuccessful = result.Data.WasSuccessful,
            CustomerSatisfactionScore = result.Data.CustomerSatisfactionScore,
            IsVerified = result.Data.IsVerified,
            CreatedAt = result.Data.CreatedAt
        };

        return CreatedAtAction(
            nameof(RecordTraining),
            new { id = response.Id },
            response
        );
    }

    [HttpGet("knowledge/common-questions")]
    [AllowAnonymous] // Allow public access for FAQ pages
    [ProducesResponseType(typeof(List<ProductKnowledgeResponse>), 200)]
    public async Task<IActionResult> GetCommonQuestions(
        [FromQuery] int limit = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _aiAssistantService.SearchKnowledgeAsync("", cancellationToken);

            if (!result.IsSuccess || result.Data == null)
            {
                return Ok(new List<ProductKnowledgeResponse>());
            }

            var responses = result.Data
                .Where(k => k.IsCommonQuestion)
                .OrderByDescending(k => k.Importance)
                .Take(limit)
                .Select(k => new ProductKnowledgeResponse
                {
                    Id = k.Id,
                    Topic = k.Topic,
                    Category = k.Category,
                    Question = k.Question,
                    Answer = k.Answer,
                    ShortAnswer = k.ShortAnswer,
                    Keywords = k.Keywords,
                    RelatedTopics = k.RelatedTopics,
                    Importance = k.Importance,
                    IsCommonQuestion = k.IsCommonQuestion,
                    TimesReferenced = k.TimesReferenced
                })
                .ToList();

            return Ok(responses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get common questions");
            return Ok(new List<ProductKnowledgeResponse>());
        }
    }
}
