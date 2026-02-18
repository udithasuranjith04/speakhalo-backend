using System.Net;
using Microsoft.Extensions.Logging;
using SpeakHalo.Application.DTOs;
using SpeakHalo.Application.Interfaces;
using SpeakHalo.Domain.Entities;
using SpeakHalo.Domain.Interfaces.Repositories;
using SpeakHalo.Shared.Constants;
using SpeakHalo.Shared.Extensions;
using SpeakHalo.Shared.Models;

#pragma warning disable CS0618

namespace SpeakHalo.Application.Services;

public class QuoteService : IQuoteService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailService _emailService;
    private readonly ILogger<QuoteService> _logger;

    public QuoteService(IUnitOfWork unitOfWork, IEmailService emailService, ILogger<QuoteService> logger)
    {
        _unitOfWork = unitOfWork;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<Result<QuoteResponse>> CreateQuoteAsync(string userId, CreateQuoteRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var customer = await _unitOfWork.Customers.GetByIdAsync(request.CustomerId, cancellationToken);
            if (customer == null)
                return Result<QuoteResponse>.Failure(ErrorMessages.NotFound("Customer"));

            var quoteNumber = await _unitOfWork.Quotes.GenerateQuoteNumberAsync(userId, cancellationToken);

            var subtotal = request.LineItems.Sum(item => item.Quantity * item.UnitPrice);
            var tax = subtotal * request.TaxRate;
            var total = subtotal + tax;

            var quote = new Quote
            {
                UserId = userId,
                CustomerId = request.CustomerId,
                QuoteNumber = quoteNumber,
                Status = "draft",
                Subtotal = subtotal,
                Tax = tax,
                Total = total,
                ExpiresAt = request.ExpiresAt,
                Notes = request.Notes
            };

            await _unitOfWork.Quotes.AddAsync(quote, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            foreach (var item in request.LineItems)
            {
                var lineItem = new Domain.Entities.QuoteLineItem
                {
                    QuoteId = quote.Id,
                    Description = item.Description,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    Amount = item.Quantity * item.UnitPrice
                };
                await _unitOfWork.Repository<Domain.Entities.QuoteLineItem>().AddAsync(lineItem, cancellationToken);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var createdQuote = await _unitOfWork.Quotes.GetWithLineItemsAsync(quote.Id, cancellationToken);
            return Result<QuoteResponse>.Success(MapToResponse(createdQuote!));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create quote for user {UserId}", userId);
            return Result<QuoteResponse>.Failure($"Failed to create quote: {ex.Message}");
        }
    }

    public async Task<Result<QuoteResponse>> GetQuoteByIdAsync(string quoteId, CancellationToken cancellationToken = default)
    {
        try
        {
            var quote = await _unitOfWork.Quotes.GetWithLineItemsAsync(quoteId, cancellationToken);
            if (quote == null)
                return Result<QuoteResponse>.Failure(ErrorMessages.NotFound("Quote"));

            return Result<QuoteResponse>.Success(MapToResponse(quote));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get quote {QuoteId}", quoteId);
            return Result<QuoteResponse>.Failure($"Failed to get quote: {ex.Message}");
        }
    }

    public async Task<Result<PaginatedList<QuoteResponse>>> GetUserQuotesAsync(string userId, int pageNumber = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        try
        {
            var quotes = await _unitOfWork.Quotes.GetByUserIdAsync(userId, cancellationToken);

            var paginatedQuotes = await quotes
                .AsQueryable()
                .ToPaginatedListAsync(pageNumber, pageSize, cancellationToken);

            var responses = paginatedQuotes.Items.Select(MapToResponse).ToList();

            var result = new PaginatedList<QuoteResponse>(
                responses,
                paginatedQuotes.TotalCount,
                paginatedQuotes.CurrentPage,
                paginatedQuotes.PageSize
            );

            return Result<PaginatedList<QuoteResponse>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get quotes for user {UserId}", userId);
            return Result<PaginatedList<QuoteResponse>>.Failure($"Failed to get quotes: {ex.Message}");
        }
    }

    public async Task<Result<PaginatedList<QuoteResponse>>> GetCustomerQuotesAsync(string customerId, int pageNumber = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        try
        {
            var quotes = await _unitOfWork.Quotes.GetByCustomerIdAsync(customerId, cancellationToken);

            var paginatedQuotes = await quotes
                .AsQueryable()
                .ToPaginatedListAsync(pageNumber, pageSize, cancellationToken);

            var responses = paginatedQuotes.Items.Select(MapToResponse).ToList();

            var result = new PaginatedList<QuoteResponse>(
                responses,
                paginatedQuotes.TotalCount,
                paginatedQuotes.CurrentPage,
                paginatedQuotes.PageSize
            );

            return Result<PaginatedList<QuoteResponse>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get quotes for customer {CustomerId}", customerId);
            return Result<PaginatedList<QuoteResponse>>.Failure($"Failed to get customer quotes: {ex.Message}");
        }
    }

    public async Task<Result<QuoteResponse>> UpdateQuoteAsync(string quoteId, UpdateQuoteRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var quote = await _unitOfWork.Quotes.GetWithLineItemsAsync(quoteId, cancellationToken);
            if (quote == null)
                return Result<QuoteResponse>.Failure(ErrorMessages.NotFound("Quote"));

            if (quote.Status != "draft")
                return Result<QuoteResponse>.Failure("Only draft quotes can be edited");

            if (request.LineItems != null && request.LineItems.Any())
            {
                var existingLineItems = quote.LineItems.ToList();
                foreach (var item in existingLineItems)
                {
                    _unitOfWork.Repository<Domain.Entities.QuoteLineItem>().Remove(item);
                }

                var subtotal = request.LineItems.Sum(item => item.Quantity * item.UnitPrice);
                var tax = subtotal * (request.TaxRate ?? 0);
                var total = subtotal + tax;

                quote.Subtotal = subtotal;
                quote.Tax = tax;
                quote.Total = total;

                foreach (var item in request.LineItems)
                {
                    var lineItem = new Domain.Entities.QuoteLineItem
                    {
                        QuoteId = quote.Id,
                        Description = item.Description,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        Amount = item.Quantity * item.UnitPrice
                    };
                    await _unitOfWork.Repository<Domain.Entities.QuoteLineItem>().AddAsync(lineItem, cancellationToken);
                }
            }

            if (request.ExpiresAt.HasValue)
                quote.ExpiresAt = request.ExpiresAt;

            if (request.Notes != null)
                quote.Notes = request.Notes;

            _unitOfWork.Quotes.Update(quote);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var updatedQuote = await _unitOfWork.Quotes.GetWithLineItemsAsync(quote.Id, cancellationToken);
            return Result<QuoteResponse>.Success(MapToResponse(updatedQuote!));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update quote {QuoteId}", quoteId);
            return Result<QuoteResponse>.Failure($"Failed to update quote: {ex.Message}");
        }
    }

    public async Task<Result<bool>> DeleteQuoteAsync(string quoteId, CancellationToken cancellationToken = default)
    {
        try
        {
            var quote = await _unitOfWork.Quotes.GetByIdAsync(quoteId, cancellationToken);
            if (quote == null)
                return Result<bool>.Failure(ErrorMessages.NotFound("Quote"));

            if (quote.Status != "draft")
                return Result<bool>.Failure("Only draft quotes can be deleted");

            _unitOfWork.Quotes.Remove(quote);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete quote {QuoteId}", quoteId);
            return Result<bool>.Failure($"Failed to delete quote: {ex.Message}");
        }
    }

    public async Task<Result<QuoteResponse>> SendQuoteAsync(string quoteId, CancellationToken cancellationToken = default)
    {
        try
        {
            var quote = await _unitOfWork.Quotes.GetWithLineItemsAsync(quoteId, cancellationToken);
            if (quote == null)
                return Result<QuoteResponse>.Failure(ErrorMessages.NotFound("Quote"));

            if (quote.Status != "draft")
                return Result<QuoteResponse>.Failure("Quote has already been sent");

            quote.Status = "sent";
            quote.SentAt = DateTime.UtcNow;

            _unitOfWork.Quotes.Update(quote);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            if (!string.IsNullOrEmpty(quote.Customer.Email))
            {
                var subject = $"Quote {quote.QuoteNumber} from {quote.User.BusinessName}";
                var htmlContent = GenerateQuoteEmailHtml(quote);
                await _emailService.SendEmailAsync(quote.Customer.Email, subject, htmlContent, cancellationToken: cancellationToken);
            }

            return Result<QuoteResponse>.Success(MapToResponse(quote));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send quote {QuoteId}", quoteId);
            return Result<QuoteResponse>.Failure($"Failed to send quote: {ex.Message}");
        }
    }

    public async Task<Result<QuoteResponse>> AcceptQuoteAsync(string quoteId, CancellationToken cancellationToken = default)
    {
        try
        {
            var quote = await _unitOfWork.Quotes.GetWithLineItemsAsync(quoteId, cancellationToken);
            if (quote == null)
                return Result<QuoteResponse>.Failure(ErrorMessages.NotFound("Quote"));

            if (quote.Status != "sent")
                return Result<QuoteResponse>.Failure("Only sent quotes can be accepted");

            quote.Status = "accepted";
            quote.AcceptedAt = DateTime.UtcNow;

            _unitOfWork.Quotes.Update(quote);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<QuoteResponse>.Success(MapToResponse(quote));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to accept quote {QuoteId}", quoteId);
            return Result<QuoteResponse>.Failure($"Failed to accept quote: {ex.Message}");
        }
    }

    public async Task<Result<QuoteResponse>> RejectQuoteAsync(string quoteId, CancellationToken cancellationToken = default)
    {
        try
        {
            var quote = await _unitOfWork.Quotes.GetWithLineItemsAsync(quoteId, cancellationToken);
            if (quote == null)
                return Result<QuoteResponse>.Failure(ErrorMessages.NotFound("Quote"));

            if (quote.Status != "sent")
                return Result<QuoteResponse>.Failure("Only sent quotes can be rejected");

            quote.Status = "rejected";
            quote.RejectedAt = DateTime.UtcNow;

            _unitOfWork.Quotes.Update(quote);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<QuoteResponse>.Success(MapToResponse(quote));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reject quote {QuoteId}", quoteId);
            return Result<QuoteResponse>.Failure($"Failed to reject quote: {ex.Message}");
        }
    }

    public async Task<Result<QuoteResponse>> MarkAsViewedAsync(string quoteId, CancellationToken cancellationToken = default)
    {
        try
        {
            var quote = await _unitOfWork.Quotes.GetWithLineItemsAsync(quoteId, cancellationToken);
            if (quote == null)
                return Result<QuoteResponse>.Failure(ErrorMessages.NotFound("Quote"));

            if (quote.ViewedAt == null)
            {
                quote.ViewedAt = DateTime.UtcNow;
                _unitOfWork.Quotes.Update(quote);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            return Result<QuoteResponse>.Success(MapToResponse(quote));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to mark quote {QuoteId} as viewed", quoteId);
            return Result<QuoteResponse>.Failure($"Failed to mark quote as viewed: {ex.Message}");
        }
    }

    private static QuoteResponse MapToResponse(Quote quote)
    {
        return new QuoteResponse
        {
            Id = quote.Id,
            UserId = quote.UserId,
            CustomerId = quote.CustomerId,
            QuoteNumber = quote.QuoteNumber,
            Status = quote.Status,
            Subtotal = quote.Subtotal,
            Tax = quote.Tax,
            Total = quote.Total,
            SentAt = quote.SentAt,
            ViewedAt = quote.ViewedAt,
            AcceptedAt = quote.AcceptedAt,
            RejectedAt = quote.RejectedAt,
            ExpiresAt = quote.ExpiresAt,
            PdfUrl = quote.PdfUrl,
            Notes = quote.Notes,
            LineItems = quote.LineItems.Select(item => new QuoteLineItemResponse
            {
                Id = item.Id,
                Description = item.Description,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                Amount = item.Amount
            }).ToList(),
            CreatedAt = quote.CreatedAt
        };
    }

    /// <summary>
    /// Generate three tiered quotes (Good, Better, Best) for a service request
    /// </summary>
    public async Task<Result<TieredQuotesResponse>> GenerateTieredQuotesAsync(string userId, GenerateTieredQuotesRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var customer = await _unitOfWork.Customers.GetByIdAsync(request.CustomerId, cancellationToken);
            if (customer == null)
                return Result<TieredQuotesResponse>.Failure(ErrorMessages.NotFound("Customer"));

            var taxRate = request.TaxRate;
            var baseItems = request.BaseItems ?? new List<QuoteLineItemRequest>();

            // Generate Good tier (Basic/Budget option)
            var goodQuote = await CreateTieredQuoteAsync(userId, request.CustomerId, "Good", baseItems, taxRate, request.ExpiresAt, cancellationToken);

            // Generate Better tier (Standard/Recommended option)
            var betterItems = baseItems.Select(item => new QuoteLineItemRequest
            {
                Description = item.Description,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice * 1.25m // 25% increase for better quality/service
            }).ToList();

            // Add extra items for Better tier
            if (request.IncludeWarranty)
            {
                betterItems.Add(new QuoteLineItemRequest
                {
                    Description = "Extended Warranty (1 year)",
                    Quantity = 1,
                    UnitPrice = baseItems.Sum(i => i.UnitPrice * i.Quantity) * 0.15m
                });
            }

            var betterQuote = await CreateTieredQuoteAsync(userId, request.CustomerId, "Better", betterItems, taxRate, request.ExpiresAt, cancellationToken);

            // Generate Best tier (Premium/Comprehensive option)
            var bestItems = baseItems.Select(item => new QuoteLineItemRequest
            {
                Description = $"{item.Description} (Premium)",
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice * 1.5m // 50% increase for premium
            }).ToList();

            // Add premium services
            if (request.IncludeWarranty)
            {
                bestItems.Add(new QuoteLineItemRequest
                {
                    Description = "Lifetime Warranty",
                    Quantity = 1,
                    UnitPrice = baseItems.Sum(i => i.UnitPrice * i.Quantity) * 0.25m
                });
            }

            bestItems.Add(new QuoteLineItemRequest
            {
                Description = "Priority Support (24/7)",
                Quantity = 1,
                UnitPrice = 299m
            });

            bestItems.Add(new QuoteLineItemRequest
            {
                Description = "Annual Maintenance Package",
                Quantity = 1,
                UnitPrice = 499m
            });

            var bestQuote = await CreateTieredQuoteAsync(userId, request.CustomerId, "Best", bestItems, taxRate, request.ExpiresAt, cancellationToken);

            var response = new TieredQuotesResponse
            {
                GoodQuote = goodQuote,
                BetterQuote = betterQuote,
                BestQuote = bestQuote,
                RecommendedTier = "Better"
            };

            return Result<TieredQuotesResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate tiered quotes for user {UserId}", userId);
            return Result<TieredQuotesResponse>.Failure($"Failed to generate tiered quotes: {ex.Message}");
        }
    }

    private async Task<QuoteResponse> CreateTieredQuoteAsync(
        string userId,
        string customerId,
        string tier,
        List<QuoteLineItemRequest> items,
        decimal taxRate,
        DateTime? expiresAt,
        CancellationToken cancellationToken)
    {
        var quoteNumber = await _unitOfWork.Quotes.GenerateQuoteNumberAsync(userId, cancellationToken);

        var subtotal = items.Sum(item => item.Quantity * item.UnitPrice);
        var tax = subtotal * taxRate;
        var total = subtotal + tax;

        var quote = new Quote
        {
            UserId = userId,
            CustomerId = customerId,
            QuoteNumber = $"{quoteNumber}-{tier[0]}", // Append tier suffix (G/B/B)
            Status = "draft",
            Tier = tier,
            Subtotal = subtotal,
            Tax = tax,
            Total = total,
            ExpiresAt = expiresAt,
            Notes = GetTierDescription(tier)
        };

        await _unitOfWork.Quotes.AddAsync(quote, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        foreach (var item in items)
        {
            var lineItem = new Domain.Entities.QuoteLineItem
            {
                QuoteId = quote.Id,
                Description = item.Description,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                Amount = item.Quantity * item.UnitPrice
            };
            await _unitOfWork.Repository<Domain.Entities.QuoteLineItem>().AddAsync(lineItem, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var createdQuote = await _unitOfWork.Quotes.GetWithLineItemsAsync(quote.Id, cancellationToken);
        return MapToResponse(createdQuote!);
    }

    private static string GetTierDescription(string tier)
    {
        return tier switch
        {
            "Good" => "Budget-friendly option with essential services and standard quality parts.",
            "Better" => "⭐ RECOMMENDED: Best value with enhanced service, quality parts, and extended warranty.",
            "Best" => "Premium option with top-tier parts, lifetime warranty, priority support, and annual maintenance.",
            _ => string.Empty
        };
    }

    private static string GenerateQuoteEmailHtml(Quote quote)
    {
        var customerName = WebUtility.HtmlEncode(quote.Customer.Name);
        var businessName = WebUtility.HtmlEncode(quote.User.BusinessName);
        var lineItemsHtml = string.Join("", quote.LineItems.Select(item =>
            $@"<tr>
                <td style=""padding:10px 12px;border-bottom:1px solid #e5e7eb;"">{WebUtility.HtmlEncode(item.Description)}</td>
                <td style=""padding:10px 12px;border-bottom:1px solid #e5e7eb;text-align:center;"">{item.Quantity}</td>
                <td style=""padding:10px 12px;border-bottom:1px solid #e5e7eb;text-align:right;"">${item.UnitPrice:F2}</td>
                <td style=""padding:10px 12px;border-bottom:1px solid #e5e7eb;text-align:right;font-weight:600;"">${item.Amount:F2}</td>
            </tr>"));
        var expiryHtml = quote.ExpiresAt.HasValue
            ? $@"<p style=""color:#f59e0b;font-size:14px;margin-top:16px;"">This quote expires on {quote.ExpiresAt.Value:MMMM dd, yyyy}</p>"
            : "";

        return $@"<!DOCTYPE html>
<html><head><meta charset=""utf-8""><meta name=""viewport"" content=""width=device-width,initial-scale=1""></head>
<body style=""margin:0;padding:0;background-color:#f3f4f6;font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,sans-serif;"">
<div style=""max-width:600px;margin:0 auto;padding:20px;"">
  <div style=""background:linear-gradient(135deg,#667eea 0%,#764ba2 100%);border-radius:12px 12px 0 0;padding:30px;text-align:center;"">
    <h1 style=""color:white;margin:0;font-size:24px;"">Quote {WebUtility.HtmlEncode(quote.QuoteNumber)}</h1>
    <p style=""color:rgba(255,255,255,0.85);margin:8px 0 0;font-size:14px;"">From {businessName}</p>
  </div>
  <div style=""background:white;padding:30px;border-radius:0 0 12px 12px;"">
    <p style=""color:#374151;font-size:16px;"">Dear {customerName},</p>
    <p style=""color:#374151;font-size:16px;"">Thank you for your interest. Please find your quote below:</p>
    <table style=""width:100%;border-collapse:collapse;margin:20px 0;"">
      <thead>
        <tr style=""background:#f9fafb;"">
          <th style=""padding:10px 12px;text-align:left;font-size:13px;color:#6b7280;border-bottom:2px solid #e5e7eb;"">Description</th>
          <th style=""padding:10px 12px;text-align:center;font-size:13px;color:#6b7280;border-bottom:2px solid #e5e7eb;"">Qty</th>
          <th style=""padding:10px 12px;text-align:right;font-size:13px;color:#6b7280;border-bottom:2px solid #e5e7eb;"">Unit Price</th>
          <th style=""padding:10px 12px;text-align:right;font-size:13px;color:#6b7280;border-bottom:2px solid #e5e7eb;"">Amount</th>
        </tr>
      </thead>
      <tbody>{lineItemsHtml}</tbody>
    </table>
    <div style=""text-align:right;margin-top:10px;"">
      <p style=""color:#6b7280;margin:4px 0;"">Subtotal: <strong>${quote.Subtotal:F2}</strong></p>
      <p style=""color:#6b7280;margin:4px 0;"">Tax: <strong>${quote.Tax:F2}</strong></p>
      <p style=""color:#111827;font-size:18px;margin:8px 0 0;font-weight:700;"">Total: ${quote.Total:F2}</p>
    </div>
    {expiryHtml}
    <p style=""color:#374151;margin-top:24px;"">Best regards,<br/><strong>{businessName}</strong></p>
  </div>
  <p style=""text-align:center;color:#9ca3af;font-size:12px;margin-top:20px;"">Powered by SpeakHalo</p>
</div>
</body></html>";
    }
}

#pragma warning restore CS0618
