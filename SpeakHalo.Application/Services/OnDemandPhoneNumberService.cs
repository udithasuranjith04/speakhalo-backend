using Microsoft.Extensions.Logging;
using SpeakHalo.Application.Interfaces;
using SpeakHalo.Application.Exceptions;
using SpeakHalo.Domain.Entities;
using SpeakHalo.Domain.Interfaces.Repositories;

namespace SpeakHalo.Application.Services;

public class OnDemandPhoneNumberService
{
    private readonly ITwilioService _twilioService;
    private readonly IPhoneNumberRepository _phoneNumberRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<OnDemandPhoneNumberService> _logger;
    private const int MAX_RETRIES = 3;

    public OnDemandPhoneNumberService(
        ITwilioService twilioService,
        IPhoneNumberRepository phoneNumberRepo,
        IUnitOfWork unitOfWork,
        ILogger<OnDemandPhoneNumberService> logger)
    {
        _twilioService = twilioService;
        _phoneNumberRepo = phoneNumberRepo;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<(bool Success, PhoneNumber? PhoneNumber, string? ErrorMessage)>
        PurchaseAndAssignNumberAsync(
            string userId,
            string countryCode,
            CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Starting on-demand phone number purchase for user {UserId}, country: {Country}",
            userId, countryCode);

        try
        {
            var existingNumbers = await _phoneNumberRepo.GetByUserIdAsync(userId, cancellationToken);
            if (existingNumbers.Any())
            {
                _logger.LogWarning("User {UserId} already has {Count} phone number(s)", userId, existingNumbers.Count);
                return (false, null, "User already has a phone number assigned");
            }

            TwilioPhoneNumberResult? twilioNumber = null;
            Exception? lastException = null;

            for (int attempt = 1; attempt <= MAX_RETRIES; attempt++)
            {
                try
                {
                    _logger.LogInformation("Calling Twilio API to purchase {Country} number (attempt {Attempt}/{Max})",
                        countryCode, attempt, MAX_RETRIES);

                    twilioNumber = await _twilioService.PurchasePhoneNumberAsync(new PurchasePhoneNumberOptions
                    {
                        CountryCode = countryCode,
                        VoiceEnabled = true,
                        SmsEnabled = true,
                        MmsEnabled = true,
                        Capabilities = PhoneNumberCapabilities.Voice | PhoneNumberCapabilities.SMS
                    }, cancellationToken);

                    break;
                }
                catch (Exception ex) when (IsTransientError(ex) && attempt < MAX_RETRIES)
                {
                    lastException = ex;
                    var delaySeconds = Math.Pow(2, attempt);
                    _logger.LogWarning(ex, "Retry {Attempt}/{Max} after {Delay}s due to transient error",
                        attempt, MAX_RETRIES, delaySeconds);
                    await Task.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken);
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    break;
                }
            }

            if (lastException != null && twilioNumber == null)
            {
                throw lastException;
            }

            if (twilioNumber == null)
            {
                _logger.LogError("Twilio returned null when purchasing {Country} number", countryCode);
                return (false, null, $"No phone numbers available for country: {countryCode}. Please try again or contact support.");
            }

            _logger.LogInformation(
                "Successfully purchased number {Number} (SID: {Sid}) from Twilio",
                twilioNumber.PhoneNumber, twilioNumber.Sid);

            var phoneNumber = new PhoneNumber
            {
                UserId = userId,
                Number = twilioNumber.PhoneNumber,
                TwilioSid = twilioNumber.Sid,
                CountryCode = countryCode,
                FriendlyName = $"Business Number ({countryCode})",
                IsPrimary = true,
                VoiceEnabled = true,
                SmsEnabled = true,
                WhatsAppEnabled = true,
                WhatsAppVerified = false,
                AssignedAt = DateTime.UtcNow,
                AssignmentMethod = "OnDemand",
                IsActive = true
            };

            await _phoneNumberRepo.AddAsync(phoneNumber, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Phone number {Number} successfully assigned to user {UserId}",
                phoneNumber.Number, userId);

            try
            {
                await _twilioService.ConfigureNumberForWhatsAppAsync(twilioNumber.Sid, cancellationToken);
                _logger.LogInformation("WhatsApp configured for number {Number}", phoneNumber.Number);
            }
            catch (Exception ex)
            {
                // WhatsApp configuration is not critical, log and continue
                _logger.LogWarning(ex, "Failed to configure WhatsApp for {Number}, can be done later", phoneNumber.Number);
            }

            return (true, phoneNumber, null);
        }
        catch (TwilioRateLimitException ex)
        {
            _logger.LogError(ex, "Twilio rate limit exceeded for user {UserId}", userId);
            return (false, null, "Too many requests. Please try again in a few moments.");
        }
        catch (TwilioNoNumbersAvailableException ex)
        {
            _logger.LogError(ex, "No {Country} numbers available from Twilio", countryCode);
            return (false, null, $"No phone numbers available for {countryCode}. Please try a different country or contact support.");
        }
        catch (TwilioRegulatoryComplianceException ex)
        {
            _logger.LogError(ex, "Regulatory compliance required for {Country}", countryCode);
            return (false, null, $"Phone numbers in {countryCode} require additional verification. Please contact support.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error purchasing phone number for user {UserId}", userId);
            return (false, null, "An error occurred while setting up your phone number. Please try again or contact support.");
        }
    }

    private bool IsTransientError(Exception ex)
    {
        if (ex is TwilioRateLimitException) return true;
        if (ex is TwilioServiceUnavailableException) return true;
        if (ex is HttpRequestException) return true;
        if (ex is TaskCanceledException) return true;

        return false;
    }

    public async Task<List<string>> GetAvailableCountriesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _twilioService.GetAvailablePhoneNumberCountriesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching available countries from Twilio");
            return new List<string> { "US", "CA", "GB", "AU", "IE" };
        }
    }

    public async Task<bool> AreNumbersAvailableAsync(string countryCode, CancellationToken cancellationToken = default)
    {
        try
        {
            var available = await _twilioService.SearchAvailablePhoneNumbersAsync(
                countryCode,
                limit: 1,
                cancellationToken);

            return available.Any();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking availability for {Country}", countryCode);
            return true;
        }
    }
}
