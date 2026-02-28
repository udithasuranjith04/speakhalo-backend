using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using SpeakHalo.Application.DTOs.Auth;
using Xunit;

namespace SpeakHalo.IntegrationTests;

[Collection("Integration")]
public class AuthenticationTests
{
    private readonly SpeakHaloWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AuthenticationTests(SpeakHaloWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ShouldReturnUnauthorized()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            Email = "nonexistent@example.com",
            Password = "WrongPassword123!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithSeededUser_ShouldReturnToken()
    {
        // Arrange - use the seeded Owner user with proper BCrypt password
        var loginRequest = new LoginRequest
        {
            Email = SpeakHaloWebApplicationFactory.OwnerEmail,
            Password = SpeakHaloWebApplicationFactory.TestPassword
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonSerializer.Deserialize<JsonElement>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        // API response wrapper: { isSuccess, data, message }
        if (json.TryGetProperty("data", out var data))
        {
            data.GetProperty("accessToken").GetString().Should().NotBeNullOrEmpty();
            data.GetProperty("refreshToken").GetString().Should().NotBeNullOrEmpty();
        }
        else
        {
            json.GetProperty("accessToken").GetString().Should().NotBeNullOrEmpty();
        }
    }

    [Fact]
    public async Task RefreshToken_WithInvalidToken_ShouldReturnUnauthorized()
    {
        // Arrange
        var refreshRequest = new RefreshTokenRequest
        {
            RefreshToken = "invalid-refresh-token"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/refresh", refreshRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
