namespace SpeakHalo.Application.DTOs;

public record RecordUsageRequest
{
    public required string UserId { get; init; }
    public decimal? AiCosts { get; init; }
    public decimal? TwilioCosts { get; init; }
    public decimal? SendGridCosts { get; init; }
    public decimal? StorageCosts { get; init; }
    public int? CallMinutes { get; init; }
    public int? SmsCount { get; init; }
    public int? EmailCount { get; init; }
}

public record UsageCostsResponse
{
    public required string Id { get; init; }
    public required string UserId { get; init; }
    public DateTime BillingPeriodStart { get; init; }
    public DateTime BillingPeriodEnd { get; init; }
    public decimal AiCosts { get; init; }
    public decimal TwilioCosts { get; init; }
    public decimal SendGridCosts { get; init; }
    public decimal StorageCosts { get; init; }
    public decimal TotalCosts { get; init; }
    public int TotalCalls { get; init; }
    public int TotalMinutes { get; init; }
    public int TotalSms { get; init; }
    public int TotalEmails { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public record UsageSummaryResponse
{
    public required string UserId { get; init; }
    public decimal CurrentPeriodCosts { get; init; }
    public int CurrentPeriodMinutes { get; init; }
    public int CurrentPeriodSms { get; init; }
    public int CurrentPeriodEmails { get; init; }
    public decimal MonthToDateCosts { get; init; }
    public decimal YearToDateCosts { get; init; }
    public decimal AverageDailyCosts { get; init; }
    public decimal ProjectedMonthlyCosts { get; init; }
}
