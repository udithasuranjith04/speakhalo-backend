namespace SpeakHalo.Domain.Entities;

public class Invoice : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }

    public decimal Subtotal { get; set; } = 0;
    public decimal Tax { get; set; } = 0;
    public decimal Total { get; set; } = 0;

    public string Status { get; set; } = "draft";
    public DateTime? PaidAt { get; set; }
    public string? PaymentMethod { get; set; }
    public string? PaymentReference { get; set; }

    public string? PdfUrl { get; set; }
    public DateTime? EmailedAt { get; set; }

    public string? Notes { get; set; }

    public uint RowVersion { get; set; }

    public virtual User User { get; set; } = null!;
    public virtual ICollection<InvoiceLineItem> LineItems { get; set; } = new List<InvoiceLineItem>();
}
