namespace SpeakHalo.Domain.Entities;

public class ReminderSettings : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public int HoursBeforeAppointment { get; set; } = 24;
    public bool SendSms { get; set; } = true;
    public bool SendEmail { get; set; } = false;
    public bool SendVoiceCall { get; set; } = false;
    public string? CustomMessage { get; set; }

    public virtual User User { get; set; } = null!;
}
