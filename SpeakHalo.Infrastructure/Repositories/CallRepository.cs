using Microsoft.EntityFrameworkCore;
using SpeakHalo.Domain.Entities;
using SpeakHalo.Domain.Interfaces.Repositories;
using SpeakHalo.Infrastructure.Data;

namespace SpeakHalo.Infrastructure.Repositories;

public class CallRepository : Repository<Call>, ICallRepository
{
    public CallRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Call>> GetUserCallsAsync(string userId, int skip = 0, int take = 50, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AsNoTracking()
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<Call?> GetWithTranscriptAsync(string callId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(c => c.TranscriptEntries.OrderBy(t => t.SequenceNumber))
            .FirstOrDefaultAsync(c => c.Id == callId, cancellationToken);
    }
}
