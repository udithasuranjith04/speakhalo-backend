using SpeakHalo.Application.DTOs;
using SpeakHalo.Shared.Models;

namespace SpeakHalo.Application.Interfaces;

public interface IQuoteService
{
    Task<Result<QuoteResponse>> CreateQuoteAsync(string userId, CreateQuoteRequest request, CancellationToken cancellationToken = default);
    Task<Result<QuoteResponse>> GetQuoteByIdAsync(string quoteId, CancellationToken cancellationToken = default);
    Task<Result<PaginatedList<QuoteResponse>>> GetUserQuotesAsync(string userId, int pageNumber = 1, int pageSize = 20, CancellationToken cancellationToken = default);
    Task<Result<PaginatedList<QuoteResponse>>> GetCustomerQuotesAsync(string customerId, int pageNumber = 1, int pageSize = 20, CancellationToken cancellationToken = default);
    Task<Result<QuoteResponse>> UpdateQuoteAsync(string quoteId, UpdateQuoteRequest request, CancellationToken cancellationToken = default);
    Task<Result<bool>> DeleteQuoteAsync(string quoteId, CancellationToken cancellationToken = default);
    Task<Result<QuoteResponse>> SendQuoteAsync(string quoteId, CancellationToken cancellationToken = default);
    Task<Result<QuoteResponse>> AcceptQuoteAsync(string quoteId, CancellationToken cancellationToken = default);
    Task<Result<QuoteResponse>> RejectQuoteAsync(string quoteId, CancellationToken cancellationToken = default);
    Task<Result<QuoteResponse>> MarkAsViewedAsync(string quoteId, CancellationToken cancellationToken = default);
    Task<Result<TieredQuotesResponse>> GenerateTieredQuotesAsync(string userId, GenerateTieredQuotesRequest request, CancellationToken cancellationToken = default);
}
