namespace SpeakHalo.Domain.Entities;

public class InvoiceLineItem : BaseEntity
{
    public string InvoiceId { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Amount { get; set; }
    public string Category { get; set; } = string.Empty;

    public virtual Invoice Invoice { get; set; } = null!;
}
