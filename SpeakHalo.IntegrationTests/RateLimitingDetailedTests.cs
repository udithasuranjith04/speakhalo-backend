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
/// These tests confirm that behavior.
/// </summary>
[Collection("Integration")]
public class RateLimitingDetailedTests
{
    private readonly SpeakHaloWebApplicationFactory _factory;

    public RateLimitingDetailedTests(SpeakHaloWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // ────────────────────────────────────────────────────────────────
    // Rate Limiting Disabled Verification Tests
    // ────────────────────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "RateLimiting")]
    public async Task Anonymous_25Requests_SucceedWhenRateLimitingDisabled()
    {
        // Arrange
        var client = AuthHelper.CreateAnonymousClient(_factory);

        // Act - Send 25 requests (anonymous limit would be 20/min in production)
        HttpResponseMessage? response = null;
        for (int i = 0; i < 25; i++)
        {
            response = await client.GetAsync("/api/v1/industries");
        }

        // Assert - Rate limiting is disabled in Testing environment, so all requests succeed
        response!.StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests,
            "rate limiting is disabled in the Testing environment");
    }

    [Fact]
    [Trait("Category", "RateLimiting")]
    public async Task Authenticated_105Requests_SucceedWhenRateLimitingDisabled()
    {
        // Arrange
        var client = await AuthHelper.LoginAsOwnerAsync(_factory);

        // Act - Send 105 requests (authenticated limit would be 100/min in production)
        HttpResponseMessage? response = null;
        for (int i = 0; i < 105; i++)
        {
            response = await client.GetAsync("/api/v1/calls/user/calls?pageNumber=1&pageSize=1");
        }

        // Assert - Rate limiting is disabled in Testing environment, so all requests succeed
        response!.StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests,
            "rate limiting is disabled in the Testing environment");
    }

    [Fact]
    [Trait("Category", "RateLimiting")]
    public async Task Login_6Requests_SucceedWhenRateLimitingDisabled()
    {
        // Arrange
        var client = AuthHelper.CreateAnonymousClient(_factory);
        var uniqueEmail = $"ratelimit-login-{Guid.NewGuid():N}@example.com";

        // Act - Send 6 login requests (login limit would be 5/min in production)
        HttpResponseMessage? response = null;
        for (int i = 0; i < 6; i++)
        {
            response = await client.PostAsJsonAsync("/api/v1/auth/login",
                new { Email = uniqueEmail, Password = "TestPassword123!" });
        }

        // Assert - Rate limiting is disabled in Testing environment, so all requests succeed
        response!.StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests,
            "rate limiting is disabled in the Testing environment");
    }

    [Fact]
    [Trait("Category", "RateLimiting")]
    public async Task ForgotPassword_4Requests_SucceedWhenRateLimitingDisabled()
    {
        // Arrange
        var client = AuthHelper.CreateAnonymousClient(_factory);
        var uniqueEmail = $"ratelimit-forgot-{Guid.NewGuid():N}@example.com";

        // Act - Send 4 forgot-password requests (limit would be 3/min in production)
        HttpResponseMessage? response = null;
        for (int i = 0; i < 4; i++)
        {
            response = await client.PostAsJsonAsync("/api/v1/auth/forgot-password",
                new { Email = uniqueEmail });
        }

        // Assert - Rate limiting is disabled in Testing environment, so all requests succeed
        response!.StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests,
            "rate limiting is disabled in the Testing environment");
    }

    // ────────────────────────────────────────────────────────────────
    // Rate Limit Headers Not Present Tests
    // ────────────────────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "RateLimiting")]
    public async Task RateLimit_XRateLimitLimit_Header_NotPresentInTestingEnvironment()
    {
        // Arrange
        var client = AuthHelper.CreateAnonymousClient(_factory);

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/auth/login",
            new { Email = $"ratelimit-header-limit-{Guid.NewGuid():N}@example.com", Password = "test" });

        // Assert - Rate limiting is disabled in Testing environment, so headers are not present
        response.Headers.Contains("X-RateLimit-Limit").Should().BeFalse(
            "rate limiting headers should not be present when rate limiting is disabled");
    }

    [Fact]
    [Trait("Category", "RateLimiting")]
    public async Task RateLimit_XRateLimitRemaining_Header_NotPresentInTestingEnvironment()
    {
        // Arrange
        var client = AuthHelper.CreateAnonymousClient(_factory);

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/auth/login",
            new { Email = $"ratelimit-header-remaining-{Guid.NewGuid():N}@example.com", Password = "test" });

        // Assert - Rate limiting is disabled in Testing environment, so headers are not present
        response.Headers.Contains("X-RateLimit-Remaining").Should().BeFalse(
            "rate limiting headers should not be present when rate limiting is disabled");
    }

    [Fact]
    [Trait("Category", "RateLimiting")]
    public async Task RateLimit_XRateLimitReset_Header_NotPresentInTestingEnvironment()
    {
        // Arrange
        var client = AuthHelper.CreateAnonymousClient(_factory);

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/auth/login",
            new { Email = $"ratelimit-header-reset-{Guid.NewGuid():N}@example.com", Password = "test" });

        // Assert - Rate limiting is disabled in Testing environment, so headers are not present
        response.Headers.Contains("X-RateLimit-Reset").Should().BeFalse(
            "rate limiting headers should not be present when rate limiting is disabled");
    }

    [Fact]
    [Trait("Category", "RateLimiting")]
    public async Task RateLimit_RetryAfter_Header_NotPresentWhenDisabled()
    {
        // Arrange
        var client = AuthHelper.CreateAnonymousClient(_factory);
        var uniqueEmail = $"ratelimit-retry-after-{Guid.NewGuid():N}@example.com";

        // Act - Send requests that would exceed the login limit in production (5/min)
        HttpResponseMessage? lastResponse = null;
        for (int i = 0; i < 7; i++)
        {
            lastResponse = await client.PostAsJsonAsync("/api/v1/auth/login",
                new { Email = uniqueEmail, Password = "test" });
        }

        // Assert - No request should be rate limited, and no Retry-After header should be present
        lastResponse!.StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests,
            "rate limiting is disabled in the Testing environment");
        lastResponse.Headers.Contains("Retry-After").Should().BeFalse(
            "Retry-After header should not be present when rate limiting is disabled");
    }

    // ────────────────────────────────────────────────────────────────
    // Endpoint Availability Tests (Health Check and OPTIONS)
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task HealthCheck_AlwaysSucceeds()
    {
        // Arrange
        var client = AuthHelper.CreateAnonymousClient(_factory);

        // Act - Send many health check requests
        var responses = new List<HttpResponseMessage>();
        for (int i = 0; i < 50; i++)
        {
            var response = await client.GetAsync("/health/live");
            responses.Add(response);
        }

        // Assert - All should succeed (health check is always accessible)
        responses.Should().OnlyContain(r => r.StatusCode == HttpStatusCode.OK,
            "health check endpoints should always return 200 OK");
    }

    [Fact]
    public async Task OptionsRequest_AlwaysSucceeds()
    {
        // Arrange
        var client = AuthHelper.CreateAnonymousClient(_factory);

        // Act - Send many OPTIONS requests (CORS preflight)
        var responses = new List<HttpResponseMessage>();
        for (int i = 0; i < 50; i++)
        {
            var request = new HttpRequestMessage(HttpMethod.Options, "/api/v1/auth/login");
            request.Headers.TryAddWithoutValidation("Origin", "http://localhost:3000");
            request.Headers.TryAddWithoutValidation("Access-Control-Request-Method", "POST");
            var response = await client.SendAsync(request);
            responses.Add(response);
        }

        // Assert - None should be rate limited
        var rateLimitedCount = responses.Count(r => r.StatusCode == HttpStatusCode.TooManyRequests);
        rateLimitedCount.Should().Be(0,
            "OPTIONS requests (CORS preflight) should never be rate limited");
    }

    // ────────────────────────────────────────────────────────────────
    // Rate Limiting Disabled Isolation Tests
    // ────────────────────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "RateLimiting")]
    public async Task MultipleClients_AllSucceedWhenRateLimitingDisabled()
    {
        // Arrange - two clients with different forwarded IPs
        var client1 = AuthHelper.CreateAnonymousClient(_factory);
        client1.DefaultRequestHeaders.TryAddWithoutValidation("X-Forwarded-For", "203.0.113.1");

        var client2 = AuthHelper.CreateAnonymousClient(_factory);
        client2.DefaultRequestHeaders.TryAddWithoutValidation("X-Forwarded-For", "203.0.113.2");

        var uniqueEmail1 = $"ratelimit-ip1-{Guid.NewGuid():N}@example.com";
        var uniqueEmail2 = $"ratelimit-ip2-{Guid.NewGuid():N}@example.com";

        // Act - Send requests that would exhaust rate limit for client1 in production
        HttpResponseMessage? client1Response = null;
        for (int i = 0; i < 6; i++)
        {
            client1Response = await client1.PostAsJsonAsync("/api/v1/auth/login",
                new { Email = uniqueEmail1, Password = "test" });
        }

        var client2Response = await client2.PostAsJsonAsync("/api/v1/auth/login",
            new { Email = uniqueEmail2, Password = "test" });

        // Assert - Both clients should succeed since rate limiting is disabled
        client1Response!.StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests,
            "rate limiting is disabled in the Testing environment");
        client2Response.StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests,
            "rate limiting is disabled in the Testing environment");
    }

    [Fact]
    [Trait("Category", "RateLimiting")]
    public async Task ExcessiveRequests_NeverReturn429WhenRateLimitingDisabled()
    {
        // Arrange
        var client = AuthHelper.CreateAnonymousClient(_factory);
        var uniqueEmail = $"ratelimit-429-body-{Guid.NewGuid():N}@example.com";

        // Act - Send requests that would exceed the login limit in production
        var responses = new List<HttpResponseMessage>();
        for (int i = 0; i < 7; i++)
        {
            var response = await client.PostAsJsonAsync("/api/v1/auth/login",
                new { Email = uniqueEmail, Password = "test" });
            responses.Add(response);
        }

        // Assert - No request should return 429 since rate limiting is disabled
        var rateLimitedCount = responses.Count(r => r.StatusCode == HttpStatusCode.TooManyRequests);
        rateLimitedCount.Should().Be(0,
            "no requests should be rate limited when rate limiting is disabled in the Testing environment");
    }

    [Fact]
    [Trait("Category", "RateLimiting")]
    public async Task RateLimit_Headers_NotPresentOnAnyRequestInTestingEnvironment()
    {
        // Arrange
        var client = await AuthHelper.LoginAsOwnerAsync(_factory);

        // Act
        var response = await client.GetAsync("/api/v1/industries");

        // Assert - Rate limiting middleware skips entirely in Testing, so no headers
        response.IsSuccessStatusCode.Should().BeTrue();
        response.Headers.Contains("X-RateLimit-Limit").Should().BeFalse(
            "rate limiting headers should not be present when rate limiting is disabled");
        response.Headers.Contains("X-RateLimit-Remaining").Should().BeFalse(
            "rate limiting headers should not be present when rate limiting is disabled");
        response.Headers.Contains("X-RateLimit-Reset").Should().BeFalse(
            "rate limiting headers should not be present when rate limiting is disabled");
    }
}
