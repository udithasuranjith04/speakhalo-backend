using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SpeakHalo.Application.DTOs;
using SpeakHalo.Application.Interfaces;
using SpeakHalo.Domain.Entities;
using SpeakHalo.Domain.Interfaces.Repositories;
using SpeakHalo.Shared.Constants;
using SpeakHalo.Shared.Models;

namespace SpeakHalo.Application.Services;

public class InvoiceService : IInvoiceService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUsageMeteringService _usageMeteringService;
    private readonly IEmailService _emailService;
    private readonly ISMSConversationService _smsService;
    private readonly ILogger<InvoiceService> _logger;
    private readonly string _supportEmail;

    public InvoiceService(
        IUnitOfWork unitOfWork,
        IUsageMeteringService usageMeteringService,
        IEmailService emailService,
        ISMSConversationService smsService,
        IConfiguration configuration,
        ILogger<InvoiceService> logger)
    {
        _unitOfWork = unitOfWork;
        _usageMeteringService = usageMeteringService;
        _emailService = emailService;
        _smsService = smsService;
        _logger = logger;
        _supportEmail = configuration["SendGrid:SupportEmail"] ?? "support@speakhalo.com";
    }

    public async Task<Result<InvoiceResponse>> GenerateInvoiceAsync(GenerateInvoiceRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _unitOfWork.Users.GetByIdAsync(request.UserId, cancellationToken);
            if (user == null)
                return Result<InvoiceResponse>.Failure(ErrorMessages.NotFound("User"));

            var usageResult = await _usageMeteringService.GetCurrentPeriodUsageAsync(request.UserId, cancellationToken);
            if (!usageResult.IsSuccess)
                return Result<InvoiceResponse>.Failure(usageResult.Errors);

            var usage = usageResult.Data!;

            var invoiceNumber = await GenerateInvoiceNumberAsync(request.UserId, cancellationToken);

            var invoice = new Invoice
            {
                UserId = request.UserId,
                InvoiceNumber = invoiceNumber,
                PeriodStart = request.PeriodStart,
                PeriodEnd = request.PeriodEnd,
                Status = "draft",
                Notes = request.Notes
            };

            var lineItems = new List<InvoiceLineItem>();

            if (usage.AiCosts > 0)
            {
                lineItems.Add(new InvoiceLineItem
                {
                    Description = "AI API Usage",
                    Quantity = usage.TotalMinutes,
                    UnitPrice = usage.AiCosts / Math.Max(usage.TotalMinutes, 1),
                    Amount = usage.AiCosts,
                    Category = "ai"
                });
            }

            if (usage.TwilioCosts > 0)
            {
                lineItems.Add(new InvoiceLineItem
                {
                    Description = "Twilio Voice & SMS",
                    Quantity = usage.TotalMinutes + usage.TotalSms,
                    UnitPrice = usage.TwilioCosts / Math.Max(usage.TotalMinutes + usage.TotalSms, 1),
                    Amount = usage.TwilioCosts,
                    Category = "communication"
                });
            }

            if (usage.SendGridCosts > 0)
            {
                lineItems.Add(new InvoiceLineItem
                {
                    Description = "Email Delivery",
                    Quantity = usage.TotalEmails,
                    UnitPrice = usage.SendGridCosts / Math.Max(usage.TotalEmails, 1),
                    Amount = usage.SendGridCosts,
                    Category = "email"
                });
            }

            if (usage.StorageCosts > 0)
            {
                lineItems.Add(new InvoiceLineItem
                {
                    Description = "Storage & Data Transfer",
                    Quantity = 1,
                    UnitPrice = usage.StorageCosts,
                    Amount = usage.StorageCosts,
                    Category = "storage"
                });
            }

            invoice.Subtotal = lineItems.Sum(li => li.Amount);
            invoice.Tax = invoice.Subtotal * request.TaxRate;
            invoice.Total = invoice.Subtotal + invoice.Tax;

            await _unitOfWork.Repository<Invoice>().AddAsync(invoice, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            foreach (var lineItem in lineItems)
            {
                lineItem.InvoiceId = invoice.Id;
                await _unitOfWork.Repository<InvoiceLineItem>().AddAsync(lineItem, cancellationToken);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            invoice.LineItems = lineItems;

            return Result<InvoiceResponse>.Success(MapToResponse(invoice));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate invoice for user {UserId}", request.UserId);
            return Result<InvoiceResponse>.Failure($"Failed to generate invoice: {ex.Message}");
        }
    }

    public async Task<Result<InvoiceResponse>> GetInvoiceByIdAsync(string invoiceId, CancellationToken cancellationToken = default)
    {
        try
        {
            var invoice = await _unitOfWork.Repository<Invoice>().GetByIdAsync(invoiceId, cancellationToken);
            if (invoice == null)
                return Result<InvoiceResponse>.Failure(ErrorMessages.NotFound("Invoice"));

            var lineItems = await _unitOfWork.Repository<InvoiceLineItem>()
                .FindAsync(li => li.InvoiceId == invoiceId, cancellationToken);

            invoice.LineItems = lineItems.ToList();

            if (!string.IsNullOrEmpty(invoice.CustomerId))
                invoice.Customer = await _unitOfWork.Repository<Customer>().GetByIdAsync(invoice.CustomerId, cancellationToken);

            return Result<InvoiceResponse>.Success(MapToResponse(invoice));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get invoice {InvoiceId}", invoiceId);
            return Result<InvoiceResponse>.Failure($"Failed to get invoice: {ex.Message}");
        }
    }

    public async Task<Result<InvoiceResponse>> GetInvoiceByNumberAsync(string invoiceNumber, CancellationToken cancellationToken = default)
    {
        try
        {
            var invoice = await _unitOfWork.Repository<Invoice>()
                .FirstOrDefaultAsync(i => i.InvoiceNumber == invoiceNumber, cancellationToken);

            if (invoice == null)
                return Result<InvoiceResponse>.Failure(ErrorMessages.NotFound("Invoice"));

            var lineItems = await _unitOfWork.Repository<InvoiceLineItem>()
                .FindAsync(li => li.InvoiceId == invoice.Id, cancellationToken);

            invoice.LineItems = lineItems.ToList();

            return Result<InvoiceResponse>.Success(MapToResponse(invoice));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get invoice by number {InvoiceNumber}", invoiceNumber);
            return Result<InvoiceResponse>.Failure($"Failed to get invoice: {ex.Message}");
        }
    }

    public async Task<Result<List<InvoiceResponse>>> GetUserInvoicesAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var invoices = await _unitOfWork.Repository<Invoice>()
                .FindAsync(i => i.UserId == userId, cancellationToken);

            var invoiceList = invoices.OrderByDescending(i => i.CreatedAt).ToList();

            // Batch-load all line items in a single query to avoid N+1
            var invoiceIds = invoiceList.Select(i => i.Id).ToList();
            var allLineItems = await _unitOfWork.Repository<InvoiceLineItem>()
                .FindAsync(li => invoiceIds.Contains(li.InvoiceId), cancellationToken);
            var lineItemsByInvoice = allLineItems.GroupBy(li => li.InvoiceId)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Batch-load customers for invoices that have CustomerId
            var customerIds = invoiceList.Where(i => !string.IsNullOrEmpty(i.CustomerId)).Select(i => i.CustomerId!).Distinct().ToList();
            var customers = customerIds.Count > 0
                ? (await _unitOfWork.Repository<Customer>().FindAsync(c => customerIds.Contains(c.Id), cancellationToken)).ToDictionary(c => c.Id)
                : new Dictionary<string, Customer>();

            var responses = new List<InvoiceResponse>();
            foreach (var invoice in invoiceList)
            {
                invoice.LineItems = lineItemsByInvoice.TryGetValue(invoice.Id, out var items) ? items : new List<InvoiceLineItem>();
                if (!string.IsNullOrEmpty(invoice.CustomerId) && customers.TryGetValue(invoice.CustomerId, out var customer))
                    invoice.Customer = customer;
                responses.Add(MapToResponse(invoice));
            }

            return Result<List<InvoiceResponse>>.Success(responses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get invoices for user {UserId}", userId);
            return Result<List<InvoiceResponse>>.Failure($"Failed to get invoices: {ex.Message}");
        }
    }

    public async Task<Result<InvoiceResponse>> MarkInvoicePaidAsync(string invoiceId, MarkInvoicePaidRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var invoice = await _unitOfWork.Repository<Invoice>().GetByIdAsync(invoiceId, cancellationToken);
            if (invoice == null)
                return Result<InvoiceResponse>.Failure(ErrorMessages.NotFound("Invoice"));

            invoice.Status = "paid";
            invoice.PaidAt = DateTime.UtcNow;
            invoice.PaymentMethod = request.PaymentMethod;
            invoice.PaymentReference = request.PaymentReference;

            _unitOfWork.Repository<Invoice>().Update(invoice);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var lineItems = await _unitOfWork.Repository<InvoiceLineItem>()
                .FindAsync(li => li.InvoiceId == invoiceId, cancellationToken);

            invoice.LineItems = lineItems.ToList();

            return Result<InvoiceResponse>.Success(MapToResponse(invoice));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to mark invoice {InvoiceId} as paid", invoiceId);
            return Result<InvoiceResponse>.Failure($"Failed to mark invoice as paid: {ex.Message}");
        }
    }

    public async Task<Result<bool>> EmailInvoiceAsync(string invoiceId, CancellationToken cancellationToken = default)
    {
        try
        {
            var invoice = await _unitOfWork.Repository<Invoice>().GetByIdAsync(invoiceId, cancellationToken);
            if (invoice == null)
                return Result<bool>.Failure(ErrorMessages.NotFound("Invoice"));

            var user = await _unitOfWork.Users.GetByIdAsync(invoice.UserId, cancellationToken);
            if (user == null)
                return Result<bool>.Failure(ErrorMessages.NotFound("User"));

            var lineItems = await _unitOfWork.Repository<InvoiceLineItem>()
                .FindAsync(li => li.InvoiceId == invoiceId, cancellationToken);

            var subject = $"Invoice {invoice.InvoiceNumber} from SpeakHalo";
            var htmlContent = GenerateInvoiceEmailHtml(invoice, lineItems.ToList(), user);

            await _emailService.SendEmailAsync(user.Email, subject, htmlContent, cancellationToken: cancellationToken);

            invoice.EmailedAt = DateTime.UtcNow;
            _unitOfWork.Repository<Invoice>().Update(invoice);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to email invoice {InvoiceId}", invoiceId);
            return Result<bool>.Failure($"Failed to email invoice: {ex.Message}");
        }
    }

    public async Task<Result<InvoiceResponse>> CreateServiceInvoiceAsync(string userId, CreateServiceInvoiceRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var customer = await _unitOfWork.Repository<Customer>().GetByIdAsync(request.CustomerId, cancellationToken);
            if (customer == null || customer.UserId != userId)
                return Result<InvoiceResponse>.Failure(ErrorMessages.NotFound("Customer"));

            var invoiceNumber = await GenerateInvoiceNumberAsync(userId, cancellationToken);

            var now = DateTime.UtcNow;
            var invoice = new Invoice
            {
                UserId = userId,
                CustomerId = request.CustomerId,
                InvoiceNumber = invoiceNumber,
                PeriodStart = now,
                PeriodEnd = now,
                Status = "unpaid",
                Notes = request.Notes
            };

            var lineItems = request.LineItems.Select(li => new InvoiceLineItem
            {
                Description = li.Description,
                Quantity = li.Quantity,
                UnitPrice = li.UnitPrice,
                Amount = li.Quantity * li.UnitPrice,
                Category = li.Category
            }).ToList();

            invoice.Subtotal = lineItems.Sum(li => li.Amount);
            invoice.Tax = invoice.Subtotal * request.TaxRate;
            invoice.Total = invoice.Subtotal + invoice.Tax;

            await _unitOfWork.Repository<Invoice>().AddAsync(invoice, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            foreach (var lineItem in lineItems)
            {
                lineItem.InvoiceId = invoice.Id;
                await _unitOfWork.Repository<InvoiceLineItem>().AddAsync(lineItem, cancellationToken);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            invoice.LineItems = lineItems;
            invoice.Customer = customer;

            return Result<InvoiceResponse>.Success(MapToResponse(invoice));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create service invoice for user {UserId}", userId);
            return Result<InvoiceResponse>.Failure($"Failed to create invoice: {ex.Message}");
        }
    }

    public async Task<Result<bool>> SendInvoiceSmsAsync(string invoiceId, string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var invoice = await _unitOfWork.Repository<Invoice>().GetByIdAsync(invoiceId, cancellationToken);
            if (invoice == null || invoice.UserId != userId)
                return Result<bool>.Failure(ErrorMessages.NotFound("Invoice"));

            if (string.IsNullOrEmpty(invoice.CustomerId))
                return Result<bool>.Failure("Invoice has no associated customer");

            var customer = await _unitOfWork.Repository<Customer>().GetByIdAsync(invoice.CustomerId, cancellationToken);
            if (customer == null)
                return Result<bool>.Failure(ErrorMessages.NotFound("Customer"));

            if (string.IsNullOrEmpty(customer.Phone))
                return Result<bool>.Failure("Customer has no phone number");

            var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
            var profile = user != null ? await _unitOfWork.Repository<UserProfile>()
                .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken) : null;
            var businessName = profile?.BusinessName ?? "SpeakHalo";

            var lineItems = await _unitOfWork.Repository<InvoiceLineItem>()
                .FindAsync(li => li.InvoiceId == invoiceId, cancellationToken);
            var services = string.Join(", ", lineItems.Select(li => li.Description).Take(3));
            if (lineItems.Count() > 3)
                services += $" +{lineItems.Count() - 3} more";

            var message = $"Invoice {invoice.InvoiceNumber} from {businessName}: ${invoice.Total:F2} for {services}. Thank you!";

            var smsRequest = new SendSMSRequest
            {
                UserId = userId,
                ToNumber = customer.Phone,
                Content = message,
                CustomerId = customer.Id
            };

            var smsResult = await _smsService.SendSMSAsync(smsRequest, cancellationToken);
            if (!smsResult.IsSuccess)
                return Result<bool>.Failure(smsResult.Errors);

            invoice.SmsSentAt = DateTime.UtcNow;
            _unitOfWork.Repository<Invoice>().Update(invoice);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send SMS for invoice {InvoiceId}", invoiceId);
            return Result<bool>.Failure($"Failed to send SMS: {ex.Message}");
        }
    }

    public Task<Result<bool>> GenerateInvoicePdfAsync(string invoiceId, CancellationToken cancellationToken = default)
    {
        // PDF generation not yet implemented
        return Task.FromResult(Result<bool>.Success(true));
    }

    private async Task<string> GenerateInvoiceNumberAsync(string userId, CancellationToken cancellationToken)
    {
        var count = await _unitOfWork.Repository<Invoice>()
            .CountAsync(i => i.UserId == userId, cancellationToken);

        var year = DateTime.UtcNow.Year;
        var month = DateTime.UtcNow.Month;

        return $"INV-{year}{month:D2}-{(count + 1):D4}";
    }

    private string GenerateInvoiceEmailHtml(Invoice invoice, List<InvoiceLineItem> lineItems, User user)
    {
        var lineItemsHtml = string.Join("", lineItems.Select(li =>
            $@"<tr>
                <td style=""padding:10px 12px;border-bottom:1px solid #e5e7eb;"">{System.Net.WebUtility.HtmlEncode(li.Description)}</td>
                <td style=""padding:10px 12px;border-bottom:1px solid #e5e7eb;text-align:center;"">{li.Quantity}</td>
                <td style=""padding:10px 12px;border-bottom:1px solid #e5e7eb;text-align:right;"">${li.UnitPrice:F2}</td>
                <td style=""padding:10px 12px;border-bottom:1px solid #e5e7eb;text-align:right;font-weight:600;"">${li.Amount:F2}</td>
            </tr>"));

        var statusColor = invoice.Status?.ToLowerInvariant() switch
        {
            "paid" => "#10b981",
            "overdue" => "#dc2626",
            _ => "#f59e0b"
        };

        return $@"<!DOCTYPE html>
<html><head><meta charset=""utf-8""><meta name=""viewport"" content=""width=device-width,initial-scale=1""></head>
<body style=""margin:0;padding:0;background-color:#f3f4f6;font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,sans-serif;"">
<div style=""max-width:600px;margin:0 auto;padding:20px;"">
  <div style=""background:linear-gradient(135deg,#667eea 0%,#764ba2 100%);border-radius:12px 12px 0 0;padding:30px;text-align:center;"">
    <h1 style=""color:white;margin:0;font-size:24px;"">Invoice {System.Net.WebUtility.HtmlEncode(invoice.InvoiceNumber)}</h1>
    <p style=""color:rgba(255,255,255,0.85);margin:8px 0 0;font-size:14px;"">{invoice.PeriodStart:MMMM dd, yyyy} &mdash; {invoice.PeriodEnd:MMMM dd, yyyy}</p>
  </div>
  <div style=""background:white;padding:30px;border-radius:0 0 12px 12px;"">
    <p style=""color:#374151;font-size:16px;"">Dear {System.Net.WebUtility.HtmlEncode(user.Name)},</p>
    <p style=""color:#374151;font-size:16px;"">Thank you for using SpeakHalo. Here is your invoice:</p>
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
      <p style=""color:#6b7280;margin:4px 0;"">Subtotal: <strong>${invoice.Subtotal:F2}</strong></p>
      <p style=""color:#6b7280;margin:4px 0;"">Tax: <strong>${invoice.Tax:F2}</strong></p>
      <p style=""color:#111827;font-size:18px;margin:8px 0 0;font-weight:700;"">Total: ${invoice.Total:F2}</p>
    </div>
    <p style=""margin-top:16px;"">Status: <span style=""display:inline-block;padding:4px 12px;border-radius:12px;font-size:13px;font-weight:600;color:white;background:{statusColor};"">{invoice.Status}</span></p>
    <hr style=""border:none;border-top:1px solid #e5e7eb;margin:24px 0;""/>
    <p style=""color:#6b7280;font-size:14px;"">If you have any questions about this invoice, please contact us at {_supportEmail}</p>
  </div>
  <p style=""text-align:center;color:#9ca3af;font-size:12px;margin-top:20px;"">&copy; {DateTime.UtcNow.Year} SpeakHalo. All rights reserved.</p>
</div>
</body></html>";
    }

    private static InvoiceResponse MapToResponse(Invoice invoice)
    {
        return new InvoiceResponse
        {
            Id = invoice.Id,
            UserId = invoice.UserId,
            CustomerId = invoice.CustomerId,
            CustomerName = invoice.Customer?.Name,
            CustomerPhone = invoice.Customer?.Phone,
            InvoiceNumber = invoice.InvoiceNumber,
            PeriodStart = invoice.PeriodStart,
            PeriodEnd = invoice.PeriodEnd,
            Subtotal = invoice.Subtotal,
            Tax = invoice.Tax,
            Total = invoice.Total,
            Status = invoice.Status,
            PaidAt = invoice.PaidAt,
            PaymentMethod = invoice.PaymentMethod,
            PaymentReference = invoice.PaymentReference,
            PdfUrl = invoice.PdfUrl,
            EmailedAt = invoice.EmailedAt,
            SmsSentAt = invoice.SmsSentAt,
            Notes = invoice.Notes,
            LineItems = invoice.LineItems.Select(li => new InvoiceLineItemDto
            {
                Description = li.Description,
                Quantity = li.Quantity,
                UnitPrice = li.UnitPrice,
                Amount = li.Amount,
                Category = li.Category
            }).ToList(),
            CreatedAt = invoice.CreatedAt,
            UpdatedAt = invoice.UpdatedAt
        };
    }
}
