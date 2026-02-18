using Microsoft.EntityFrameworkCore;
using SpeakHalo.Domain.Entities;
using SpeakHalo.Domain.Interfaces.Repositories;
using SpeakHalo.Infrastructure.Data;

namespace SpeakHalo.Infrastructure.Repositories;

public class QuoteRepository : Repository<Quote>, IQuoteRepository
{
    public QuoteRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Quote>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _context.Quotes
            .AsNoTracking()
            .Where(q => q.UserId == userId)
            .Include(q => q.LineItems)
            .OrderByDescending(q => q.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Quote>> GetByCustomerIdAsync(string customerId, CancellationToken cancellationToken = default)
    {
        return await _context.Quotes
            .AsNoTracking()
            .Where(q => q.CustomerId == customerId)
            .Include(q => q.LineItems)
            .OrderByDescending(q => q.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Quote?> GetWithLineItemsAsync(string quoteId, CancellationToken cancellationToken = default)
    {
        return await _context.Quotes
            .AsNoTracking()
            .Include(q => q.LineItems)
            .Include(q => q.Customer)
            .FirstOrDefaultAsync(q => q.Id == quoteId, cancellationToken);
    }

    public async Task<Quote?> GetByQuoteNumberAsync(string quoteNumber, CancellationToken cancellationToken = default)
    {
        return await _context.Quotes
            .AsNoTracking()
            .Include(q => q.LineItems)
            .FirstOrDefaultAsync(q => q.QuoteNumber == quoteNumber, cancellationToken);
    }

    public async Task<string> GenerateQuoteNumberAsync(string userId, CancellationToken cancellationToken = default)
    {
        var count = await _context.Quotes
            .Where(q => q.UserId == userId)
            .CountAsync(cancellationToken);

        var year = DateTime.UtcNow.Year;
        var quoteNumber = $"QT-{year}-{(count + 1):D5}";

        return quoteNumber;
    }
}
