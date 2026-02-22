using System.Text.Json.Serialization;
using SpeakHalo.Domain.Enums;

namespace SpeakHalo.Application.DTOs;

public record UpdateUserRequest
{
    public string? Name { get; init; }
    public string? BusinessName { get; init; }
    public string? BusinessAddress { get; init; }
}

public record UpdatePasswordRequest
{
    public required string CurrentPassword { get; init; }
    public required string NewPassword { get; init; }
}

public record UserResponse
{
    public required string Id { get; init; }
    public required string Email { get; init; }
    public required string Name { get; init; }
    public required string BusinessName { get; init; }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public UserRole Role { get; init; }
    public string? Industry { get; init; }
    public string? Phone { get; init; }
    public string? Website { get; init; }
    public string? BusinessAddress { get; init; }
    public bool IsActive { get; init; }
    public bool EmailVerified { get; init; }
    public bool TotpEnabled { get; init; }
    public DateTime CreatedAt { get; init; }
}
