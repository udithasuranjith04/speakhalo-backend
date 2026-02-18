using SpeakHalo.Application.DTOs;
using SpeakHalo.Shared.Models;

namespace SpeakHalo.Application.Interfaces;

public interface ITwilioService
{
    Task<Result<string>> MakeOutboundCallAsync(string toNumber, string fromNumber, string twimlUrl, CancellationToken cancellationToken = default);
    Task<Result<string>> SendSmsAsync(string toNumber, string fromNumber, string message, CancellationToken cancellationToken = default);
    Task<Result<string>> SendWhatsAppMessageAsync(string toNumber, string fromNumber, string message, CancellationToken cancellationToken = default);
    Task<Result<bool>> ValidateRequestAsync(string signature, string url, Dictionary<string, string> parameters);
    Task<Result<string>> GetCallStatusAsync(string callSid, CancellationToken cancellationToken = default);
    Task<Result<string>> GetMessageStatusAsync(string messageSid, CancellationToken cancellationToken = default);
    Task<List<AvailableNumberResponse>> SearchAvailableNumbersAsync(string countryCode, string? areaCode = null, CancellationToken cancellationToken = default);
    Task<TwilioNumberResponse> ProvisionPhoneNumberAsync(string phoneNumber, string countryCode, CancellationToken cancellationToken = default);
    Task<bool> EnableWhatsAppForNumberAsync(string phoneNumberSid, CancellationToken cancellationToken = default);
    Task<bool> ReleasePhoneNumberAsync(string phoneNumberSid, CancellationToken cancellationToken = default);

    // On-demand purchasing methods
    Task<TwilioPhoneNumberResult?> PurchasePhoneNumberAsync(PurchasePhoneNumberOptions options, CancellationToken cancellationToken = default);
    Task<List<string>> GetAvailablePhoneNumberCountriesAsync(CancellationToken cancellationToken = default);
    Task<List<AvailableNumberResponse>> SearchAvailablePhoneNumbersAsync(string countryCode, int limit = 10, CancellationToken cancellationToken = default);
    Task ConfigureNumberForWhatsAppAsync(string phoneNumberSid, CancellationToken cancellationToken = default);
}

// Moved from OnDemandPhoneNumberService for reusability
public class PurchasePhoneNumberOptions
{
    public string CountryCode { get; set; } = string.Empty;
    public bool VoiceEnabled { get; set; } = true;
    public bool SmsEnabled { get; set; } = true;
    public bool MmsEnabled { get; set; } = true;
    public PhoneNumberCapabilities Capabilities { get; set; }
}

[Flags]
public enum PhoneNumberCapabilities
{
    Voice = 1,
    SMS = 2,
    MMS = 4,
    Fax = 8
}

public class TwilioPhoneNumberResult
{
    public string PhoneNumber { get; set; } = string.Empty;
    public string Sid { get; set; } = string.Empty;
    public string FriendlyName { get; set; } = string.Empty;
    public decimal MonthlyCost { get; set; }
}

public class TwilioNumberResponse
{
    public string PhoneNumber { get; set; } = string.Empty;
    public string FriendlyName { get; set; } = string.Empty;
    public string Sid { get; set; } = string.Empty;
    public string AccountSid { get; set; } = string.Empty;
    public NumberCapabilities? Capabilities { get; set; }
}

public class NumberCapabilities
{
    public bool Voice { get; set; }
    public bool Sms { get; set; }
    public bool Mms { get; set; }
}
