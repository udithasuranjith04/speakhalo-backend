using Microsoft.EntityFrameworkCore;
using SpeakHalo.Domain.Entities;
using SpeakHalo.Domain.Interfaces.Repositories;
using SpeakHalo.Infrastructure.Data;

namespace SpeakHalo.Infrastructure.Repositories;

public class AITrainingExampleRepository : Repository<AITrainingExample>, IAITrainingExampleRepository
{
    public AITrainingExampleRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<List<AITrainingExample>> GetByCategory(
        string category,
        CancellationToken cancellationToken = default)
    {
        return await _context.AITrainingExamples
            .Where(e => e.Category == category && e.IncludeInTraining)
            .OrderByDescending(e => e.ImportanceWeight)
            .ThenByDescending(e => e.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<AITrainingExample>> GetSuccessfulExamplesAsync(
        int limit = 100,
        CancellationToken cancellationToken = default)
    {
        return await _context.AITrainingExamples
            .Where(e => e.WasSuccessful && e.IncludeInTraining && e.IsVerified)
            .OrderByDescending(e => e.CustomerSatisfactionScore)
            .ThenByDescending(e => e.ImportanceWeight)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<AITrainingExample>> GetForTrainingAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.AITrainingExamples
            .Where(e => e.IncludeInTraining && e.IsVerified)
            .OrderByDescending(e => e.ImportanceWeight)
            .ThenByDescending(e => e.CustomerSatisfactionScore)
            .ThenByDescending(e => e.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
