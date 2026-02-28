using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SpeakHalo.IntegrationTests.TestHelpers;
using Xunit;

namespace SpeakHalo.IntegrationTests;

/// <summary>
/// Rate limiting tests that verify rate limiting is correctly DISABLED in the Testing environment.
/// In production, the RateLimitingMiddleware enforces per-IP and per-endpoint limits.
/// In Testing, the middleware skips entirely (no enforcement, no headers).
/// </summary>
[Collection("Integration")]
public class RateLimitingTests
{
    private readonly SpeakHaloWebApplicationFactory _factory;

    public RateLimitingTests(SpeakHaloWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    [Trait("Category", "RateLimiting")]
    public async Task Login_ExceedingRateLimit_SucceedsWhenRateLimitingDisabled()
    {
        // Arrange
        var client = AuthHelper.CreateAnonymousClient(_factory);
        var loginRequest = new
        {
            Email = $"ratelimit-{Guid.NewGuid():N}@example.com",
            Password = "password"
        };

        // Act - Send 6 requests (limit would be 5 for login endpoint in production)
        var responses = new List<HttpResponseMessage>();
        for (int i = 0; i < 6; i++)
        {
            var response = await client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
            responses.Add(response);
        }

        // Assert - Rate limiting is disabled in Testing environment
        var lastResponse = responses.Last();
        lastResponse.StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests,
            "rate limiting is disabled in the Testing environment");

        // Rate limit headers should not be present
        lastResponse.Headers.Contains("X-RateLimit-Limit").Should().BeFalse(
            "rate limiting headers should not be present when rate limiting is disabled");
        lastResponse.Headers.Contains("X-RateLimit-Remaining").Should().BeFalse(
            "rate limiting headers should not be present when rate limiting is disabled");
        lastResponse.Headers.Contains("X-RateLimit-Reset").Should().BeFalse(
            "rate limiting headers should not be present when rate limiting is disabled");
        lastResponse.Headers.Contains("Retry-After").Should().BeFalse(
            "Retry-After header should not be present when rate limiting is disabled");
    }

    [Fact]
    [Trait("Category", "RateLimiting")]
    public async Task RateLimit_Headers_NotPresentInTestingEnvironment()
    {
        // Arrange
        var client = await AuthHelper.LoginAsOwnerAsync(_factory);

        // Act - Make a request to an endpoint that would have rate limiting in production
        var response = await client.GetAsync("/api/v1/industries");

        // Assert - Rate limiting middleware skips entirely in Testing environment
        response.IsSuccessStatusCode.Should().BeTrue();
        response.Headers.Contains("X-RateLimit-Limit").Should().BeFalse(
            "rate limiting headers should not be present when rate limiting is disabled");
    }
}
