using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace SpeakHalo.IntegrationTests;

public class HealthCheckTests : IClassFixture<SpeakHaloWebApplicationFactory>
{
    private readonly HttpClient _client;

    public HealthCheckTests(SpeakHaloWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task HealthCheck_ShouldReturnOk()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task HealthCheck_ShouldReturnJsonWithStatus()
    {
        // Act
        var response = await _client.GetAsync("/health");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        content.Should().NotBeNullOrEmpty();

        // Try to parse as JSON
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
