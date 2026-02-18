using Microsoft.EntityFrameworkCore;
using SpeakHalo.Domain.Entities;
using SpeakHalo.Domain.Interfaces.Repositories;
using SpeakHalo.Infrastructure.Data;

namespace SpeakHalo.Infrastructure.Repositories;

public class ProductKnowledgeRepository : Repository<ProductKnowledge>, IProductKnowledgeRepository
{
    public ProductKnowledgeRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<List<ProductKnowledge>> SearchAsync(
        string query,
        CancellationToken cancellationToken = default)
    {
        var lowerQuery = query.ToLower();

        return await _context.ProductKnowledge
            .Where(k => k.IsActive && k.IsCurrentVersion &&
                (k.Question.ToLower().Contains(lowerQuery) ||
                 k.Answer.ToLower().Contains(lowerQuery) ||
                 k.Topic.ToLower().Contains(lowerQuery) ||
                 k.Keywords.Any(kw => kw.ToLower().Contains(lowerQuery)) ||
                 k.AlternativeQuestions.Any(aq => aq.ToLower().Contains(lowerQuery))))
            .OrderByDescending(k => k.IsCommonQuestion)
            .ThenByDescending(k => k.Importance)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<ProductKnowledge>> GetByCategory(
        string category,
        CancellationToken cancellationToken = default)
    {
        return await _context.ProductKnowledge
            .Where(k => k.IsActive && k.IsCurrentVersion && k.Category == category)
            .OrderByDescending(k => k.Importance)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<ProductKnowledge>> GetCommonQuestionsAsync(
        int limit = 20,
        CancellationToken cancellationToken = default)
    {
        return await _context.ProductKnowledge
            .Where(k => k.IsActive && k.IsCurrentVersion && k.IsCommonQuestion)
            .OrderByDescending(k => k.Importance)
            .ThenByDescending(k => k.TimesReferenced)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task IncrementUsageCountAsync(
        string knowledgeId,
        CancellationToken cancellationToken = default)
    {
        var knowledge = await _context.ProductKnowledge
            .FirstOrDefaultAsync(k => k.Id == knowledgeId, cancellationToken);

        if (knowledge != null)
        {
            knowledge.TimesReferenced++;
            knowledge.LastUpdated = DateTime.UtcNow;
            _context.ProductKnowledge.Update(knowledge);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
