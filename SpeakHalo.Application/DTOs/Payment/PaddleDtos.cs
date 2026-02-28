using System.Text.Json.Serialization;

namespace SpeakHalo.Application.DTOs.Payment;

// --- Paddle API DTOs ---

public class PaddleCustomer
{
    [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;
    [JsonPropertyName("email")] public string Email { get; set; } = string.Empty;
    [JsonPropertyName("name")] public string? Name { get; set; }
}

public class PaddleSubscription
{
    [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;
    [JsonPropertyName("status")] public string Status { get; set; } = string.Empty;
    [JsonPropertyName("customer_id")] public string CustomerId { get; set; } = string.Empty;
    [JsonPropertyName("created_at")] public DateTime CreatedAt { get; set; }
    [JsonPropertyName("canceled_at")] public DateTime? CanceledAt { get; set; }
    [JsonPropertyName("current_billing_period")] public PaddleBillingPeriod? CurrentBillingPeriod { get; set; }
    [JsonPropertyName("items")] public List<PaddleSubscriptionItem>? Items { get; set; }
    [JsonPropertyName("management_urls")] public PaddleManagementUrls? ManagementUrls { get; set; }
}

public class PaddleBillingPeriod
{
    [JsonPropertyName("starts_at")] public DateTime StartsAt { get; set; }
    [JsonPropertyName("ends_at")] public DateTime EndsAt { get; set; }
}

public class PaddleSubscriptionItem
{
    [JsonPropertyName("price")] public PaddlePrice? Price { get; set; }
}

public class PaddlePrice
{
    [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;
    [JsonPropertyName("product_id")] public string ProductId { get; set; } = string.Empty;
}

public class PaddleManagementUrls
{
    [JsonPropertyName("update_payment_method")] public string? UpdatePaymentMethod { get; set; }
    [JsonPropertyName("cancel")] public string? Cancel { get; set; }
}

public class PaddleTransaction
{
    [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;
    [JsonPropertyName("status")] public string Status { get; set; } = string.Empty;
    [JsonPropertyName("checkout")] public PaddleCheckout? Checkout { get; set; }
}

public class PaddleCheckout
{
    [JsonPropertyName("url")] public string? Url { get; set; }
}

public class PaddleAdjustment
{
    [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;
    [JsonPropertyName("status")] public string Status { get; set; } = string.Empty;
}

public class PaddleApiResponse<T>
{
    [JsonPropertyName("data")] public T? Data { get; set; }
    [JsonPropertyName("error")] public PaddleApiError? Error { get; set; }
}

public class PaddleApiError
{
    [JsonPropertyName("type")] public string Type { get; set; } = string.Empty;
    [JsonPropertyName("code")] public string Code { get; set; } = string.Empty;
    [JsonPropertyName("detail")] public string Detail { get; set; } = string.Empty;
}

// --- Application DTOs ---

public class CreateCheckoutRequest
{
    public string PricingPlanId { get; set; } = string.Empty;
}

public class PublicCheckoutRequest
{
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string BusinessName { get; set; } = string.Empty;
    public string PricingPlanId { get; set; } = string.Empty;
    public string? Country { get; set; }
    public string? ForwardingNumber { get; set; }
}

public class CreateCheckoutResponse
{
    public string TransactionId { get; set; } = string.Empty;
    public string? CheckoutUrl { get; set; }
}

public class BillingInfoResponse
{
    public string? PlanName { get; set; }
    public decimal? PlanPrice { get; set; }
    public string? PlanTierSlug { get; set; }
    public string? Status { get; set; }
    public bool IsOnTrial { get; set; }
    public DateTime? TrialEndsAt { get; set; }
    public int? TrialDaysRemaining { get; set; }
    public DateTime? NextBillingDate { get; set; }
    public DateTime? FirstPaymentAt { get; set; }
    public bool RefundEligible { get; set; }
    public string? PaddleSubscriptionStatus { get; set; }
    public string? UpdatePaymentMethodUrl { get; set; }
    public string? CancelUrl { get; set; }
    public DateTime? CanceledAt { get; set; }
    public DateTime? RefundedAt { get; set; }
}

public class PublicPlanResponse
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal DisplayPrice { get; set; }
    public string? TierSlug { get; set; }
    public string? PaddlePriceId { get; set; }
    public int AIMinutesIncluded { get; set; }
    public int SMSMessagesIncluded { get; set; }
    public int WhatsAppMessagesIncluded { get; set; }
    public int ImagesIncluded { get; set; }
    public int MaxPhoneNumbers { get; set; }
    public decimal AIOverageRate { get; set; }
    public decimal SMSOverageRate { get; set; }
    public decimal WhatsAppOverageRate { get; set; }
    public decimal ImageOverageRate { get; set; }
    public object? EnabledFeatures { get; set; }
    public int SortOrder { get; set; }
}

public class CancelSubscriptionRequest
{
    public bool EffectiveImmediately { get; set; } = false;
}

public class RefundRequest
{
    public string Reason { get; set; } = string.Empty;
}

public class AdminRefundRequest
{
    public string Reason { get; set; } = string.Empty;
}

public class RegisterTrialRequest
{
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string BusinessName { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string? Country { get; set; }
    public string? ForwardingNumber { get; set; }
}
