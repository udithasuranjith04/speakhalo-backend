namespace SpeakHalo.Application.DTOs;

public record PhoneNumberResponse
{
    public required string Id { get; init; }
    public required string UserId { get; init; }
    public required string Number { get; init; }
    public string? FriendlyName { get; init; }
    public required string CountryCode { get; init; }
    public bool IsPrimary { get; init; }
    public bool WhatsAppEnabled { get; init; }
    public bool WhatsAppVerified { get; init; }
    public bool VoiceEnabled { get; init; }
    public bool SmsEnabled { get; init; }
    public string? TwilioSid { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record ProvisionPhoneNumberRequest
{
    public required string PhoneNumber { get; init; }
    public required string CountryCode { get; init; } = "US";
    public string? AreaCode { get; init; }
    public string? FriendlyName { get; init; }
}

public record UpdatePhoneNumberRequest
{
    public string? FriendlyName { get; init; }
    public bool? VoiceEnabled { get; init; }
    public bool? SmsEnabled { get; init; }
}

public record ConfigureWhatsAppRequest
{
    public bool Enabled { get; init; } = true;
}

public record AvailableNumberResponse
{
    public required string PhoneNumber { get; init; }
    public string FriendlyName { get; init; } = string.Empty;
    public string Locality { get; init; } = string.Empty;
    public string Region { get; init; } = string.Empty;
    public string IsoCountry { get; init; } = string.Empty;
    public string PostalCode { get; init; } = string.Empty;
    public string AddressRequirements { get; init; } = string.Empty;
    public decimal MonthlyPrice { get; init; } = 1.15m;
}

public record CountryResponse
{
    public required string Code { get; init; }
    public required string Name { get; init; }
    public required string Region { get; init; }
    public decimal PhoneNumberMonthly { get; init; } = 1.00m;
}
