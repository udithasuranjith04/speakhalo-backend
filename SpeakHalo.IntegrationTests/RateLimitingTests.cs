using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace SpeakHalo.IntegrationTests;

public class RateLimitingTests : IClassFixture<SpeakHaloWebApplicationFactory>
{
    private readonly SpeakHaloWebApplicationFactory _factory;

    public RateLimitingTests(SpeakHaloWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Login_ExceedingRateLimit_ShouldReturn429()
    {
        // Arrange
        var client = _factory.CreateClient();
        var loginRequest = new
        {
            Email = "test@example.com",
            Password = "password"
        };

        // Act - Send 6 requests (limit is 5 for login endpoint)
        var responses = new List<HttpResponseMessage>();
        for (int i = 0; i < 6; i++)
        {
            var response = await client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
            responses.Add(response);
        }

        // Assert
        var lastResponse = responses.Last();
        lastResponse.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);

        // Check rate limit headers
        lastResponse.Headers.Should().ContainKey("X-RateLimit-Limit");
        lastResponse.Headers.Should().ContainKey("X-RateLimit-Remaining");
        lastResponse.Headers.Should().ContainKey("X-RateLimit-Reset");
        lastResponse.Headers.Should().ContainKey("Retry-After");
    }

    [Fact]
    public async Task RateLimit_Headers_ShouldBePresent()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/v1/health");

        // Assert - Health check should be excluded from rate limiting
        // But other endpoints should have headers
        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", new { Email = "test@example.com", Password = "test" });

        loginResponse.Headers.TryGetValues("X-RateLimit-Limit", out var limitValues);
        limitValues.Should().NotBeNull();
    }

}
