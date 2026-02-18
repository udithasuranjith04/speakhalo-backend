namespace SpeakHalo.Application.DTOs.Analytics;

public class AppointmentAnalyticsDto
{
    public int TotalAppointments { get; set; }
    public int CompletedAppointments { get; set; }
    public int CancelledAppointments { get; set; }
    public int NoShowAppointments { get; set; }
    public double CompletionRate { get; set; }
    public double NoShowRate { get; set; }
    public List<AppointmentTrendDto> DailyTrends { get; set; } = new();
    public Dictionary<string, int> AppointmentsByStatus { get; set; } = new();
    public Dictionary<string, int> AppointmentsByServiceType { get; set; } = new();
}

public class AppointmentTrendDto
{
    public DateTime Date { get; set; }
    public int AppointmentCount { get; set; }
    public int CompletedCount { get; set; }
}
