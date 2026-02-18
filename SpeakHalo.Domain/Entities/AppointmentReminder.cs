namespace SpeakHalo.Domain.Entities;

public class AppointmentReminder : BaseEntity
{
    public string AppointmentId { get; set; } = string.Empty;
    public DateTime ScheduledFor { get; set; }
    public bool IsSent { get; set; } = false;
    public DateTime? SentAt { get; set; }
    public string ReminderType { get; set; } = "sms";
    public string? ErrorMessage { get; set; }

    public virtual Appointment Appointment { get; set; } = null!;
}
