namespace SpeakHalo.Application.DTOs.Analytics;

public class UsageAnalyticsDto
{
    public int TotalVoiceMinutes { get; set; }
    public int TotalWhatsAppMessages { get; set; }
    public int TotalSmsMessages { get; set; }
    public List<MonthlyUsageDto> MonthlyTrends { get; set; } = new();
    public decimal AverageDailyUsage { get; set; }
    public int PeakUsageDay { get; set; }
    public Dictionary<string, int> UsageByFeature { get; set; } = new();
}

public class MonthlyUsageDto
{
    public string Month { get; set; } = string.Empty;
    public int VoiceMinutes { get; set; }
    public int WhatsAppMessages { get; set; }
    public int SmsMessages { get; set; }
}
