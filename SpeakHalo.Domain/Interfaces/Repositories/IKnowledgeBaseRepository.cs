using SpeakHalo.Domain.Entities;
using SpeakHalo.Domain.Enums;

namespace SpeakHalo.Domain.Interfaces.Repositories;

public interface IKnowledgeBaseArticleRepository : IRepository<KnowledgeBaseArticle>
{
    Task<List<KnowledgeBaseArticle>> SearchAsync(
        string searchQuery,
        ServiceType? industry = null,
        string? category = null,
        CancellationToken cancellationToken = default);

    Task<List<KnowledgeBaseArticle>> GetByIndustryAsync(
        ServiceType industry,
        CancellationToken cancellationToken = default);

    Task<List<KnowledgeBaseArticle>> GetByEquipmentAsync(
        string equipmentType,
        string? brand = null,
        CancellationToken cancellationToken = default);

    Task<List<KnowledgeBaseArticle>> GetByTagsAsync(
        List<string> tags,
        CancellationToken cancellationToken = default);

    Task IncrementUseCountAsync(
        string articleId,
        CancellationToken cancellationToken = default);
}

public interface ICommonIssueRepository : IRepository<CommonIssue>
{
    Task<List<CommonIssue>> SearchByKeywordsAsync(
        List<string> keywords,
        ServiceType? industry = null,
        CancellationToken cancellationToken = default);

    Task<List<CommonIssue>> GetByIndustryAsync(
        ServiceType industry,
        CancellationToken cancellationToken = default);

    Task<List<CommonIssue>> GetByEquipmentTypeAsync(
        string equipmentType,
        CancellationToken cancellationToken = default);

    Task<List<CommonIssue>> GetMostFrequentAsync(
        ServiceType? industry = null,
        int limit = 10,
        CancellationToken cancellationToken = default);

    Task IncrementOccurrenceCountAsync(
        string issueId,
        CancellationToken cancellationToken = default);
}

