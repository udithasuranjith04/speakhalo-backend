namespace SpeakHalo.Application.DTOs.Analytics;

public class RevenueAnalyticsDto
{
    public decimal TotalRevenue { get; set; }
    public decimal MonthlyRecurring { get; set; }
    public decimal UsageRevenue { get; set; }
    public decimal AverageRevenuePerUser { get; set; }
    public List<MonthlyRevenueDto> MonthlyTrends { get; set; } = new();
    public Dictionary<string, decimal> RevenueByPlan { get; set; } = new();
    public decimal ProjectedMonthlyRevenue { get; set; }
}

public class MonthlyRevenueDto
{
    public string Month { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
    public int InvoiceCount { get; set; }
}
