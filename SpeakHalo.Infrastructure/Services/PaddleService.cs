using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SpeakHalo.Application.DTOs.Payment;
using SpeakHalo.Application.Interfaces;
using SpeakHalo.Shared.Models;

namespace SpeakHalo.Infrastructure.Services;

public class PaddleService : IPaddleService
{
    private readonly HttpClient _httpClient;
    private readonly PaddleSettings _settings;
    private readonly ILogger<PaddleService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true
    };

    public PaddleService(
        HttpClient httpClient,
        IOptions<PaddleSettings> settings,
        ILogger<PaddleService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;

        _httpClient.BaseAddress = new Uri(_settings.ApiUrl);
        _httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _settings.ApiKey);
    }

    public async Task<Result<PaddleCustomer>> CreateCustomerAsync(string email, string? name = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = new { email, name };
            var response = await _httpClient.PostAsJsonAsync("/customers", payload, JsonOptions, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Paddle CreateCustomer failed: {Status} {Body}", response.StatusCode, body);
                return Result<PaddleCustomer>.Failure($"Paddle API error: {response.StatusCode}");
            }

            var result = JsonSerializer.Deserialize<PaddleApiResponse<PaddleCustomer>>(body, JsonOptions);
            if (result?.Data == null)
                return Result<PaddleCustomer>.Failure("Invalid Paddle response");

            return Result<PaddleCustomer>.Success(result.Data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create Paddle customer for {Email}", email);
            return Result<PaddleCustomer>.Failure("Failed to create Paddle customer");
        }
    }

    public async Task<Result<PaddleCustomer>> GetCustomerAsync(string customerId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/customers/{customerId}", cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
                return Result<PaddleCustomer>.Failure($"Paddle API error: {response.StatusCode}");

            var result = JsonSerializer.Deserialize<PaddleApiResponse<PaddleCustomer>>(body, JsonOptions);
            return result?.Data != null
                ? Result<PaddleCustomer>.Success(result.Data)
                : Result<PaddleCustomer>.Failure("Invalid Paddle response");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get Paddle customer {CustomerId}", customerId);
            return Result<PaddleCustomer>.Failure("Failed to get Paddle customer");
        }
    }

    public async Task<Result<PaddleTransaction>> CreateCheckoutTransactionAsync(
        string priceId, string? customerId = null, string? customerEmail = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = new Dictionary<string, object>
            {
                ["items"] = new[] { new { price_id = priceId, quantity = 1 } }
            };

            if (!string.IsNullOrEmpty(customerId))
                payload["customer_id"] = customerId;

            if (!string.IsNullOrEmpty(customerEmail))
                payload["customer"] = new { email = customerEmail };

            var response = await _httpClient.PostAsJsonAsync("/transactions", payload, JsonOptions, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Paddle CreateTransaction failed: {Status} {Body}", response.StatusCode, body);
                return Result<PaddleTransaction>.Failure($"Paddle API error: {response.StatusCode}");
            }

            var result = JsonSerializer.Deserialize<PaddleApiResponse<PaddleTransaction>>(body, JsonOptions);
            if (result?.Data == null)
                return Result<PaddleTransaction>.Failure("Invalid Paddle response");

            return Result<PaddleTransaction>.Success(result.Data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create Paddle checkout transaction");
            return Result<PaddleTransaction>.Failure("Failed to create checkout");
        }
    }

    public async Task<Result<PaddleSubscription>> GetSubscriptionAsync(string subscriptionId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/subscriptions/{subscriptionId}", cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
                return Result<PaddleSubscription>.Failure($"Paddle API error: {response.StatusCode}");

            var result = JsonSerializer.Deserialize<PaddleApiResponse<PaddleSubscription>>(body, JsonOptions);
            return result?.Data != null
                ? Result<PaddleSubscription>.Success(result.Data)
                : Result<PaddleSubscription>.Failure("Invalid Paddle response");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get Paddle subscription {SubscriptionId}", subscriptionId);
            return Result<PaddleSubscription>.Failure("Failed to get subscription");
        }
    }

    public async Task<Result> CancelSubscriptionAsync(string subscriptionId, bool effectiveImmediately = false, CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = new { effective_from = effectiveImmediately ? "immediately" : "next_billing_period" };
            var response = await _httpClient.PostAsJsonAsync($"/subscriptions/{subscriptionId}/cancel", payload, JsonOptions, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Paddle CancelSubscription failed: {Status} {Body}", response.StatusCode, body);
                return Result.Failure($"Paddle API error: {response.StatusCode}");
            }

            return Result.Success("Subscription canceled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cancel Paddle subscription {SubscriptionId}", subscriptionId);
            return Result.Failure("Failed to cancel subscription");
        }
    }

    public async Task<Result<PaddleSubscription>> UpdateSubscriptionAsync(string subscriptionId, string newPriceId, CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = new { items = new[] { new { price_id = newPriceId, quantity = 1 } } };
            var request = new HttpRequestMessage(HttpMethod.Patch, $"/subscriptions/{subscriptionId}")
            {
                Content = JsonContent.Create(payload, options: JsonOptions)
            };
            var response = await _httpClient.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Paddle UpdateSubscription failed: {Status} {Body}", response.StatusCode, body);
                return Result<PaddleSubscription>.Failure($"Paddle API error: {response.StatusCode}");
            }

            var result = JsonSerializer.Deserialize<PaddleApiResponse<PaddleSubscription>>(body, JsonOptions);
            return result?.Data != null
                ? Result<PaddleSubscription>.Success(result.Data)
                : Result<PaddleSubscription>.Failure("Invalid Paddle response");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update Paddle subscription {SubscriptionId}", subscriptionId);
            return Result<PaddleSubscription>.Failure("Failed to update subscription");
        }
    }

    public async Task<Result<string>> GetSubscriptionUpdatePaymentMethodUrlAsync(string subscriptionId, CancellationToken cancellationToken = default)
    {
        var result = await GetSubscriptionAsync(subscriptionId, cancellationToken);
        if (!result.IsSuccess || result.Data == null)
            return Result<string>.Failure("Failed to get subscription");

        var url = result.Data.ManagementUrls?.UpdatePaymentMethod;
        return !string.IsNullOrEmpty(url)
            ? Result<string>.Success(url)
            : Result<string>.Failure("Update payment method URL not available");
    }

    public async Task<Result<PaddleAdjustment>> CreateRefundAdjustmentAsync(string transactionId, string reason, CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = new
            {
                action = "refund",
                transaction_id = transactionId,
                reason
            };

            var response = await _httpClient.PostAsJsonAsync("/adjustments", payload, JsonOptions, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Paddle CreateRefund failed: {Status} {Body}", response.StatusCode, body);
                return Result<PaddleAdjustment>.Failure($"Paddle API error: {response.StatusCode}");
            }

            var result = JsonSerializer.Deserialize<PaddleApiResponse<PaddleAdjustment>>(body, JsonOptions);
            return result?.Data != null
                ? Result<PaddleAdjustment>.Success(result.Data)
                : Result<PaddleAdjustment>.Failure("Invalid Paddle response");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create refund for transaction {TransactionId}", transactionId);
            return Result<PaddleAdjustment>.Failure("Failed to create refund");
        }
    }
}
