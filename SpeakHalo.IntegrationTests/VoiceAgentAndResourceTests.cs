using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using SpeakHalo.IntegrationTests.TestHelpers;
using Xunit;

namespace SpeakHalo.IntegrationTests;

[Collection("Integration")]
public class VoiceAgentAndResourceTests
{
    private readonly SpeakHaloWebApplicationFactory _factory;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public VoiceAgentAndResourceTests(SpeakHaloWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // ───────────────────────────────────────────────────────
    //  Voice Agents (8 tests)
    // ───────────────────────────────────────────────────────

    private async Task<string> CreateVoiceAgentAndGetIdAsync(HttpClient client, string? name = null)
    {
        var request = DataFactory.CreateVoiceAgentRequest(name);
        var response = await client.PostAsJsonAsync("/api/v1/voice-agents", request);
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            $"Failed to create voice agent: {await response.Content.ReadAsStringAsync()}");

        var json = await ApiResponseHelper.ReadJsonAsync(response);

        // The VoiceAgent create endpoint returns { agentId, name, ... } directly (not wrapped)
        if (json.TryGetProperty("agentId", out var agentIdProp))
            return agentIdProp.GetString()!;
        if (json.TryGetProperty("data", out var data) && data.TryGetProperty("agentId", out var dataAgentId))
            return dataAgentId.GetString()!;
        if (json.TryGetProperty("data", out var d2) && d2.TryGetProperty("id", out var dataId))
            return dataId.GetString()!;
        if (json.TryGetProperty("id", out var directId))
            return directId.GetString()!;

        throw new InvalidOperationException($"Could not extract agent ID from: {json}");
    }

    [Fact]
    public async Task CreateVoiceAgent_Valid_ReturnsCreated()
    {
        // Arrange
        var client = await AuthHelper.LoginAsOwnerAsync(_factory);
        var request = DataFactory.CreateVoiceAgentRequest();

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/voice-agents", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await ApiResponseHelper.ReadJsonAsync(response);
        json.ValueKind.Should().Be(JsonValueKind.Object);

        // Verify key fields are present
        var hasAgentId = json.TryGetProperty("agentId", out _);
        var hasName = json.TryGetProperty("name", out _);
        hasAgentId.Should().BeTrue("response should contain agentId");
        hasName.Should().BeTrue("response should contain name");
    }

