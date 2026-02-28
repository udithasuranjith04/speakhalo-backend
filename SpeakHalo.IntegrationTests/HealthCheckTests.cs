using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace SpeakHalo.IntegrationTests;

[Collection("Integration")]
public class HealthCheckTests
{
    private readonly HttpClient _client;

    public HealthCheckTests(SpeakHaloWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task HealthCheckLive_ShouldReturnOk()
    {
        // Act - /health/live only checks database connectivity
        var response = await _client.GetAsync("/health/live");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task HealthCheck_ShouldReturnJsonWithStatus()
    {
        // Act - /health includes external services which may be unavailable in tests
        var response = await _client.GetAsync("/health");
        var content = await response.Content.ReadAsStringAsync();

        // Assert - should return JSON regardless of status (200 or 503)
        content.Should().NotBeNullOrEmpty();
        var parsed = JsonSerializer.Deserialize<JsonElement>(content);
        parsed.ValueKind.Should().Be(JsonValueKind.Object);
    }

    [Fact]
    public async Task RootEndpoint_ShouldReturnApiInfo()
    {
        // Act
        var response = await _client.GetAsync("/");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<JsonElement>();

        content.GetProperty("name").GetString().Should().Be("SpeakHalo API");
        content.GetProperty("version").GetString().Should().NotBeNullOrEmpty();
        content.GetProperty("status").GetString().Should().Be("running");
    }

    [Fact]
    public async Task NonExistentEndpoint_ShouldReturn404()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/nonexistent");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
