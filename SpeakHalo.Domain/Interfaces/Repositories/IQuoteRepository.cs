using SpeakHalo.Domain.Entities;

namespace SpeakHalo.Domain.Interfaces.Repositories;

public interface IQuoteRepository : IRepository<Quote>
{
    Task<IEnumerable<Quote>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Quote>> GetByCustomerIdAsync(string customerId, CancellationToken cancellationToken = default);
    Task<Quote?> GetWithLineItemsAsync(string quoteId, CancellationToken cancellationToken = default);
    Task<Quote?> GetByQuoteNumberAsync(string quoteNumber, CancellationToken cancellationToken = default);
    Task<string> GenerateQuoteNumberAsync(string userId, CancellationToken cancellationToken = default);
}
