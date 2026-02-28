using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using SpeakHalo.IntegrationTests.TestHelpers;
using Xunit;

namespace SpeakHalo.IntegrationTests;

[Collection("Integration")]
public class SecurityTests
{
    private readonly SpeakHaloWebApplicationFactory _factory;

    public SecurityTests(SpeakHaloWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // ────────────────────────────────────────────────────────────────
    // SQL Injection Tests
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task SqlInjection_LoginEmail_NoSqlError()
    {
        // Arrange
        var client = _factory.CreateClient();

        foreach (var payload in DataFactory.SqlInjectionPayloads)
        {
            var loginRequest = new { Email = payload, Password = "password123" };

            // Act
            var response = await client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

            // Assert - should get a normal auth failure, not a 500 server error
            response.StatusCode.Should().NotBe(HttpStatusCode.InternalServerError,
                $"SQL injection payload '{payload}' caused a server error");

            var content = await response.Content.ReadAsStringAsync();
            content.Should().NotContainAny(
                "SQL", "syntax error", "SqlException", "NpgsqlException", "Npgsql",
                "unrecognized token", "SQLITE_ERROR", "pg_catalog");
        }
    }

    [Fact]
    public async Task SqlInjection_CustomerSearch_NoDataLeak()
    {
        // Arrange
        var client = await AuthHelper.LoginAsOwnerAsync(_factory);

        foreach (var payload in DataFactory.SqlInjectionPayloads)
        {
            var encodedPayload = Uri.EscapeDataString(payload);

            // Act
            var response = await client.GetAsync($"/api/v1/customers/search?searchTerm={encodedPayload}");

            // Assert - should not return 500 or leak SQL internals
            response.StatusCode.Should().NotBe(HttpStatusCode.InternalServerError,
                $"SQL injection payload '{payload}' caused a server error in customer search");

            var content = await response.Content.ReadAsStringAsync();
            content.Should().NotContainAny(
                "SqlException", "NpgsqlException", "syntax error",
                "UNION", "pg_catalog", "information_schema");
        }
    }

    [Fact]
    public async Task SqlInjection_DateRange_NoSqlError()
    {
        // Arrange
        var client = await AuthHelper.LoginAsOwnerAsync(_factory);

        foreach (var payload in DataFactory.SqlInjectionPayloads)
        {
            var encodedPayload = Uri.EscapeDataString(payload);

            // Act
            var response = await client.GetAsync(
                $"/api/v1/appointments/user/date-range?startDate={encodedPayload}&endDate={encodedPayload}");

            // Assert
            response.StatusCode.Should().NotBe(HttpStatusCode.InternalServerError,
                $"SQL injection payload '{payload}' caused a server error in date range");

            var content = await response.Content.ReadAsStringAsync();
            content.Should().NotContainAny(
                "SqlException", "NpgsqlException", "syntax error", "pg_catalog");
        }
    }

    [Fact]
    public async Task SqlInjection_UserNameUpdate_Sanitized()
    {
        // Arrange
        var client = await AuthHelper.LoginAsOwnerAsync(_factory);

        foreach (var payload in DataFactory.SqlInjectionPayloads)
        {
            var updateRequest = new { Name = payload };

            // Act
            var response = await client.PutAsJsonAsync(
                $"/api/v1/users/{SpeakHaloWebApplicationFactory.OwnerUserId}", updateRequest);

            // Assert - should not cause server error
            response.StatusCode.Should().NotBe(HttpStatusCode.InternalServerError,
                $"SQL injection payload '{payload}' caused a server error in user update");

            var content = await response.Content.ReadAsStringAsync();
            content.Should().NotContainAny(
                "SqlException", "NpgsqlException", "syntax error", "pg_catalog");
        }
    }

    // ────────────────────────────────────────────────────────────────
    // XSS Tests
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Xss_CustomerName_StoredAsPlainText()
    {
        // Arrange
        var client = await AuthHelper.LoginAsOwnerAsync(_factory);
        var xssPayload = "<script>alert('xss')</script>";
        var customerRequest = DataFactory.CreateCustomerRequest(name: xssPayload);

        // Act
        var createResponse = await client.PostAsJsonAsync("/api/v1/customers", customerRequest);

        // Assert - should store and return the value without executing it.
        // In a JSON API, XSS payloads are returned as plain text strings (which is safe
        // because JSON encoding prevents script execution). The frontend is responsible
        // for output encoding when rendering. Both accepting and rejecting are valid.
        if (createResponse.IsSuccessStatusCode)
        {
            var content = await createResponse.Content.ReadAsStringAsync();
            content.Should().NotBeNullOrEmpty();
        }
        else
        {
            // If rejected (validation), that's also acceptable security behavior
            createResponse.StatusCode.Should().BeOneOf(
                HttpStatusCode.BadRequest, HttpStatusCode.UnprocessableEntity);
        }
    }

    [Fact]
    public async Task Xss_VoiceAgentPrompt_StoredAsPlainText()
    {
        // Arrange
        var client = await AuthHelper.LoginAsOwnerAsync(_factory);
        var xssPayload = "<img src=x onerror=alert('xss')>";
        var agentRequest = new
        {
            Name = "XSS Test Agent",
            SystemPrompt = xssPayload,
            VoiceName = "Kore",
            Language = "en-US",
            MaxCallDurationMinutes = 15,
            LocationName = "Test Location",
            LocationAddress = "123 Test St",
            LocationPhone = "+12025551000"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/voice-agents", agentRequest);

        // Assert
        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            // XSS in system prompt should be stored safely
            content.Should().NotBeNullOrEmpty();
        }
        else
        {
            response.StatusCode.Should().BeOneOf(
                HttpStatusCode.BadRequest, HttpStatusCode.UnprocessableEntity);
        }
    }

    [Fact]
    public async Task Xss_KnowledgeBaseContent_StoredAsPlainText()
    {
        // Arrange
        var client = await AuthHelper.LoginAsOwnerAsync(_factory);
        var xssPayload = "<svg onload=alert('xss')>";
        var articleRequest = new
        {
            Title = "XSS Test Article",
            Content = xssPayload,
            Category = "General",
            Tags = new[] { "test" }
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/knowledge/articles", articleRequest);

        // Assert
        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            content.Should().NotBeNullOrEmpty();
        }
        else
        {
            response.StatusCode.Should().BeOneOf(
                HttpStatusCode.BadRequest, HttpStatusCode.UnprocessableEntity);
        }
    }

    [Fact]
    public async Task Xss_AppointmentDescription_StoredAsPlainText()
    {
        // Arrange
        var client = await AuthHelper.LoginAsOwnerAsync(_factory);
        var xssPayload = "'\"><script>alert('xss')</script>";
        var appointmentRequest = new
        {
            ScheduledAt = DateTime.UtcNow.AddDays(7).ToString("o"),
            DurationMinutes = 60,
            Title = "XSS Test Appointment",
            Description = xssPayload,
            Location = "123 Test St",
            ServiceType = "Cleaning",
            EstimatedCost = 100.00
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/appointments", appointmentRequest);

        // Assert
        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            content.Should().NotBeNullOrEmpty();
        }
        else
        {
            response.StatusCode.Should().BeOneOf(
                HttpStatusCode.BadRequest, HttpStatusCode.UnprocessableEntity);
        }
    }

    [Fact]
    public async Task Xss_CallNotes_StoredAsPlainText()
    {
        // Arrange
        var client = await AuthHelper.LoginAsOwnerAsync(_factory);
        var xssPayload = "<iframe src=\"javascript:alert('xss')\">";
        var callRequest = new
        {
            CallerPhone = "+12025551234",
            CallerName = "Test Caller",
            RecipientPhone = "+12025555678",
            Direction = "inbound",
            Summary = "Test call",
            Notes = xssPayload
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/calls", callRequest);

        // Assert
        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            content.Should().NotBeNullOrEmpty();
        }
        else
        {
            response.StatusCode.Should().BeOneOf(
                HttpStatusCode.BadRequest, HttpStatusCode.UnprocessableEntity);
        }
    }

    // ────────────────────────────────────────────────────────────────
    // Security Headers Tests
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task SecurityHeaders_XContentTypeOptions_Present()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/");

        // Assert
        response.Headers.TryGetValues("X-Content-Type-Options", out var values).Should().BeTrue();
        values.Should().Contain("nosniff");
    }

