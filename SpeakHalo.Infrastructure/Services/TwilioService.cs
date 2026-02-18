using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;
using Twilio.Security;
using SpeakHalo.Application.DTOs;
using SpeakHalo.Application.Interfaces;
using SpeakHalo.Application.Exceptions;
using SpeakHalo.Shared.Models;

namespace SpeakHalo.Infrastructure.Services;

public class TwilioService : ITwilioService
{
    private readonly ILogger<TwilioService> _logger;
    private readonly string _accountSid;
    private readonly string _authToken;
    private readonly string _voiceWebhookUrl;
    private readonly string _smsWebhookUrl;
    private readonly string _statusCallbackUrl;
    private readonly string _whatsAppWebhookUrl;
    private readonly Dictionary<string, CountryPricingConfig> _countryPricing;

    public TwilioService(IConfiguration configuration, ILogger<TwilioService> logger)
    {
        _logger = logger;
        _accountSid = configuration["Twilio:AccountSid"] ?? throw new ArgumentNullException("Twilio:AccountSid");
        _authToken = configuration["Twilio:AuthToken"] ?? throw new ArgumentNullException("Twilio:AuthToken");
        _voiceWebhookUrl = configuration["Twilio:VoiceWebhookUrl"] ?? throw new ArgumentNullException("Twilio:VoiceWebhookUrl");
        _smsWebhookUrl = configuration["Twilio:SmsWebhookUrl"] ?? throw new ArgumentNullException("Twilio:SmsWebhookUrl");
        _statusCallbackUrl = configuration["Twilio:StatusCallbackUrl"] ?? throw new ArgumentNullException("Twilio:StatusCallbackUrl");
        _whatsAppWebhookUrl = configuration["Twilio:WhatsAppWebhookUrl"] ?? throw new ArgumentNullException("Twilio:WhatsAppWebhookUrl");

        _countryPricing = new Dictionary<string, CountryPricingConfig>(StringComparer.OrdinalIgnoreCase);
        var pricingSection = configuration.GetSection("Twilio:CountryPricing");
        foreach (var country in pricingSection.GetChildren())
        {
            _countryPricing[country.Key] = new CountryPricingConfig
            {
                PhoneNumberMonthly = decimal.TryParse(country["PhoneNumberMonthly"], out var pn) ? pn : 1.00m,
                SmsOutbound = decimal.TryParse(country["SmsOutbound"], out var sms) ? sms : 0.0079m,
                VoicePerMinute = decimal.TryParse(country["VoicePerMinute"], out var voice) ? voice : 0.0085m,
                WhatsAppPerMessage = decimal.TryParse(country["WhatsAppPerMessage"], out var wa) ? wa : 0.005m
            };
        }

        TwilioClient.Init(_accountSid, _authToken);
    }

    public async Task<Result<string>> MakeOutboundCallAsync(string toNumber, string fromNumber, string twimlUrl, CancellationToken cancellationToken = default)
    {
        try
        {
            var call = await CallResource.CreateAsync(
                to: new PhoneNumber(toNumber),
                from: new PhoneNumber(fromNumber),
                url: new Uri(twimlUrl),
                statusCallback: new Uri(_statusCallbackUrl),
                statusCallbackEvent: new List<string> { "initiated", "ringing", "answered", "completed" },
                statusCallbackMethod: Twilio.Http.HttpMethod.Post
            );

            return Result<string>.Success(call.Sid);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to make outbound call to {ToNumber}", toNumber);
            return Result<string>.Failure($"Failed to make outbound call: {ex.Message}");
        }
    }

    public async Task<Result<string>> SendSmsAsync(string toNumber, string fromNumber, string message, CancellationToken cancellationToken = default)
    {
        try
        {
            var messageResource = await MessageResource.CreateAsync(
                to: new PhoneNumber(toNumber),
                from: new PhoneNumber(fromNumber),
                body: message,
                statusCallback: new Uri($"{_smsWebhookUrl}/status")
            );

            return Result<string>.Success(messageResource.Sid);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send SMS to {ToNumber}", toNumber);
            return Result<string>.Failure($"Failed to send SMS: {ex.Message}");
        }
    }

