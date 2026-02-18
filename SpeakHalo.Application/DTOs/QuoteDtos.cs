namespace SpeakHalo.Application.DTOs;

public record QuoteLineItemRequest
{
    public required string Description { get; init; }
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
}

public record CreateQuoteRequest
{
    public required string CustomerId { get; init; }
    public required List<QuoteLineItemRequest> LineItems { get; init; }
    public decimal TaxRate { get; init; } = 0;
    public DateTime? ExpiresAt { get; init; }
    public string? Notes { get; init; }
}

public record UpdateQuoteRequest
{
    public List<QuoteLineItemRequest>? LineItems { get; init; }
    public decimal? TaxRate { get; init; }
    public DateTime? ExpiresAt { get; init; }
    public string? Notes { get; init; }
}

public record QuoteLineItemResponse
{
    public required string Id { get; init; }
    public required string Description { get; init; }
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal Amount { get; init; }
}

public record QuoteResponse
{
    public required string Id { get; init; }
    public required string UserId { get; init; }
    public required string CustomerId { get; init; }
    public required string QuoteNumber { get; init; }
    public required string Status { get; init; }
    public decimal Subtotal { get; init; }
    public decimal Tax { get; init; }
    public decimal Total { get; init; }
    public DateTime? SentAt { get; init; }
    public DateTime? ViewedAt { get; init; }
    public DateTime? AcceptedAt { get; init; }
    public DateTime? RejectedAt { get; init; }
    public DateTime? ExpiresAt { get; init; }
    public string? PdfUrl { get; init; }
    public string? Notes { get; init; }
    public List<QuoteLineItemResponse> LineItems { get; init; } = new();
    public DateTime CreatedAt { get; init; }
}
