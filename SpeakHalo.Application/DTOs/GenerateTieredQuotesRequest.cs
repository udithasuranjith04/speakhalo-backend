namespace SpeakHalo.Application.DTOs;

public class GenerateTieredQuotesRequest
{
    public string CustomerId { get; set; } = string.Empty;
    public List<QuoteLineItemRequest>? BaseItems { get; set; }
    public decimal TaxRate { get; set; } = 0.0m;
    public DateTime? ExpiresAt { get; set; }
    public bool IncludeWarranty { get; set; } = true;
}

public class TieredQuotesResponse
{
    public QuoteResponse GoodQuote { get; set; } = null!;
    public QuoteResponse BetterQuote { get; set; } = null!;
    public QuoteResponse BestQuote { get; set; } = null!;
    public string RecommendedTier { get; set; } = "Better";
}
