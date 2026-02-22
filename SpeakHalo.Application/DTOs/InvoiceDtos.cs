namespace SpeakHalo.Application.DTOs;

public record GenerateInvoiceRequest
{
    public required string UserId { get; init; }
    public DateTime PeriodStart { get; init; }
    public DateTime PeriodEnd { get; init; }
    public decimal TaxRate { get; init; } = 0;
    public string? Notes { get; init; }
}

public record CreateServiceInvoiceRequest
{
    public required string CustomerId { get; init; }
    public decimal TaxRate { get; init; } = 0;
    public string? Notes { get; init; }
    public required List<ServiceInvoiceLineItemRequest> LineItems { get; init; }
}

public record ServiceInvoiceLineItemRequest
{
    public string? ServiceCatalogItemId { get; init; }
    public required string Description { get; init; }
    public int Quantity { get; init; } = 1;
    public decimal UnitPrice { get; init; }
    public string Category { get; init; } = "service";
}

public record InvoiceLineItemDto
{
    public required string Description { get; init; }
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal Amount { get; init; }
    public required string Category { get; init; }
}

public record InvoiceResponse
{
    public required string Id { get; init; }
    public required string UserId { get; init; }
    public string? CustomerId { get; init; }
    public string? CustomerName { get; init; }
    public string? CustomerPhone { get; init; }
    public required string InvoiceNumber { get; init; }
    public DateTime PeriodStart { get; init; }
    public DateTime PeriodEnd { get; init; }
    public decimal Subtotal { get; init; }
    public decimal Tax { get; init; }
    public decimal Total { get; init; }
    public required string Status { get; init; }
    public DateTime? PaidAt { get; init; }
    public string? PaymentMethod { get; init; }
    public string? PaymentReference { get; init; }
    public string? PdfUrl { get; init; }
    public DateTime? EmailedAt { get; init; }
    public DateTime? SmsSentAt { get; init; }
    public string? Notes { get; init; }
    public List<InvoiceLineItemDto> LineItems { get; init; } = new();
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public record MarkInvoicePaidRequest
{
    public required string PaymentMethod { get; init; }
    public string? PaymentReference { get; init; }
}
