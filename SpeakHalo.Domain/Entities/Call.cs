using SpeakHalo.Domain.Enums;

namespace SpeakHalo.Domain.Entities;

public class Call : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public string? CustomerId { get; set; }
    public string? PhoneNumberId { get; set; }
    public string CallerPhone { get; set; } = string.Empty;
    public string? CallerName { get; set; }
    public string RecipientPhone { get; set; } = string.Empty;
    public string Direction { get; set; } = "outbound";

    public CallOutcome? Outcome { get; set; }
    public CallSentiment? Sentiment { get; set; }
    public ServiceType? ServiceType { get; set; }

    public int DurationSeconds { get; set; } = 0;
    public decimal Cost { get; set; } = 0;

    public string? RecordingSid { get; set; }
    public string? RecordingUrl { get; set; }
    public int? RecordingDuration { get; set; }
    public string? RecordingStatus { get; set; }

    public string? TwilioCallSid { get; set; }
    public string? TransferredToNumber { get; set; }
    public bool WasTransferred { get; set; } = false;
    public int? TransferDurationSeconds { get; set; }

    public string? Summary { get; set; }
    public string? Notes { get; set; }
    public string? TranscriptText { get; set; }
    public string? ActionItems { get; set; }
    public bool AppointmentScheduled { get; set; } = false;
    public bool QuoteRequested { get; set; } = false;
    public DateTime? EndedAt { get; set; }

    public virtual User User { get; set; } = null!;
    public virtual Customer? Customer { get; set; }
    public virtual PhoneNumber? PhoneNumber { get; set; }
    public virtual ICollection<TranscriptEntry> TranscriptEntries { get; set; } = new List<TranscriptEntry>();
}
