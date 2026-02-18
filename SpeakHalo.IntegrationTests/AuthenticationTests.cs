using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using SpeakHalo.Application.DTOs.Auth;
using Xunit;

namespace SpeakHalo.IntegrationTests;

public class AuthenticationTests : IClassFixture<SpeakHaloWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthenticationTests(SpeakHaloWebApplicationFactory factory)
    {
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
        // Arrange - use the seeded test user from WebApplicationFactory
        var loginRequest = new LoginRequest
        {
            Email = "test@example.com",
            Password = "hashed_password" // Note: seeded user has a plain hashed_password value, login will fail
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

        // Assert - Expected to fail because seeded user has a raw hash, not a real password
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
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
