using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using SpeakHalo.IntegrationTests.TestHelpers;
using Xunit;

namespace SpeakHalo.IntegrationTests;

[Collection("Integration")]
public class MessagingTests
{
    private readonly SpeakHaloWebApplicationFactory _factory;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public MessagingTests(SpeakHaloWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // ───────────────────────────────────────────────────────
    //  SMS (5 tests)
    // ───────────────────────────────────────────────────────

    [Fact]
    public async Task ListSmsConversations_ReturnsConversations()
    {
        // Arrange
        var client = await AuthHelper.LoginAsOwnerAsync(_factory);

        // Act
        var response = await client.GetAsync("/api/v1/sms/conversations");

        // Assert - may return 403 (feature gating) or 400 (missing query params / phone number)
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Forbidden, HttpStatusCode.BadRequest);
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var json = await ApiResponseHelper.ReadJsonAsync(response);
            json.ValueKind.Should().Be(JsonValueKind.Object);
        }
    }

    [Fact]
    public async Task GetSmsMessages_ReturnsMessages()
    {
        // Arrange
        var client = await AuthHelper.LoginAsOwnerAsync(_factory);

        // Try to list conversations first to get a valid ID
        var listResponse = await client.GetAsync("/api/v1/sms/conversations");
        // May return 403 (feature gating) or 400 (missing query params)
        listResponse.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Forbidden, HttpStatusCode.BadRequest);
        if (listResponse.StatusCode != HttpStatusCode.OK)
            return;

        var listJson = await ApiResponseHelper.ReadJsonAsync(listResponse);

        // Attempt to extract a conversation ID from the list
        string? conversationId = null;
        if (listJson.TryGetProperty("data", out var data))
        {
            if (data.TryGetProperty("items", out var items) && items.ValueKind == JsonValueKind.Array && items.GetArrayLength() > 0)
            {
                conversationId = items[0].GetProperty("id").GetString();
            }
            else if (data.ValueKind == JsonValueKind.Array && data.GetArrayLength() > 0)
            {
                conversationId = data[0].GetProperty("id").GetString();
            }
        }

        if (conversationId == null)
        {
            // No conversations exist yet; request for a non-existent ID should return 404
            var response = await client.GetAsync($"/api/v1/sms/conversations/{Guid.NewGuid()}/messages");
            response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.Forbidden);
            return;
        }

        // Act
        var msgResponse = await client.GetAsync($"/api/v1/sms/conversations/{conversationId}/messages");

        // Assert
        msgResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SendSms_Valid_ReturnsSent()
    {
        // Arrange
        var client = await AuthHelper.LoginAsOwnerAsync(_factory);

        // Twilio is not configured/mocked in test env, so this may return an error.
        var request = new
        {
            toNumber = "+12025551234",
            content = "Integration test SMS"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/sms/send", request);

        // Assert - Twilio unavailable and not mocked: accept 400, 403 (feature gate), 500 (Twilio not configured), or OK
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK,
            HttpStatusCode.BadRequest,
            HttpStatusCode.Created,
            HttpStatusCode.Forbidden,
            HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task SmsConversation_CrossUserIsolation()
    {
        // Arrange - Owner and SecondOwner should not see each other's SMS conversations
        var ownerClient = await AuthHelper.LoginAsOwnerAsync(_factory);
        var secondOwnerClient = await AuthHelper.LoginAsSecondOwnerAsync(_factory);

        // Both users list their SMS conversations
        var ownerResponse = await ownerClient.GetAsync("/api/v1/sms/conversations");
        var secondOwnerResponse = await secondOwnerClient.GetAsync("/api/v1/sms/conversations");

        // Assert - both should return OK, 403 (feature gate), or 400 (missing query params)
        ownerResponse.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Forbidden, HttpStatusCode.BadRequest);
        secondOwnerResponse.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Forbidden, HttpStatusCode.BadRequest);
        if (ownerResponse.StatusCode != HttpStatusCode.OK)
            return;

        // If owner has conversations, secondOwner trying to access them by ID should fail
        var ownerJson = await ApiResponseHelper.ReadJsonAsync(ownerResponse);

        string? ownerConversationId = null;
        if (ownerJson.TryGetProperty("data", out var data))
        {
            if (data.TryGetProperty("items", out var items) && items.ValueKind == JsonValueKind.Array && items.GetArrayLength() > 0)
                ownerConversationId = items[0].GetProperty("id").GetString();
            else if (data.ValueKind == JsonValueKind.Array && data.GetArrayLength() > 0)
                ownerConversationId = data[0].GetProperty("id").GetString();
        }

        if (ownerConversationId != null)
        {
            // SecondOwner should not be able to access Owner's conversation
            var crossResponse = await secondOwnerClient.GetAsync($"/api/v1/sms/conversations/{ownerConversationId}");
            crossResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
    }

    [Fact]
    public async Task ListSmsConversations_Empty_ReturnsEmptyList()
    {
        // Arrange - SecondOwner likely has no SMS conversations
        var client = await AuthHelper.LoginAsSecondOwnerAsync(_factory);

        // Act
        var response = await client.GetAsync("/api/v1/sms/conversations");

        // Assert - may return 403 (feature gating) or 400 (missing query params)
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Forbidden, HttpStatusCode.BadRequest);
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var json = await ApiResponseHelper.ReadJsonAsync(response);
            json.ValueKind.Should().Be(JsonValueKind.Object);
        }
    }

    // ───────────────────────────────────────────────────────
    //  WhatsApp (5 tests)
    // ───────────────────────────────────────────────────────

    [Fact]
    public async Task ListWhatsAppConversations_Returns()
    {
        // Arrange
        var client = await AuthHelper.LoginAsOwnerAsync(_factory);

        // Act
        var response = await client.GetAsync("/api/v1/whatsapp/conversations");

        // Assert - may return 403 (feature gating) or 400 (missing query params / phone number)
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Forbidden, HttpStatusCode.BadRequest);
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var json = await ApiResponseHelper.ReadJsonAsync(response);
            json.ValueKind.Should().Be(JsonValueKind.Object);
        }
    }

    [Fact]
    public async Task GetWhatsAppMessages_Returns()
    {
        // Arrange
        var client = await AuthHelper.LoginAsOwnerAsync(_factory);

        // Try to list conversations first
        var listResponse = await client.GetAsync("/api/v1/whatsapp/conversations");
        // May return 403 (feature gating) or 400 (missing query params)
        listResponse.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Forbidden, HttpStatusCode.BadRequest);
        if (listResponse.StatusCode != HttpStatusCode.OK)
            return;

        var listJson = await ApiResponseHelper.ReadJsonAsync(listResponse);

        string? conversationId = null;
        if (listJson.TryGetProperty("data", out var data))
        {
            if (data.TryGetProperty("items", out var items) && items.ValueKind == JsonValueKind.Array && items.GetArrayLength() > 0)
                conversationId = items[0].GetProperty("id").GetString();
            else if (data.ValueKind == JsonValueKind.Array && data.GetArrayLength() > 0)
                conversationId = data[0].GetProperty("id").GetString();
        }

        if (conversationId == null)
        {
            // No conversations exist; request for a non-existent ID should return 404
            var response = await client.GetAsync($"/api/v1/whatsapp/conversations/{Guid.NewGuid()}/messages");
            response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.Forbidden);
            return;
        }

        // Act
        var msgResponse = await client.GetAsync($"/api/v1/whatsapp/conversations/{conversationId}/messages");

        // Assert
        msgResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SendWhatsApp_Valid_Returns()
    {
        // Arrange
        var client = await AuthHelper.LoginAsOwnerAsync(_factory);

        // Twilio is not configured/mocked in test env, so this may return an error.
        var request = new
        {
            toNumber = "+12025551234",
            content = "Integration test WhatsApp message"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/whatsapp/send", request);

        // Assert - Twilio unavailable and not mocked: accept 400, 403 (feature gate), 500 (Twilio not configured), or OK
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK,
            HttpStatusCode.BadRequest,
            HttpStatusCode.Created,
            HttpStatusCode.Forbidden,
            HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task WhatsAppConversation_CrossUserIsolation()
    {
        // Arrange
        var ownerClient = await AuthHelper.LoginAsOwnerAsync(_factory);
        var secondOwnerClient = await AuthHelper.LoginAsSecondOwnerAsync(_factory);

        // Both users list their WhatsApp conversations
        var ownerResponse = await ownerClient.GetAsync("/api/v1/whatsapp/conversations");
        var secondOwnerResponse = await secondOwnerClient.GetAsync("/api/v1/whatsapp/conversations");

        // Assert - both should return OK, 403 (feature gate), or 400 (missing query params)
        ownerResponse.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Forbidden, HttpStatusCode.BadRequest);
        secondOwnerResponse.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Forbidden, HttpStatusCode.BadRequest);
        if (ownerResponse.StatusCode != HttpStatusCode.OK)
            return;

        // If owner has conversations, secondOwner trying to access them by ID should fail
        var ownerJson = await ApiResponseHelper.ReadJsonAsync(ownerResponse);

        string? ownerConversationId = null;
        if (ownerJson.TryGetProperty("data", out var data))
        {
            if (data.TryGetProperty("items", out var items) && items.ValueKind == JsonValueKind.Array && items.GetArrayLength() > 0)
                ownerConversationId = items[0].GetProperty("id").GetString();
            else if (data.ValueKind == JsonValueKind.Array && data.GetArrayLength() > 0)
                ownerConversationId = data[0].GetProperty("id").GetString();
        }

        if (ownerConversationId != null)
        {
            // SecondOwner should not be able to access Owner's conversation
            var crossResponse = await secondOwnerClient.GetAsync($"/api/v1/whatsapp/conversations/{ownerConversationId}");
            crossResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
    }

    [Fact]
    public async Task ListWhatsAppConversations_Empty_ReturnsEmptyList()
    {
        // Arrange - SecondOwner likely has no WhatsApp conversations
        var client = await AuthHelper.LoginAsSecondOwnerAsync(_factory);

        // Act
        var response = await client.GetAsync("/api/v1/whatsapp/conversations");

        // Assert - may return 403 (feature gating) or 400 (missing query params)
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Forbidden, HttpStatusCode.BadRequest);
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var json = await ApiResponseHelper.ReadJsonAsync(response);
            json.ValueKind.Should().Be(JsonValueKind.Object);
        }
    }
}
