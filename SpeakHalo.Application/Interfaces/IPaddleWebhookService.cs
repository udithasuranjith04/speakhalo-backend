using System.Text.Json;

namespace SpeakHalo.Application.Interfaces;

public interface IPaddleWebhookService
{
    Task HandleSubscriptionCreatedAsync(JsonElement data, CancellationToken cancellationToken = default);
    Task HandleSubscriptionUpdatedAsync(JsonElement data, CancellationToken cancellationToken = default);
    Task HandleSubscriptionCanceledAsync(JsonElement data, CancellationToken cancellationToken = default);
    Task HandleTransactionCompletedAsync(JsonElement data, CancellationToken cancellationToken = default);
    Task HandleTransactionPaymentFailedAsync(JsonElement data, CancellationToken cancellationToken = default);
}
