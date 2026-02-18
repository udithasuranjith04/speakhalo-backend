using Microsoft.EntityFrameworkCore;
using SpeakHalo.Domain.Entities;
using SpeakHalo.Domain.Enums;
using SpeakHalo.Domain.Interfaces.Repositories;
using SpeakHalo.Infrastructure.Data;

namespace SpeakHalo.Infrastructure.Repositories;

public class KnowledgeBaseArticleRepository : Repository<KnowledgeBaseArticle>, IKnowledgeBaseArticleRepository
{
    public KnowledgeBaseArticleRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<List<KnowledgeBaseArticle>> SearchAsync(
        string searchQuery,
        ServiceType? industry = null,
        string? category = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsNoTracking().Where(a => a.IsActive);

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            var lowerSearch = searchQuery.ToLower();
            query = query.Where(a =>
                a.Title.ToLower().Contains(lowerSearch) ||
                a.Content.ToLower().Contains(lowerSearch) ||
                a.Tags.Any(t => t.ToLower().Contains(lowerSearch)));
        }

        if (industry.HasValue)
        {
            query = query.Where(a => a.Industry == industry.Value);
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(a => a.Category == category);
        }

        return await query
            .OrderByDescending(a => a.UseCount)
            .ThenByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<KnowledgeBaseArticle>> GetByIndustryAsync(
        ServiceType industry,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(a => a.Industry == industry && a.IsActive)
            .OrderByDescending(a => a.UseCount)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<KnowledgeBaseArticle>> GetByEquipmentAsync(
        string equipmentType,
        string? brand = null,
        CancellationToken cancellationToken = default)
    {
        var lowerEquipment = equipmentType.ToLower();
        var query = _dbSet
            .AsNoTracking()
            .Where(a => a.IsActive &&
                        ((a.EquipmentType != null && a.EquipmentType.ToLower().Contains(lowerEquipment)) ||
                         a.Title.ToLower().Contains(lowerEquipment) ||
                         a.Content.ToLower().Contains(lowerEquipment)));

        if (!string.IsNullOrWhiteSpace(brand))
        {
            var lowerBrand = brand.ToLower();
            query = query.Where(a =>
                (a.EquipmentBrand != null && a.EquipmentBrand.ToLower().Contains(lowerBrand)) ||
                a.Title.ToLower().Contains(lowerBrand) ||
                a.Content.ToLower().Contains(lowerBrand));
        }

        return await query
            .OrderByDescending(a => a.UseCount)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<KnowledgeBaseArticle>> GetByTagsAsync(
        List<string> tags,
        CancellationToken cancellationToken = default)
    {
        if (tags == null || !tags.Any())
            return new List<KnowledgeBaseArticle>();

        var lowerTags = tags.Select(t => t.ToLower()).ToList();
        var query = _dbSet.AsNoTracking()
            .Where(a => a.IsActive && a.Tags.Any(t => lowerTags.Contains(t.ToLower())));

        return await query
            .OrderByDescending(a => a.UseCount)
            .ToListAsync(cancellationToken);
    }

    public async Task IncrementUseCountAsync(
        string articleId,
        CancellationToken cancellationToken = default)
    {
        var article = await _dbSet.FindAsync(new object[] { articleId }, cancellationToken);
        if (article != null)
        {
            article.UseCount++;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}

public class CommonIssueRepository : Repository<CommonIssue>, ICommonIssueRepository
{
    public CommonIssueRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<List<CommonIssue>> SearchByKeywordsAsync(
        List<string> keywords,
        ServiceType? industry = null,
        CancellationToken cancellationToken = default)
    {
        if (keywords == null || !keywords.Any())
            return new List<CommonIssue>();

        var lowerKeywords = keywords.Select(k => k.ToLower()).ToList();
        var query = _dbSet.AsNoTracking().Where(i => i.IsActive);

        if (industry.HasValue)
        {
            query = query.Where(i => i.Industry == industry.Value);
        }

        query = query.Where(i =>
            lowerKeywords.Any(k =>
                i.IssueName.ToLower().Contains(k) ||
                i.EquipmentType.ToLower().Contains(k) ||
                i.ProbableCause.ToLower().Contains(k) ||
                i.RecommendedSolution.ToLower().Contains(k) ||
                i.CustomerKeywords.Any(ck => ck.ToLower().Contains(k))));

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<List<CommonIssue>> GetByIndustryAsync(
        ServiceType industry,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(i => i.Industry == industry && i.IsActive)
            .OrderByDescending(i => i.OccurrenceCount)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<CommonIssue>> GetByEquipmentTypeAsync(
        string equipmentType,
        CancellationToken cancellationToken = default)
    {
        var lowerEquipment = equipmentType.ToLower();
        return await _dbSet
            .AsNoTracking()
            .Where(i => i.IsActive && i.EquipmentType.ToLower().Contains(lowerEquipment))
            .OrderByDescending(i => i.OccurrenceCount)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<CommonIssue>> GetMostFrequentAsync(
        ServiceType? industry = null,
        int limit = 10,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsNoTracking().Where(i => i.IsActive);

        if (industry.HasValue)
        {
            query = query.Where(i => i.Industry == industry.Value);
        }

        return await query
            .OrderByDescending(i => i.OccurrenceCount)
            .ThenByDescending(i => i.Priority)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task IncrementOccurrenceCountAsync(
        string issueId,
        CancellationToken cancellationToken = default)
    {
        var issue = await _dbSet.FindAsync(new object[] { issueId }, cancellationToken);
        if (issue != null)
        {
            issue.OccurrenceCount++;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}

