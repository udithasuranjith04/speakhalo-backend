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

    public async Task<IEnumerable<Call>> GetUserCallsAsync(string userId, int skip = 0, int take = 50, string? search = null, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var query = _dbSet.AsNoTracking().Where(c => c.UserId == userId && c.CreatedAt <= now);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.ToLower();
            query = query.Where(c =>
                c.CallerPhone.ToLower().Contains(term) ||
                c.RecipientPhone.ToLower().Contains(term) ||
                (c.CallerName != null && c.CallerName.ToLower().Contains(term)) ||
                (c.Summary != null && c.Summary.ToLower().Contains(term)) ||
                (c.Notes != null && c.Notes.ToLower().Contains(term)));
        }

        return await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountUserCallsAsync(string userId, string? search = null, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var query = _dbSet.AsNoTracking().Where(c => c.UserId == userId && c.CreatedAt <= now);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.ToLower();
            query = query.Where(c =>
                c.CallerPhone.ToLower().Contains(term) ||
                c.RecipientPhone.ToLower().Contains(term) ||
                (c.CallerName != null && c.CallerName.ToLower().Contains(term)) ||
                (c.Summary != null && c.Summary.ToLower().Contains(term)) ||
                (c.Notes != null && c.Notes.ToLower().Contains(term)));
        }

        return await query.CountAsync(cancellationToken);
    }

    public async Task<Call?> GetWithTranscriptAsync(string callId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(c => c.TranscriptEntries.OrderBy(t => t.SequenceNumber))
            .FirstOrDefaultAsync(c => c.Id == callId, cancellationToken);
    }
}
