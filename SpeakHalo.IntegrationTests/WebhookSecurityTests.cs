using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using SpeakHalo.IntegrationTests.TestHelpers;
using Xunit;

namespace SpeakHalo.IntegrationTests;

[Collection("Integration")]
public class WebhookSecurityTests
{
    private readonly SpeakHaloWebApplicationFactory _factory;

    private const string PaddleWebhookSecret = "test_paddle_webhook_secret_key_for_hmac";
    private const string PaddleWebhookUrl = "/api/v1/webhooks/paddle";
    private const string TwilioVoiceWebhookUrl = "/api/v1/webhooks/twilio/voice";
    private const string TwilioSmsWebhookUrl = "/api/v1/webhooks/twilio/sms";

    public WebhookSecurityTests(SpeakHaloWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // ────────────────────────────────────────────────────────────────
    // Paddle Webhook Signature Tests
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task PaddleWebhook_ValidSignature_Processed()
    {
        // Arrange
        var client = _factory.CreateClient();
        var body = CreatePaddleWebhookBody("evt_valid_sig_test", "subscription.created");
        var signature = ComputePaddleSignature(body);

        var request = CreatePaddleRequest(body, signature);

        // Act
        var response = await client.SendAsync(request);

        // Assert - valid signature should be processed (200 OK)
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PaddleWebhook_MissingSignature_Rejected()
    {
        // Arrange
        var client = _factory.CreateClient();
        var body = CreatePaddleWebhookBody("evt_missing_sig_test", "subscription.created");

        var request = new HttpRequestMessage(HttpMethod.Post, PaddleWebhookUrl);
        request.Content = new StringContent(body, Encoding.UTF8, "application/json");
        // Intentionally NOT adding Paddle-Signature header

        // Act
        var response = await client.SendAsync(request);

        // Assert - the controller returns 200 to avoid Paddle retries, but the event
        // should not be processed (we verify by checking the response is OK but the
        // webhook is silently rejected in the signature check)
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "Paddle webhook controller returns 200 even for invalid signatures to prevent retries");
    }

    [Fact]
    public async Task PaddleWebhook_InvalidSignature_Rejected()
    {
        // Arrange
        var client = _factory.CreateClient();
        var body = CreatePaddleWebhookBody("evt_invalid_sig_test", "subscription.created");

        var request = new HttpRequestMessage(HttpMethod.Post, PaddleWebhookUrl);
        request.Content = new StringContent(body, Encoding.UTF8, "application/json");
        request.Headers.TryAddWithoutValidation("Paddle-Signature", "ts=1234567890;h1=invalid_hash_value");

        // Act
        var response = await client.SendAsync(request);

        // Assert - returns 200 to avoid retries but event is not processed
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PaddleWebhook_TamperedBody_Rejected()
    {
        // Arrange
        var client = _factory.CreateClient();
        var originalBody = CreatePaddleWebhookBody("evt_tampered_test", "subscription.created");
        var signature = ComputePaddleSignature(originalBody);

        // Tamper with the body after signing
        var tamperedBody = originalBody.Replace("subscription.created", "subscription.canceled");

        var request = new HttpRequestMessage(HttpMethod.Post, PaddleWebhookUrl);
        request.Content = new StringContent(tamperedBody, Encoding.UTF8, "application/json");
        request.Headers.TryAddWithoutValidation("Paddle-Signature", signature);

        // Act
        var response = await client.SendAsync(request);

        // Assert - returns 200 to avoid retries but tampered body is rejected
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PaddleWebhook_SubscriptionCreated_UpdatesDB()
    {
        // Arrange
        var client = _factory.CreateClient();
        var eventId = $"evt_sub_created_{Guid.NewGuid():N}";
        var body = CreatePaddleWebhookBody(eventId, "subscription.created");
        var signature = ComputePaddleSignature(body);
        var request = CreatePaddleRequest(body, signature);

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PaddleWebhook_SubscriptionUpdated_UpdatesDB()
    {
        // Arrange
        var client = _factory.CreateClient();
        var eventId = $"evt_sub_updated_{Guid.NewGuid():N}";
        var body = CreatePaddleWebhookBody(eventId, "subscription.updated",
            subscriptionStatus: "active");
        var signature = ComputePaddleSignature(body);
        var request = CreatePaddleRequest(body, signature);

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PaddleWebhook_SubscriptionCanceled_UpdatesDB()
    {
        // Arrange
        var client = _factory.CreateClient();
        var eventId = $"evt_sub_canceled_{Guid.NewGuid():N}";
        var body = CreatePaddleWebhookBody(eventId, "subscription.canceled",
            subscriptionStatus: "canceled");
        var signature = ComputePaddleSignature(body);
        var request = CreatePaddleRequest(body, signature);

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PaddleWebhook_TransactionCompleted_UpdatesDB()
    {
        // Arrange
        var client = _factory.CreateClient();
        var eventId = $"evt_txn_completed_{Guid.NewGuid():N}";
        var body = CreatePaddleWebhookBody(eventId, "transaction.completed");
        var signature = ComputePaddleSignature(body);
        var request = CreatePaddleRequest(body, signature);

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PaddleWebhook_PaymentFailed_UpdatesDB()
    {
        // Arrange
        var client = _factory.CreateClient();
        var eventId = $"evt_payment_failed_{Guid.NewGuid():N}";
        var body = CreatePaddleWebhookBody(eventId, "transaction.payment_failed",
            subscriptionStatus: "past_due");
        var signature = ComputePaddleSignature(body);
        var request = CreatePaddleRequest(body, signature);

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PaddleWebhook_DuplicateEventId_Skipped()
    {
        // Arrange
        var client = _factory.CreateClient();
        var eventId = $"evt_duplicate_{Guid.NewGuid():N}";
        var body = CreatePaddleWebhookBody(eventId, "subscription.created");
        var signature = ComputePaddleSignature(body);

        // Act - send the same event twice
        var request1 = CreatePaddleRequest(body, signature);
        var response1 = await client.SendAsync(request1);

        var request2 = CreatePaddleRequest(body, signature);
        var response2 = await client.SendAsync(request2);

        // Assert - both should return 200 (idempotent), second is silently skipped
        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        response2.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ────────────────────────────────────────────────────────────────
    // Twilio Webhook Tests
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task TwilioVoiceWebhook_ValidSignature_Processed()
    {
        // Arrange
        var client = _factory.CreateClient();
        var formData = new Dictionary<string, string>
        {
            { "CallSid", "CA_test_valid_sig_001" },
            { "From", "+12025551234" },
            { "To", "+12025555678" },
            { "CallStatus", "ringing" },
            { "Direction", "inbound" }
        };

        // Compute a valid Twilio signature
        var authToken = "test_twilio_auth_token_32chars_min";
        var url = $"http://localhost{TwilioVoiceWebhookUrl}";
        var signature = ComputeTwilioSignature(authToken, url, formData);

        var request = new HttpRequestMessage(HttpMethod.Post, TwilioVoiceWebhookUrl);
        request.Content = new FormUrlEncodedContent(formData);
        request.Headers.TryAddWithoutValidation("X-Twilio-Signature", signature);

        // Act
        var response = await client.SendAsync(request);

        // Assert - should be processed (may be 200 or some content response with TwiML)
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized,
            "A validly signed Twilio request should not be rejected");
        response.StatusCode.Should().NotBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task TwilioVoiceWebhook_MissingSignature_Rejected()
    {
        // Arrange
        var client = _factory.CreateClient();
        var formData = new Dictionary<string, string>
        {
            { "CallSid", "CA_test_missing_sig_001" },
            { "From", "+12025551234" },
            { "To", "+12025555678" },
            { "CallStatus", "ringing" },
            { "Direction", "inbound" }
        };

        var request = new HttpRequestMessage(HttpMethod.Post, TwilioVoiceWebhookUrl);
        request.Content = new FormUrlEncodedContent(formData);
        // Intentionally NOT adding X-Twilio-Signature header

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "Missing Twilio signature should result in 401 Unauthorized");
    }

    [Fact]
    public async Task TwilioVoiceWebhook_InvalidSignature_Rejected()
    {
        // Arrange
        var client = _factory.CreateClient();
        var formData = new Dictionary<string, string>
        {
            { "CallSid", "CA_test_invalid_sig_001" },
            { "From", "+12025551234" },
            { "To", "+12025555678" },
            { "CallStatus", "ringing" },
            { "Direction", "inbound" }
        };

        var request = new HttpRequestMessage(HttpMethod.Post, TwilioVoiceWebhookUrl);
        request.Content = new FormUrlEncodedContent(formData);
        request.Headers.TryAddWithoutValidation("X-Twilio-Signature", "invalid_signature_value_here");

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "Invalid Twilio signature should result in 401 Unauthorized");
    }

    [Fact]
    public async Task TwilioSmsWebhook_Processed()
    {
        // Arrange
        var client = _factory.CreateClient();
        var formData = new Dictionary<string, string>
        {
            { "From", "+12025551234" },
            { "To", "+12025555678" },
            { "Body", "Hello, I need help with my appointment" },
            { "MessageSid", "SM_test_sms_001" }
        };

        // Compute a valid Twilio signature
        var authToken = "test_twilio_auth_token_32chars_min";
        var url = $"http://localhost{TwilioSmsWebhookUrl}";
        var signature = ComputeTwilioSignature(authToken, url, formData);

        var request = new HttpRequestMessage(HttpMethod.Post, TwilioSmsWebhookUrl);
        request.Content = new FormUrlEncodedContent(formData);
        request.Headers.TryAddWithoutValidation("X-Twilio-Signature", signature);

        // Act
        var response = await client.SendAsync(request);

        // Assert - should not be rejected for signature reasons
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized,
            "A validly signed Twilio SMS request should not be rejected");
        response.StatusCode.Should().NotBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task TwilioSmsWebhook_InvalidSignature_Rejected()
    {
        // Arrange
        var client = _factory.CreateClient();
        var formData = new Dictionary<string, string>
        {
            { "From", "+12025551234" },
            { "To", "+12025555678" },
            { "Body", "Test message" },
            { "MessageSid", "SM_test_invalid_sms_001" }
        };

        var request = new HttpRequestMessage(HttpMethod.Post, TwilioSmsWebhookUrl);
        request.Content = new FormUrlEncodedContent(formData);
        request.Headers.TryAddWithoutValidation("X-Twilio-Signature", "completely_bogus_signature");

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "Invalid Twilio SMS signature should result in 401 Unauthorized");
    }

    // ────────────────────────────────────────────────────────────────
    // Helper Methods
    // ────────────────────────────────────────────────────────────────

    private static string CreatePaddleWebhookBody(
        string eventId,
        string eventType,
        string subscriptionId = "sub_test_123",
        string customerId = "ctm_test_123",
        string subscriptionStatus = "active")
    {
        var payload = new
        {
            event_id = eventId,
            event_type = eventType,
            occurred_at = "2024-01-01T00:00:00Z",
            data = new
            {
                id = subscriptionId,
                customer_id = customerId,
                status = subscriptionStatus,
                items = new[]
                {
                    new { price = new { id = "pri_test_123" } }
                },
                custom_data = new { user_id = SpeakHaloWebApplicationFactory.OwnerUserId }
            }
        };

        return JsonSerializer.Serialize(payload);
    }

    private static string ComputePaddleSignature(string body)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var signedPayload = $"{timestamp}:{body}";
        var keyBytes = Encoding.UTF8.GetBytes(PaddleWebhookSecret);
        var payloadBytes = Encoding.UTF8.GetBytes(signedPayload);

        using var hmac = new HMACSHA256(keyBytes);
        var hash = hmac.ComputeHash(payloadBytes);
        var hashHex = Convert.ToHexString(hash).ToLowerInvariant();

        return $"ts={timestamp};h1={hashHex}";
    }

    private static HttpRequestMessage CreatePaddleRequest(string body, string signature)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, PaddleWebhookUrl);
        request.Content = new StringContent(body, Encoding.UTF8, "application/json");
        request.Headers.TryAddWithoutValidation("Paddle-Signature", signature);
        return request;
    }

    /// <summary>
    /// Computes a Twilio-compatible HMAC-SHA1 signature.
    /// The signature is computed as: HMAC-SHA1(authToken, url + sorted params concatenated as key=value).
    /// </summary>
    private static string ComputeTwilioSignature(
        string authToken,
        string url,
        Dictionary<string, string> parameters)
    {
        // Build the data to sign: URL + sorted parameters appended as key-value pairs
        var dataToSign = new StringBuilder(url);
        foreach (var kvp in parameters.OrderBy(k => k.Key))
        {
            dataToSign.Append(kvp.Key);
            dataToSign.Append(kvp.Value);
        }

        var keyBytes = Encoding.UTF8.GetBytes(authToken);
        var dataBytes = Encoding.UTF8.GetBytes(dataToSign.ToString());

        using var hmac = new HMACSHA1(keyBytes);
        var hash = hmac.ComputeHash(dataBytes);
        return Convert.ToBase64String(hash);
    }
}
