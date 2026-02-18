namespace SpeakHalo.Domain.Entities;

public class QuoteLineItem : BaseEntity
{
    public string QuoteId { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Amount { get; set; }

    public virtual Quote Quote { get; set; } = null!;
}
