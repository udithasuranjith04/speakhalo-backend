namespace SpeakHalo.Application.Interfaces;

public interface ICallerRecognitionService
{
    Task<RecognizedCaller?> RecognizeCallerAsync(string userId, string phone, string channel, CancellationToken ct = default);
}

public record RecognizedCaller(
    string? CustomerId,
    string? Name,
    int JobCount,
    string? LastServiceType,
    DateTime? LastServiceDate,
    string? Notes,
    CallerCallHistory CallHistory,
    CallerMessagingHistory WhatsAppHistory,
    CallerMessagingHistory SmsHistory,
    IReadOnlyList<CallerAppointmentInfo> UpcomingAppointments,
    IReadOnlyList<CallerAppointmentInfo> PastAppointments
);

public record CallerCallHistory(int TotalCalls, DateTime? LastCallDate, string? LastCallSummary, string? LastCallServiceType);
public record CallerMessagingHistory(int ConversationCount, int TotalMessages, DateTime? LastMessageDate, string? LastConversationSummary);
public record CallerAppointmentInfo(string ServiceType, string Status, DateTime ScheduledAt, string? Description);