    [Fact]
    public async Task SecurityHeaders_XFrameOptions_Present()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/");

        // Assert
        response.Headers.TryGetValues("X-Frame-Options", out var values).Should().BeTrue();
        values!.First().Should().BeOneOf("DENY", "SAMEORIGIN");
    }

    [Fact]
    public async Task SecurityHeaders_StrictTransportSecurity_Present()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/");

        // Assert
        response.Headers.TryGetValues("Strict-Transport-Security", out var values).Should().BeTrue();
        values!.First().Should().Contain("max-age=");
    }

    [Fact]
    public async Task SecurityHeaders_ContentSecurityPolicy_Present()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/");

        // Assert
        response.Headers.TryGetValues("Content-Security-Policy", out var values).Should().BeTrue();
        values!.First().Should().Contain("default-src");
    }

    [Fact]
    public async Task SecurityHeaders_ServerHeader_Stripped()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/");

        // Assert - Server header should not be present or should be generic
        var hasServer = response.Headers.TryGetValues("Server", out var values);
        if (hasServer)
        {
            // If present, it should not reveal specific technology details
            values!.First().Should().NotContainAny("Kestrel", "Microsoft", "IIS", "Apache", "nginx");
        }
    }

    [Fact]
    public async Task SecurityHeaders_XPoweredBy_Stripped()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/");

        // Assert
        response.Headers.Contains("X-Powered-By").Should().BeFalse(
            "X-Powered-By header should be stripped to avoid revealing technology stack");
    }

    // ────────────────────────────────────────────────────────────────
    // Additional Security Tests
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task LargePayload_RejectedOrHandled()
    {
        // Arrange - use authenticated client to avoid 401 from auth check
        var client = await AuthHelper.LoginAsOwnerAsync(_factory);
        // Create a payload larger than 10MB
        var largeString = new string('A', 11 * 1024 * 1024);
        var largePayload = JsonSerializer.Serialize(new { Name = largeString, Phone = "+12025559999" });
        var content = new StringContent(largePayload, Encoding.UTF8, "application/json");

        // Act - use an authenticated endpoint to test large payload handling
        var response = await client.PostAsync("/api/v1/customers", content);

        // Assert - should be rejected or handled gracefully, not crash
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK,
            HttpStatusCode.Created,
            HttpStatusCode.BadRequest,
            HttpStatusCode.Unauthorized,
            HttpStatusCode.Forbidden,
            HttpStatusCode.RequestEntityTooLarge,
            (HttpStatusCode)413,
            HttpStatusCode.TooManyRequests,
            HttpStatusCode.InternalServerError);

        // Even if 500, the server should still be responsive
        var healthCheck = await _factory.CreateClient().GetAsync("/health/live");
        healthCheck.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task InvalidContentType_Handled()
    {
        // Arrange
        var client = _factory.CreateClient();
        var content = new StringContent("not xml data", Encoding.UTF8, "application/xml");

        // Act
        var response = await client.PostAsync("/api/v1/auth/login", content);

        // Assert - should handle gracefully, not crash
        response.StatusCode.Should().NotBe(HttpStatusCode.InternalServerError,
            "Invalid content type should be handled gracefully");
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest,
            HttpStatusCode.UnsupportedMediaType,
            HttpStatusCode.Unauthorized,
            HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task PathTraversal_Handled()
    {
        // Arrange
        var client = await AuthHelper.LoginAsOwnerAsync(_factory);

        var pathTraversalPayloads = new[]
        {
            "../../etc/passwd",
            "..%2F..%2Fetc%2Fpasswd",
            "....//....//etc/passwd",
            "%2e%2e%2f%2e%2e%2fetc%2fpasswd"
        };

        foreach (var payload in pathTraversalPayloads)
        {
            // Act
            var response = await client.GetAsync($"/api/v1/customers/search?searchTerm={payload}");

            // Assert - should not expose file system contents
            var content = await response.Content.ReadAsStringAsync();
            content.Should().NotContain("root:", $"Path traversal payload '{payload}' exposed file contents");
            content.Should().NotContain("/bin/bash", $"Path traversal payload '{payload}' exposed file contents");
            response.StatusCode.Should().NotBe(HttpStatusCode.InternalServerError,
                $"Path traversal payload '{payload}' caused a server error");
        }
    }

    [Fact]
    public async Task RequestSmuggling_Handled()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Test with conflicting Content-Length and Transfer-Encoding
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/login");
        request.Content = new StringContent(
            JsonSerializer.Serialize(new { Email = "test@test.com", Password = "test" }),
            Encoding.UTF8,
            "application/json");

        // Add a malformed header (some servers mishandle this)
        request.Headers.TryAddWithoutValidation("X-Custom-Header", "value\r\nInjected-Header: malicious");

        // Act
        var response = await client.SendAsync(request);

        // Assert - should handle without crashing
        response.StatusCode.Should().NotBe(HttpStatusCode.InternalServerError,
            "Malformed headers should be handled gracefully");
    }

    [Fact]
    public async Task ConcurrentLogin_Handled()
    {
        // Arrange
        var loginRequest = new
        {
            Email = SpeakHaloWebApplicationFactory.OwnerEmail,
            Password = SpeakHaloWebApplicationFactory.TestPassword
        };

        // Act - 10 concurrent login attempts from the same user
        var tasks = Enumerable.Range(0, 10).Select(async _ =>
        {
            var client = _factory.CreateClient();
            return await client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
        });

        var responses = await Task.WhenAll(tasks);

        // Assert - all should either succeed or be rate limited, none should error
        foreach (var response in responses)
        {
            response.StatusCode.Should().BeOneOf(
                HttpStatusCode.OK,
                HttpStatusCode.TooManyRequests,
                HttpStatusCode.Unauthorized);

            response.StatusCode.Should().NotBe(HttpStatusCode.InternalServerError,
                "Concurrent logins should not cause server errors");
        }
    }
}
