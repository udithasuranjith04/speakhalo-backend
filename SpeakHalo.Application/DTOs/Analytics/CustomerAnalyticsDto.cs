namespace SpeakHalo.Application.DTOs.Analytics;

public class CustomerAnalyticsDto
{
    public int TotalCustomers { get; set; }
    public int NewCustomersThisMonth { get; set; }
    public int ActiveCustomers { get; set; }
    public double CustomerRetentionRate { get; set; }
    public double ChurnRate { get; set; }
    public List<CustomerGrowthDto> GrowthTrend { get; set; } = new();
    public Dictionary<string, int> CustomersBySource { get; set; } = new();
}

public class CustomerGrowthDto
{
    public string Month { get; set; } = string.Empty;
    public int NewCustomers { get; set; }
    public int TotalCustomers { get; set; }
}
