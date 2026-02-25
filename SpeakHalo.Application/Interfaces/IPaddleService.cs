using SpeakHalo.Application.DTOs.Payment;
using SpeakHalo.Shared.Models;

namespace SpeakHalo.Application.Interfaces;

public interface IPaddleService
{
    Task<Result<PaddleCustomer>> CreateCustomerAsync(string email, string? name = null, CancellationToken cancellationToken = default);
    Task<Result<PaddleCustomer>> GetCustomerAsync(string customerId, CancellationToken cancellationToken = default);
    Task<Result<PaddleTransaction>> CreateCheckoutTransactionAsync(string priceId, string? customerId = null, string? customerEmail = null, CancellationToken cancellationToken = default);
    Task<Result<PaddleSubscription>> GetSubscriptionAsync(string subscriptionId, CancellationToken cancellationToken = default);
    Task<Result> CancelSubscriptionAsync(string subscriptionId, bool effectiveImmediately = false, CancellationToken cancellationToken = default);
    Task<Result<PaddleSubscription>> UpdateSubscriptionAsync(string subscriptionId, string newPriceId, CancellationToken cancellationToken = default);
    Task<Result<string>> GetSubscriptionUpdatePaymentMethodUrlAsync(string subscriptionId, CancellationToken cancellationToken = default);
    Task<Result<PaddleAdjustment>> CreateRefundAdjustmentAsync(string transactionId, string reason, CancellationToken cancellationToken = default);
}