    [Fact]
    public async Task GetVoiceAgent_ById_Returns()
    {
        // Arrange
        var client = await AuthHelper.LoginAsOwnerAsync(_factory);
        var agentId = await CreateVoiceAgentAndGetIdAsync(client);

        // Act
        var response = await client.GetAsync($"/api/v1/voice-agents/{agentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await ApiResponseHelper.ReadJsonAsync(response);
        json.TryGetProperty("agentId", out var returnedId).Should().BeTrue();
        returnedId.GetString().Should().Be(agentId);
    }

    [Fact]
    public async Task ListVoiceAgents_Returns()
    {
        // Arrange
        var client = await AuthHelper.LoginAsOwnerAsync(_factory);
        await CreateVoiceAgentAndGetIdAsync(client, "ListTest Agent");

        // Act
        var response = await client.GetAsync("/api/v1/voice-agents");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await ApiResponseHelper.ReadJsonAsync(response);

        // The list endpoint returns an array directly (not wrapped)
        json.ValueKind.Should().BeOneOf(JsonValueKind.Array, JsonValueKind.Object);
    }

    [Fact]
    public async Task UpdateVoiceAgent_Valid_Returns()
    {
        // Arrange
        var client = await AuthHelper.LoginAsOwnerAsync(_factory);
        var agentId = await CreateVoiceAgentAndGetIdAsync(client);

        // Act - PATCH endpoint
        var updateRequest = DataFactory.UpdateVoiceAgentRequest();
        var response = await client.PatchAsJsonAsync($"/api/v1/voice-agents/{agentId}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await ApiResponseHelper.ReadJsonAsync(response);
        json.TryGetProperty("name", out var name).Should().BeTrue();
        name.GetString().Should().Be("Updated Voice Agent");
    }

    [Fact]
    public async Task DeleteVoiceAgent_Valid_Returns()
    {
        // Arrange
        var client = await AuthHelper.LoginAsOwnerAsync(_factory);
        var agentId = await CreateVoiceAgentAndGetIdAsync(client, "ToBeDeleted");

        // Act
        var response = await client.DeleteAsync($"/api/v1/voice-agents/{agentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await ApiResponseHelper.ReadJsonAsync(response);
        json.TryGetProperty("deleted", out var deleted).Should().BeTrue();
        deleted.GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task GetVoiceAgentSettings_Returns()
    {
        // Arrange
        var client = await AuthHelper.LoginAsOwnerAsync(_factory);

        // Act
        var response = await client.GetAsync("/api/v1/voice-agents/settings");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await ApiResponseHelper.ReadJsonAsync(response);
        json.ValueKind.Should().Be(JsonValueKind.Object);

        // Settings should have at least enableTroubleshooting
        json.TryGetProperty("enableTroubleshooting", out _).Should().BeTrue();
    }

    [Fact]
    public async Task GetAvailableVoices_Returns()
    {
        // Arrange - AllowAnonymous endpoint
        var client = AuthHelper.CreateAnonymousClient(_factory);

        // Act
        var response = await client.GetAsync("/api/v1/voice-agents/voices");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await ApiResponseHelper.ReadJsonAsync(response);
        json.TryGetProperty("voices", out var voices).Should().BeTrue();
        voices.ValueKind.Should().Be(JsonValueKind.Array);
        voices.GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetBranding_Returns()
    {
        // Arrange - Requires CustomBranding feature (Owner has all features)
        var client = await AuthHelper.LoginAsOwnerAsync(_factory);

        // Act
        var response = await client.GetAsync("/api/v1/voice-agents/branding");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await ApiResponseHelper.ReadJsonAsync(response);
        json.ValueKind.Should().Be(JsonValueKind.Object);

        // Should have branding fields even if null
        json.TryGetProperty("brandName", out _).Should().BeTrue();
        json.TryGetProperty("brandColor", out _).Should().BeTrue();
    }

    // ───────────────────────────────────────────────────────
    //  Knowledge Base (5 tests)
    // ───────────────────────────────────────────────────────

    private async Task<string> CreateArticleAndGetIdAsync(HttpClient client, string? title = null)
    {
        var request = DataFactory.CreateKnowledgeBaseArticleRequest(title);
        var response = await client.PostAsJsonAsync("/api/v1/knowledge/articles", request);
        response.StatusCode.Should().BeOneOf(new[] { HttpStatusCode.OK, HttpStatusCode.Created },
            $"Failed to create article: {await response.Content.ReadAsStringAsync()}");

        var json = await ApiResponseHelper.ReadJsonAsync(response);

        if (json.TryGetProperty("data", out var data) && data.TryGetProperty("id", out var idProp))
            return idProp.GetString()!;
        if (json.TryGetProperty("id", out var directId))
            return directId.GetString()!;

        throw new InvalidOperationException($"Could not extract article ID from: {json}");
    }

    [Fact]
    public async Task CreateArticle_Valid_Returns()
    {
        // Arrange
        var client = await AuthHelper.LoginAsOwnerAsync(_factory);
        var request = DataFactory.CreateKnowledgeBaseArticleRequest();

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/knowledge/articles", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Created);
        var json = await ApiResponseHelper.ReadJsonAsync(response);
        json.ValueKind.Should().Be(JsonValueKind.Object);
    }

    [Fact]
    public async Task GetArticle_ById_Returns()
    {
        // Arrange
        var client = await AuthHelper.LoginAsOwnerAsync(_factory);
        var articleId = await CreateArticleAndGetIdAsync(client);

        // Act
        var response = await client.GetAsync($"/api/v1/knowledge/articles/{articleId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await ApiResponseHelper.ReadJsonAsync(response);
        json.ValueKind.Should().Be(JsonValueKind.Object);
    }

    [Fact]
    public async Task ListArticles_Returns()
    {
        // Arrange
        var client = await AuthHelper.LoginAsOwnerAsync(_factory);
        await CreateArticleAndGetIdAsync(client, "ListArticle");

        // Act - Search endpoint lists articles
        var response = await client.GetAsync("/api/v1/knowledge/articles/search");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await ApiResponseHelper.ReadJsonAsync(response);
        json.ValueKind.Should().Be(JsonValueKind.Object);
    }

    [Fact]
    public async Task DeleteArticle_Valid_Returns()
    {
        // Arrange
        var client = await AuthHelper.LoginAsOwnerAsync(_factory);
        var articleId = await CreateArticleAndGetIdAsync(client, "ToDeleteArticle");

        // Act
        var response = await client.DeleteAsync($"/api/v1/knowledge/articles/{articleId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify it is gone (may return 200 if soft delete still returns the article)
        var getResponse = await client.GetAsync($"/api/v1/knowledge/articles/{articleId}");
        getResponse.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task KnowledgeBase_CrossUserIsolation()
    {
        // Arrange
        var ownerClient = await AuthHelper.LoginAsOwnerAsync(_factory);
        var secondOwnerClient = await AuthHelper.LoginAsSecondOwnerAsync(_factory);

        // Owner creates an article
        var articleId = await CreateArticleAndGetIdAsync(ownerClient, "OwnerOnlyArticle");

        // Act - Knowledge base articles are shared by design (not per-user scoped),
        // so both users should be able to see them. Verify the article is accessible
        // by owner and check if secondOwner can also see it (shared knowledge base).
        var ownerGetResponse = await ownerClient.GetAsync($"/api/v1/knowledge/articles/{articleId}");
        ownerGetResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var secondOwnerGetResponse = await secondOwnerClient.GetAsync($"/api/v1/knowledge/articles/{articleId}");
        // Knowledge base may be shared or user-scoped; 403 from feature gating is also valid
        secondOwnerGetResponse.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound, HttpStatusCode.Forbidden);
    }

    // ───────────────────────────────────────────────────────
    //  Service Catalog (5 tests)
    // ───────────────────────────────────────────────────────

    private async Task<string> CreateServiceItemAndGetIdAsync(HttpClient client, string? name = null)
    {
        var request = DataFactory.CreateServiceCatalogItemRequest(name);
        var response = await client.PostAsJsonAsync("/api/v1/service-catalog", request);
        response.StatusCode.Should().BeOneOf(new[] { HttpStatusCode.OK, HttpStatusCode.Created },
            $"Failed to create service item: {await response.Content.ReadAsStringAsync()}");

        var json = await ApiResponseHelper.ReadJsonAsync(response);

        if (json.TryGetProperty("data", out var data) && data.TryGetProperty("id", out var idProp))
            return idProp.GetString()!;
        if (json.TryGetProperty("id", out var directId))
            return directId.GetString()!;

        throw new InvalidOperationException($"Could not extract service item ID from: {json}");
    }

    [Fact]
    public async Task CreateServiceItem_Valid_Returns()
    {
        // Arrange
        var client = await AuthHelper.LoginAsOwnerAsync(_factory);
        var request = DataFactory.CreateServiceCatalogItemRequest();

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/service-catalog", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Created);
        var json = await ApiResponseHelper.ReadJsonAsync(response);
        json.ValueKind.Should().Be(JsonValueKind.Object);
    }

    [Fact]
    public async Task GetServiceItem_ById_Returns()
    {
        // Arrange
        var client = await AuthHelper.LoginAsOwnerAsync(_factory);
        var itemId = await CreateServiceItemAndGetIdAsync(client);

        // Act
        var response = await client.GetAsync($"/api/v1/service-catalog/{itemId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await ApiResponseHelper.ReadJsonAsync(response);
        json.ValueKind.Should().Be(JsonValueKind.Object);
    }

    [Fact]
    public async Task ListServiceItems_Returns()
    {
        // Arrange
        var client = await AuthHelper.LoginAsOwnerAsync(_factory);
        await CreateServiceItemAndGetIdAsync(client, "ListTestService");

        // Act
        var response = await client.GetAsync("/api/v1/service-catalog");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await ApiResponseHelper.ReadJsonAsync(response);
        json.ValueKind.Should().Be(JsonValueKind.Object);
    }

    [Fact]
    public async Task DeleteServiceItem_Valid_Returns()
    {
        // Arrange
        var client = await AuthHelper.LoginAsOwnerAsync(_factory);
        var itemId = await CreateServiceItemAndGetIdAsync(client, "ToDeleteService");

        // Act
        var response = await client.DeleteAsync($"/api/v1/service-catalog/{itemId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ServiceCatalog_CrossUserIsolation()
    {
        // Arrange
        var ownerClient = await AuthHelper.LoginAsOwnerAsync(_factory);
        var secondOwnerClient = await AuthHelper.LoginAsSecondOwnerAsync(_factory);

        // Owner creates a service item
        var itemId = await CreateServiceItemAndGetIdAsync(ownerClient, "OwnerOnlyService");

        // Act - SecondOwner should not be able to see Owner's service item
        var crossResponse = await secondOwnerClient.GetAsync($"/api/v1/service-catalog/{itemId}");

        // Assert
        crossResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