    public async Task<Result<string>> SendWhatsAppMessageAsync(string toNumber, string fromNumber, string message, CancellationToken cancellationToken = default)
    {
        try
        {
            var messageResource = await MessageResource.CreateAsync(
                to: new PhoneNumber($"whatsapp:{toNumber}"),
                from: new PhoneNumber($"whatsapp:{fromNumber}"),
                body: message,
                statusCallback: new Uri($"{_whatsAppWebhookUrl}/status")
            );

            return Result<string>.Success(messageResource.Sid);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send WhatsApp message to {ToNumber}", toNumber);
            return Result<string>.Failure($"Failed to send WhatsApp message: {ex.Message}");
        }
    }

    public Task<Result<bool>> ValidateRequestAsync(string signature, string url, Dictionary<string, string> parameters)
    {
        try
        {
            var validator = new RequestValidator(_authToken);
            var isValid = validator.Validate(url, parameters, signature);

            return Task.FromResult(Result<bool>.Success(isValid));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate Twilio request");
            return Task.FromResult(Result<bool>.Failure($"Failed to validate request: {ex.Message}"));
        }
    }

    public async Task<Result<string>> GetCallStatusAsync(string callSid, CancellationToken cancellationToken = default)
    {
        try
        {
            var call = await CallResource.FetchAsync(pathSid: callSid);
            return Result<string>.Success(call.Status.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get call status for {CallSid}", callSid);
            return Result<string>.Failure($"Failed to get call status: {ex.Message}");
        }
    }

    public async Task<Result<string>> GetMessageStatusAsync(string messageSid, CancellationToken cancellationToken = default)
    {
        try
        {
            var message = await MessageResource.FetchAsync(pathSid: messageSid);
            return Result<string>.Success(message.Status.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get message status for {MessageSid}", messageSid);
            return Result<string>.Failure($"Failed to get message status: {ex.Message}");
        }
    }

    public async Task<List<AvailableNumberResponse>> SearchAvailableNumbersAsync(
        string countryCode,
        string? areaCode = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var availableNumbers = await Twilio.Rest.Api.V2010.Account.AvailablePhoneNumberCountry.LocalResource.ReadAsync(
                pathCountryCode: countryCode,
                voiceEnabled: true,
                smsEnabled: true,
                areaCode: !string.IsNullOrEmpty(areaCode) ? int.Parse(areaCode) : null,
                limit: 20
            );

            return availableNumbers.Select(n =>
            {
                var phoneStr = n.PhoneNumber?.ToString() ?? string.Empty;
                return new AvailableNumberResponse
                {
                    PhoneNumber = phoneStr,
                    FriendlyName = n.FriendlyName?.ToString() ?? phoneStr,
                    Locality = n.Locality ?? string.Empty,
                    Region = n.Region ?? string.Empty,
                    IsoCountry = n.IsoCountry ?? countryCode,
                    PostalCode = n.PostalCode ?? string.Empty,
                    AddressRequirements = n.Capabilities?.Voice == true ? "none" : "any",
                    MonthlyPrice = GetCountryPricing(countryCode).PhoneNumberMonthly
                };
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search available numbers for country {CountryCode}", countryCode);
            return new List<AvailableNumberResponse>();
        }
    }

    public async Task<TwilioNumberResponse> ProvisionPhoneNumberAsync(
        string phoneNumber,
        string countryCode,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var number = await IncomingPhoneNumberResource.CreateAsync(
                phoneNumber: new PhoneNumber(phoneNumber),
                voiceUrl: new Uri(_voiceWebhookUrl),
                smsUrl: new Uri(_smsWebhookUrl),
                statusCallback: new Uri(_statusCallbackUrl)
            );

            return new TwilioNumberResponse
            {
                PhoneNumber = number.PhoneNumber.ToString(),
                FriendlyName = number.FriendlyName ?? number.PhoneNumber.ToString(),
                Sid = number.Sid,
                AccountSid = number.AccountSid,
                Capabilities = new NumberCapabilities
                {
                    Voice = number.Capabilities?.Voice ?? false,
                    Sms = number.Capabilities?.Sms ?? false,
                    Mms = number.Capabilities?.Mms ?? false
                }
            };
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to provision phone number: {ex.Message}", ex);
        }
    }

    public async Task<bool> EnableWhatsAppForNumberAsync(
        string phoneNumberSid,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await IncomingPhoneNumberResource.UpdateAsync(
                pathSid: phoneNumberSid,
                smsUrl: new Uri(_whatsAppWebhookUrl)
            );

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enable WhatsApp for number {PhoneNumberSid}", phoneNumberSid);
            return false;
        }
    }

    public async Task<bool> ReleasePhoneNumberAsync(
        string phoneNumberSid,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await IncomingPhoneNumberResource.DeleteAsync(pathSid: phoneNumberSid);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to release phone number {PhoneNumberSid}", phoneNumberSid);
            return false;
        }
    }

