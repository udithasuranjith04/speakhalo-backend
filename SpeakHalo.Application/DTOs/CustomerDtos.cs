using SpeakHalo.Domain.Enums;

namespace SpeakHalo.Application.DTOs;

public record CreateCustomerRequest
{
    public required string UserId { get; init; }
    public required string Phone { get; init; }
    public string? Name { get; init; }
    public string? Email { get; init; }
    public string? Address { get; init; }
    public string? Notes { get; init; }
    public LeadStatus LeadStatus { get; init; } = LeadStatus.New;
}

public record UpdateCustomerRequest
{
    public string? Name { get; init; }
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public string? Address { get; init; }
    public string? Notes { get; init; }
    public LeadStatus? LeadStatus { get; init; }
    public int? TotalCalls { get; init; }
    public int? TotalAppointments { get; init; }
    public decimal? TotalRevenue { get; init; }
}

public record CustomerResponse
{
    public required string Id { get; init; }
    public required string UserId { get; init; }
    public required string Phone { get; init; }
    public string? Name { get; init; }
    public string? Email { get; init; }
    public string? Address { get; init; }
    public string? Notes { get; init; }
    public LeadStatus LeadStatus { get; init; }
    public int TotalCalls { get; init; }
    public int TotalAppointments { get; init; }
    public decimal TotalRevenue { get; init; }
    public DateTime? LastContactedAt { get; init; }
    public DateTime CreatedAt { get; init; }
}
