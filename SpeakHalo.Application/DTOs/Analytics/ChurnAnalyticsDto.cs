namespace SpeakHalo.Application.DTOs.Analytics;

public class ChurnAnalyticsDto
{
    public double CurrentMonthChurnRate { get; set; }
    public double PreviousMonthChurnRate { get; set; }
    public double ChurnRateChange { get; set; }
    public int TotalActiveSubscriptions { get; set; }
    public int ChurnedThisMonth { get; set; }
    public Dictionary<string, double> MonthlyChurnRates { get; set; } = new();
    public ChurnBreakdownDto Breakdown { get; set; } = new();
    public DateTime CalculatedAt { get; set; }
}

public class ChurnBreakdownDto
{
    public int VoluntaryCancellations { get; set; }
    public int SubscriptionExpirations { get; set; }
    public int PaymentFailures { get; set; }
}
