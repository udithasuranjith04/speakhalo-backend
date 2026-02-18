namespace SpeakHalo.Application.DTOs.Analytics;

public class PerformanceMetricsDto
{
    public double AverageResponseTime { get; set; }
    public double CallSuccessRate { get; set; }
    public double ApiUptime { get; set; }
    public int ErrorCount { get; set; }
    public double CustomerSatisfactionScore { get; set; }
    public Dictionary<string, double> MetricsByEndpoint { get; set; } = new();
}
