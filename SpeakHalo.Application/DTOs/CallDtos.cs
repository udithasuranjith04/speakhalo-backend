using SpeakHalo.Domain.Enums;

namespace SpeakHalo.Application.DTOs;

public record CreateCallRequest
{
    public required string UserId { get; init; }
    public string? CustomerId { get; init; }
    public required string FromNumber { get; init; }
    public required string ToNumber { get; init; }
    public required string Direction { get; init; }
}

public record UpdateCallRequest
{
    public int? DurationSeconds { get; init; }
    public string? RecordingUrl { get; init; }
    public CallOutcome? Outcome { get; init; }
    public CallSentiment? Sentiment { get; init; }
    public ServiceType? ServiceType { get; init; }
    public string? Summary { get; init; }
    public string? ActionItems { get; init; }
    public bool? AppointmentScheduled { get; init; }
    public bool? QuoteRequested { get; init; }
}

public record CallResponse
{
    public required string Id { get; init; }
    public required string UserId { get; init; }
    public string? CustomerId { get; init; }
    public required string FromNumber { get; init; }
    public required string ToNumber { get; init; }
    public required string Direction { get; init; }
    public string? TwilioCallSid { get; init; }
    public int DurationSeconds { get; init; }
    public string? RecordingUrl { get; init; }
    public string? TranscriptText { get; init; }
    public CallOutcome? Outcome { get; init; }
    public CallSentiment? Sentiment { get; init; }
    public ServiceType? ServiceType { get; init; }
    public string? Summary { get; init; }
    public string? ActionItems { get; init; }
    public bool AppointmentScheduled { get; init; }
    public bool QuoteRequested { get; init; }
    public decimal Cost { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record TranscriptEntryResponse
{
    public required string Id { get; init; }
    public required string CallId { get; init; }
    public required string Speaker { get; init; }
    public required string Text { get; init; }
    public double Confidence { get; init; }
    public DateTime Timestamp { get; init; }
}

public record CallAnalyticsResponse
{
    public int TotalCalls { get; init; }
    public int TotalMinutes { get; init; }
    public decimal TotalCost { get; init; }
    public decimal AverageCallDuration { get; init; }
    public int InboundCalls { get; init; }
    public int OutboundCalls { get; init; }
    public int AppointmentsScheduled { get; init; }
    public int QuotesRequested { get; init; }
    public Dictionary<string, int> OutcomeBreakdown { get; init; } = new();
    public Dictionary<string, int> SentimentBreakdown { get; init; } = new();
    public Dictionary<string, int> ServiceTypeBreakdown { get; init; } = new();
}