    public async Task<TwilioPhoneNumberResult?> PurchasePhoneNumberAsync(
        PurchasePhoneNumberOptions options,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var availableNumbers = await Twilio.Rest.Api.V2010.Account.AvailablePhoneNumberCountry.LocalResource.ReadAsync(
                pathCountryCode: options.CountryCode,
                voiceEnabled: options.VoiceEnabled,
                smsEnabled: options.SmsEnabled,
                mmsEnabled: options.MmsEnabled,
                limit: 1
            );

            var firstNumber = availableNumbers.FirstOrDefault();
            if (firstNumber == null)
            {
                throw new TwilioNoNumbersAvailableException(
                    $"No phone numbers available for country: {options.CountryCode}");
            }

            var purchasedNumber = await IncomingPhoneNumberResource.CreateAsync(
                phoneNumber: firstNumber.PhoneNumber,
                voiceUrl: new Uri(_voiceWebhookUrl),
                smsUrl: new Uri(_smsWebhookUrl),
                statusCallback: new Uri(_statusCallbackUrl)
            );

            return new TwilioPhoneNumberResult
            {
                PhoneNumber = purchasedNumber.PhoneNumber.ToString(),
                Sid = purchasedNumber.Sid,
                FriendlyName = purchasedNumber.FriendlyName ?? purchasedNumber.PhoneNumber.ToString(),
                MonthlyCost = GetCountryPricing(options.CountryCode).PhoneNumberMonthly
            };
        }
        catch (Twilio.Exceptions.ApiException ex)
        {
            if (ex.Status == 429)
            {
                throw new TwilioRateLimitException("Twilio rate limit exceeded", ex);
            }
            else if (ex.Status == 503)
            {
                throw new TwilioServiceUnavailableException("Twilio service temporarily unavailable", ex);
            }
            else if (ex.Status == 400 && ex.Message.Contains("compliance"))
            {
                throw new TwilioRegulatoryComplianceException(
                    $"Regulatory compliance required for {options.CountryCode}", ex);
            }
            else if (ex.Status == 404)
            {
                throw new TwilioNoNumbersAvailableException(
                    $"No phone numbers available for country: {options.CountryCode}", ex);
            }
            throw;
        }
        catch (TwilioRateLimitException)
        {
            throw;
        }
        catch (TwilioNoNumbersAvailableException)
        {
            throw;
        }
        catch (TwilioRegulatoryComplianceException)
        {
            throw;
        }
        catch (TwilioServiceUnavailableException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to purchase phone number: {ex.Message}", ex);
        }
    }

    public async Task<List<string>> GetAvailablePhoneNumberCountriesAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var countries = await Twilio.Rest.Api.V2010.Account.AvailablePhoneNumberCountryResource.ReadAsync();

            return countries
                .Select(c => c.CountryCode)
                .Where(code => !string.IsNullOrEmpty(code))
                .ToList();
        }
        catch (Twilio.Exceptions.ApiException ex)
        {
            if (ex.Status == 429)
            {
                throw new TwilioRateLimitException("Twilio rate limit exceeded", ex);
            }
            else if (ex.Status == 503)
            {
                throw new TwilioServiceUnavailableException("Twilio service temporarily unavailable", ex);
            }
            throw;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to get available countries: {ex.Message}", ex);
        }
    }

    public async Task<List<AvailableNumberResponse>> SearchAvailablePhoneNumbersAsync(
        string countryCode,
        int limit = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var availableNumbers = await Twilio.Rest.Api.V2010.Account.AvailablePhoneNumberCountry.LocalResource.ReadAsync(
                pathCountryCode: countryCode,
                voiceEnabled: true,
                smsEnabled: true,
                limit: limit
            );

            return availableNumbers.Select(n => new AvailableNumberResponse
            {
                PhoneNumber = n.PhoneNumber?.ToString() ?? string.Empty,
                FriendlyName = n.FriendlyName?.ToString() ?? n.PhoneNumber?.ToString() ?? string.Empty,
                Locality = n.Locality ?? string.Empty,
                Region = n.Region ?? string.Empty,
                IsoCountry = n.IsoCountry ?? countryCode,
                PostalCode = n.PostalCode ?? string.Empty,
                AddressRequirements = n.Capabilities?.Voice == true ? "none" : "any",
                MonthlyPrice = GetCountryPricing(countryCode).PhoneNumberMonthly
            }).ToList();
        }
        catch (Twilio.Exceptions.ApiException ex)
        {
            if (ex.Status == 429)
            {
                throw new TwilioRateLimitException("Twilio rate limit exceeded", ex);
            }
            else if (ex.Status == 503)
            {
                throw new TwilioServiceUnavailableException("Twilio service temporarily unavailable", ex);
            }
            else if (ex.Status == 404)
            {
                throw new TwilioNoNumbersAvailableException(
                    $"No phone numbers available for country: {countryCode}", ex);
            }
            throw;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to search available phone numbers: {ex.Message}", ex);
        }
    }

    public async Task ConfigureNumberForWhatsAppAsync(
        string phoneNumberSid,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await IncomingPhoneNumberResource.UpdateAsync(
                pathSid: phoneNumberSid,
                smsUrl: new Uri(_whatsAppWebhookUrl),
                statusCallback: new Uri(_statusCallbackUrl)
            );
        }
        catch (Twilio.Exceptions.ApiException ex)
        {
            if (ex.Status == 429)
            {
                throw new TwilioRateLimitException("Twilio rate limit exceeded", ex);
            }
            else if (ex.Status == 503)
            {
                throw new TwilioServiceUnavailableException("Twilio service temporarily unavailable", ex);
            }
            throw;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to configure WhatsApp for number: {ex.Message}", ex);
        }
    }

    public CountryPricingConfig GetCountryPricing(string countryCode)
    {
        if (_countryPricing.TryGetValue(countryCode, out var pricing))
            return pricing;

        // Default US pricing as fallback
        return new CountryPricingConfig
        {
            PhoneNumberMonthly = 1.00m,
            SmsOutbound = 0.0079m,
            VoicePerMinute = 0.0085m,
            WhatsAppPerMessage = 0.005m
        };
    }
}

public class CountryPricingConfig
{
    public decimal PhoneNumberMonthly { get; set; } = 1.00m;
    public decimal SmsOutbound { get; set; } = 0.0079m;
    public decimal VoicePerMinute { get; set; } = 0.0085m;
    public decimal WhatsAppPerMessage { get; set; } = 0.005m;
}
