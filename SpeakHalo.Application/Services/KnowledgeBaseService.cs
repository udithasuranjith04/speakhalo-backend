using Microsoft.Extensions.Logging;
using SpeakHalo.Application.DTOs;
using SpeakHalo.Application.Interfaces;
using SpeakHalo.Domain.Entities;
using SpeakHalo.Domain.Enums;
using SpeakHalo.Domain.Interfaces.Repositories;
using SpeakHalo.Shared.Models;
using System.Text.Json;

namespace SpeakHalo.Application.Services;

/// <summary>
/// Knowledge Base Service
/// Manages industry-specific technical knowledge for AI training and reference
/// Provides intelligent search and recommendations based on context
/// </summary>
public interface IKnowledgeBaseService
{
    // Article management
    Task<Result<KnowledgeBaseArticle>> CreateArticleAsync(CreateKnowledgeArticleRequest request, CancellationToken cancellationToken = default);
    Task<Result<KnowledgeBaseArticle>> UpdateArticleAsync(string articleId, UpdateKnowledgeArticleRequest request, CancellationToken cancellationToken = default);
    Task<Result<bool>> DeleteArticleAsync(string articleId, CancellationToken cancellationToken = default);
    Task<Result<KnowledgeBaseArticle>> GetArticleAsync(string articleId, CancellationToken cancellationToken = default);
    Task<Result<List<KnowledgeBaseArticle>>> SearchArticlesAsync(KnowledgeSearchRequest request, CancellationToken cancellationToken = default);

    // AI-enhanced search
    Task<Result<List<KnowledgeRecommendation>>> GetRecommendationsForIssueAsync(
        string issueDescription,
        ServiceType industry,
        string? equipmentType = null,
        CancellationToken cancellationToken = default);

    Task<Result<List<CommonIssue>>> FindSimilarIssuesAsync(
        string customerDescription,
        ServiceType industry,
        CancellationToken cancellationToken = default);

    // Training data export
    Task<Result<KnowledgeBaseExport>> ExportTrainingDataAsync(
        ServiceType? industry = null,
        CancellationToken cancellationToken = default);
}

public class KnowledgeBaseService : IKnowledgeBaseService
{
    private readonly IKnowledgeBaseArticleRepository _articleRepository;
    private readonly ICommonIssueRepository _issueRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAiChatService _aiChatService;
    private readonly ILogger<KnowledgeBaseService> _logger;

    public KnowledgeBaseService(
        IKnowledgeBaseArticleRepository articleRepository,
        ICommonIssueRepository issueRepository,
        IUnitOfWork unitOfWork,
        IAiChatService aiChatService,
        ILogger<KnowledgeBaseService> logger)
    {
        _articleRepository = articleRepository;
        _issueRepository = issueRepository;
        _unitOfWork = unitOfWork;
        _aiChatService = aiChatService;
        _logger = logger;
    }

    public async Task<Result<KnowledgeBaseArticle>> CreateArticleAsync(
        CreateKnowledgeArticleRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var industry = ServiceType.HomeServices;
            if (!string.IsNullOrWhiteSpace(request.Industry))
                Enum.TryParse<ServiceType>(request.Industry, true, out industry);

            var article = new KnowledgeBaseArticle
            {
                Title = request.Title,
                Content = request.Content,
                Industry = industry,
                Category = request.Category,
                Tags = request.Tags,
                EquipmentType = request.EquipmentType,
                EquipmentBrand = request.EquipmentBrand,
                EquipmentModel = request.EquipmentModel,
                IssueType = request.IssueType,
                PartNumbers = request.PartNumbers != null ? JsonSerializer.Serialize(request.PartNumbers) : null,
                Symptoms = request.Symptoms,
                DiagnosisSteps = request.DiagnosisSteps,
                RepairSteps = request.RepairSteps,
                SafetyWarnings = request.SafetyWarnings,
                RequiredTools = request.RequiredTools != null ? JsonSerializer.Serialize(request.RequiredTools) : null,
                Complexity = request.Complexity,
                EstimatedTimeMinutes = request.EstimatedTimeMinutes,
                SkillLevel = request.SkillLevel,
                TypicalPartsCost = request.TypicalPartsCost,
                TypicalLaborCost = request.TypicalLaborCost,
                Author = request.Author,
                Source = request.Source,
                IsActive = true,
                PublishedAt = DateTime.UtcNow
            };

            await _articleRepository.AddAsync(article, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Created knowledge base article: {Title} for {Industry}", article.Title, article.Industry);

            return Result<KnowledgeBaseArticle>.Success(article);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create knowledge base article");
            return Result<KnowledgeBaseArticle>.Failure($"Failed to create article: {ex.Message}");
        }
    }

