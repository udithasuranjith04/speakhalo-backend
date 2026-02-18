namespace SpeakHalo.Application.DTOs.Analytics;

public class CallAnalyticsDto
{
    public int TotalCalls { get; set; }
    public int AnsweredCalls { get; set; }
    public int MissedCalls { get; set; }
    public double AverageCallDuration { get; set; }
    public int TotalMinutes { get; set; }
    public decimal TotalCost { get; set; }
    public double AnswerRate { get; set; }

    // Conversion tracking
    public int CallsWithAppointments { get; set; }
    public int CallsWithQuotes { get; set; }
    public double ConversionRate { get; set; } // % of answered calls that resulted in an appointment

    public List<CallTrendDto> DailyTrends { get; set; } = new();
    public Dictionary<string, int> CallsByHour { get; set; } = new();
    public List<TopCustomerDto> TopCustomers { get; set; } = new();
}

public class CallTrendDto
{
    public DateTime Date { get; set; }
    public int CallCount { get; set; }
    public int Duration { get; set; }
    public decimal Cost { get; set; }
}

public class TopCustomerDto
{
    public string CustomerId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public int CallCount { get; set; }
    public int TotalMinutes { get; set; }
}
