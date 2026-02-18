namespace SpeakHalo.Domain.Entities;

public class UsageCosts : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public DateTime BillingPeriodStart { get; set; }
    public DateTime BillingPeriodEnd { get; set; }
    public decimal AiCosts { get; set; } = 0;
    public decimal TwilioCosts { get; set; } = 0;
    public decimal SendGridCosts { get; set; } = 0;
    public decimal StorageCosts { get; set; } = 0;
    public decimal TotalCosts { get; set; } = 0;
    public int TotalCalls { get; set; } = 0;
    public int TotalMinutes { get; set; } = 0;
    public int TotalSms { get; set; } = 0;
    public int TotalEmails { get; set; } = 0;

    public virtual User User { get; set; } = null!;
}