    public async Task<Result<KnowledgeBaseArticle>> UpdateArticleAsync(
        string articleId,
        UpdateKnowledgeArticleRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var article = await _articleRepository.GetByIdAsync(articleId, cancellationToken);
            if (article == null)
            {
                return Result<KnowledgeBaseArticle>.Failure("Article not found");
            }

            // Update fields
            if (request.Title != null) article.Title = request.Title;
            if (request.Content != null) article.Content = request.Content;
            if (request.Category != null) article.Category = request.Category;
            if (request.Tags != null) article.Tags = request.Tags;
            if (request.IsActive.HasValue) article.IsActive = request.IsActive.Value;

            _articleRepository.Update(article);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<KnowledgeBaseArticle>.Success(article);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update article {ArticleId}", articleId);
            return Result<KnowledgeBaseArticle>.Failure($"Failed to update article: {ex.Message}");
        }
    }

    public async Task<Result<bool>> DeleteArticleAsync(
        string articleId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var article = await _articleRepository.GetByIdAsync(articleId, cancellationToken);
            if (article == null)
            {
                return Result<bool>.Failure("Article not found");
            }

            // Soft delete
            article.IsActive = false;
            _articleRepository.Update(article);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete article {ArticleId}", articleId);
            return Result<bool>.Failure($"Failed to delete article: {ex.Message}");
        }
    }

    public async Task<Result<KnowledgeBaseArticle>> GetArticleAsync(
        string articleId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var article = await _articleRepository.GetByIdAsync(articleId, cancellationToken);
            if (article == null)
            {
                return Result<KnowledgeBaseArticle>.Failure("Article not found");
            }

            return Result<KnowledgeBaseArticle>.Success(article);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get article {ArticleId}", articleId);
            return Result<KnowledgeBaseArticle>.Failure($"Failed to get article: {ex.Message}");
        }
    }

    public async Task<Result<List<KnowledgeBaseArticle>>> SearchArticlesAsync(
        KnowledgeSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var articles = await _articleRepository.SearchAsync(
                request.SearchQuery,
                request.Industry,
                request.Category,
                cancellationToken
            );

            return Result<List<KnowledgeBaseArticle>>.Success(articles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search articles");
            return Result<List<KnowledgeBaseArticle>>.Failure($"Search failed: {ex.Message}");
        }
    }

    public async Task<Result<List<KnowledgeRecommendation>>> GetRecommendationsForIssueAsync(
        string issueDescription,
        ServiceType industry,
        string? equipmentType = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Extract keywords from description using AI
            var keywords = await ExtractKeywordsAsync(issueDescription, cancellationToken);

            // Search common issues
            var similarIssues = await _issueRepository.SearchByKeywordsAsync(
                keywords,
                industry,
                cancellationToken
            );

            // Search knowledge base articles
            var relevantArticles = await _articleRepository.GetByTagsAsync(
                keywords,
                cancellationToken
            );

            // Combine and rank recommendations
            var recommendations = new List<KnowledgeRecommendation>();

            foreach (var issue in similarIssues.Take(5))
            {
                recommendations.Add(new KnowledgeRecommendation
                {
                    Type = "CommonIssue",
                    Title = issue.IssueName,
                    Summary = issue.ProbableCause,
                    RelevanceScore = CalculateRelevanceScore(issueDescription, issue.CustomerKeywords),
                    Source = "Common Issues Database",
                    EquipmentType = issue.EquipmentType,
                    EstimatedCost = issue.AverageCost,
                    EstimatedTimeMinutes = issue.EstimatedRepairTimeMinutes,
                    Severity = issue.Severity
                });
            }

            foreach (var article in relevantArticles.Take(5))
            {
                recommendations.Add(new KnowledgeRecommendation
                {
                    Type = "Article",
                    Title = article.Title,
                    Summary = article.Content.Length > 200 ? article.Content.Substring(0, 200) + "..." : article.Content,
                    RelevanceScore = article.Relevance,
                    Source = article.Source ?? "Knowledge Base",
                    EquipmentType = article.EquipmentType,
                    EstimatedCost = (article.TypicalPartsCost ?? 0) + (article.TypicalLaborCost ?? 0),
                    EstimatedTimeMinutes = article.EstimatedTimeMinutes,
                    Severity = article.Complexity
                });
            }

            // Sort by relevance
            var sortedRecommendations = recommendations
                .OrderByDescending(r => r.RelevanceScore)
                .ToList();

            return Result<List<KnowledgeRecommendation>>.Success(sortedRecommendations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get recommendations for issue");
            return Result<List<KnowledgeRecommendation>>.Failure($"Failed to get recommendations: {ex.Message}");
        }
    }

    public async Task<Result<List<CommonIssue>>> FindSimilarIssuesAsync(
        string customerDescription,
        ServiceType industry,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var keywords = await ExtractKeywordsAsync(customerDescription, cancellationToken);

            var similarIssues = await _issueRepository.SearchByKeywordsAsync(
                keywords,
                industry,
                cancellationToken
            );

            // Increment occurrence count for matched issues
            foreach (var issue in similarIssues.Take(3))
            {
                await _issueRepository.IncrementOccurrenceCountAsync(issue.Id, cancellationToken);
            }

            return Result<List<CommonIssue>>.Success(similarIssues);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to find similar issues");
            return Result<List<CommonIssue>>.Failure($"Failed to find similar issues: {ex.Message}");
        }
    }

    public async Task<Result<KnowledgeBaseExport>> ExportTrainingDataAsync(
        ServiceType? industry = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var articles = industry.HasValue
                ? await _articleRepository.GetByIndustryAsync(industry.Value, cancellationToken)
                : await _articleRepository.GetAllAsync(cancellationToken);

            var issues = industry.HasValue
                ? await _issueRepository.GetByIndustryAsync(industry.Value, cancellationToken)
                : await _issueRepository.GetAllAsync(cancellationToken);

            var export = new KnowledgeBaseExport
            {
                TotalArticles = articles.Count(),
                TotalCommonIssues = issues.Count(),
                Industry = industry?.ToString(),
                ExportedAt = DateTime.UtcNow,
                Articles = articles.Select(a => new
                {
                    a.Title,
                    a.Content,
                    a.Industry,
                    a.Tags,
                    a.EquipmentType,
                    a.IssueType,
                    a.Complexity
                }).Cast<object>().ToList(),
                CommonIssues = issues.Select(i => new
                {
                    i.IssueName,
                    i.CommonSymptoms,
                    i.VisualIndicators,
                    i.ProbableCause,
                    i.RecommendedSolution,
                    i.Severity
                }).Cast<object>().ToList()
            };

            return Result<KnowledgeBaseExport>.Success(export);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export training data");
            return Result<KnowledgeBaseExport>.Failure($"Export failed: {ex.Message}");
        }
    }

    private async Task<List<string>> ExtractKeywordsAsync(string text, CancellationToken cancellationToken)
    {
        try
        {
            var systemPrompt = "You are a keyword extraction expert. Extract relevant technical keywords from service requests.";
            var userPrompt = $"Extract 5-10 technical keywords from this service request: \"{text}\". Return only the keywords as a comma-separated list.";

            var response = await _aiChatService.ChatCompletionAsync(systemPrompt, userPrompt, cancellationToken);

            if (response.IsSuccess && response.Data != null)
            {
                return response.Data
                    .Split(',')
                    .Select(k => k.Trim().ToLower())
                    .Where(k => !string.IsNullOrEmpty(k))
                    .ToList();
            }

            // Fallback: simple word extraction
            return text.ToLower()
                .Split(new[] { ' ', ',', '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length > 3)
                .Distinct()
                .ToList();
        }
        catch
        {
            // Fallback on error
            return text.ToLower()
                .Split(new[] { ' ', ',', '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length > 3)
                .Distinct()
                .ToList();
        }
    }

    private double CalculateRelevanceScore(string query, List<string> keywords)
    {
        var queryWords = query.ToLower().Split(' ').ToHashSet();
        var matchCount = keywords.Count(k => queryWords.Contains(k.ToLower()));
        return keywords.Any() ? (double)matchCount / keywords.Count : 0.0;
    }
}
