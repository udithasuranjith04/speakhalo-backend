namespace SpeakHalo.Domain.Entities;

public class Quote : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public string QuoteNumber { get; set; } = string.Empty;
    public string Status { get; set; } = "draft";
    public string Tier { get; set; } = "Better";
    public decimal Subtotal { get; set; } = 0;
    public decimal Tax { get; set; } = 0;
    public decimal Total { get; set; } = 0;
    public DateTime? SentAt { get; set; }
    public DateTime? ViewedAt { get; set; }
    public DateTime? AcceptedAt { get; set; }
    public DateTime? RejectedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? PdfUrl { get; set; }
    public string? Notes { get; set; }

    public virtual User User { get; set; } = null!;
    public virtual Customer Customer { get; set; } = null!;
    public virtual ICollection<QuoteLineItem> LineItems { get; set; } = new List<QuoteLineItem>();
}
