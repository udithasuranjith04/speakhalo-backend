using SpeakHalo.Application.DTOs;
using SpeakHalo.Shared.Models;

namespace SpeakHalo.Application.Interfaces;

public interface IInvoiceService
{
    Task<Result<InvoiceResponse>> GenerateInvoiceAsync(GenerateInvoiceRequest request, CancellationToken cancellationToken = default);
    Task<Result<InvoiceResponse>> GetInvoiceByIdAsync(string invoiceId, CancellationToken cancellationToken = default);
    Task<Result<InvoiceResponse>> GetInvoiceByNumberAsync(string invoiceNumber, CancellationToken cancellationToken = default);
    Task<Result<List<InvoiceResponse>>> GetUserInvoicesAsync(string userId, CancellationToken cancellationToken = default);
    Task<Result<InvoiceResponse>> MarkInvoicePaidAsync(string invoiceId, MarkInvoicePaidRequest request, CancellationToken cancellationToken = default);
    Task<Result<bool>> EmailInvoiceAsync(string invoiceId, CancellationToken cancellationToken = default);
    Task<Result<bool>> GenerateInvoicePdfAsync(string invoiceId, CancellationToken cancellationToken = default);
}
