using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;

namespace SpeakHalo.IntegrationTests.TestHelpers;

public static class AuthHelper
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Get an authenticated HttpClient for the seeded Owner user.
    /// Uses direct JWT generation to avoid rate limiting.
    /// </summary>
    public static Task<HttpClient> LoginAsOwnerAsync(SpeakHaloWebApplicationFactory factory)
    {
        return Task.FromResult(CreateAuthenticatedClient(factory,
            SpeakHaloWebApplicationFactory.OwnerUserId,
            SpeakHaloWebApplicationFactory.OwnerEmail,
            "Owner"));
    }

    /// <summary>
    /// Get an authenticated HttpClient for the seeded Admin user.
    /// </summary>
    public static Task<HttpClient> LoginAsAdminAsync(SpeakHaloWebApplicationFactory factory)
    {
        return Task.FromResult(CreateAuthenticatedClient(factory,
            SpeakHaloWebApplicationFactory.AdminUserId,
            SpeakHaloWebApplicationFactory.AdminEmail,
            "Admin"));
    }

    /// <summary>
    /// Get an authenticated HttpClient for the seeded Member user.
    /// </summary>
    public static Task<HttpClient> LoginAsMemberAsync(SpeakHaloWebApplicationFactory factory)
    {
        return Task.FromResult(CreateAuthenticatedClient(factory,
            SpeakHaloWebApplicationFactory.MemberUserId,
            SpeakHaloWebApplicationFactory.MemberEmail,
            "Member"));
    }

    /// <summary>
    /// Get an authenticated HttpClient for the seeded Second Owner user (for data isolation tests).
    /// </summary>
    public static Task<HttpClient> LoginAsSecondOwnerAsync(SpeakHaloWebApplicationFactory factory)
    {
        return Task.FromResult(CreateAuthenticatedClient(factory,
            SpeakHaloWebApplicationFactory.SecondOwnerUserId,
            SpeakHaloWebApplicationFactory.SecondOwnerEmail,
            "Owner"));
    }

    /// <summary>
    /// Login with the given credentials via the actual login endpoint.
    /// Use this only for tests that specifically test the login flow.
    /// For general authenticated testing, use CreateAuthenticatedClient instead.
    /// </summary>
    public static async Task<HttpClient> LoginViaEndpointAsync(SpeakHaloWebApplicationFactory factory, string email, string password)
    {
        var client = factory.CreateClient();
        var loginRequest = new { Email = email, Password = password };
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var loginResponse = JsonSerializer.Deserialize<JsonElement>(content, JsonOptions);

        string accessToken;
        if (loginResponse.TryGetProperty("data", out var data))
            accessToken = data.GetProperty("accessToken").GetString()!;
        else
            accessToken = loginResponse.GetProperty("accessToken").GetString()!;

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return client;
    }

    /// <summary>
    /// Kept for backward compatibility. Uses direct JWT to avoid rate limiting.
    /// </summary>
    public static async Task<HttpClient> LoginAsync(SpeakHaloWebApplicationFactory factory, string email, string password)
    {
        return await LoginViaEndpointAsync(factory, email, password);
    }

    /// <summary>
    /// Generate a JWT token directly (without going through the login endpoint).
    /// </summary>
    public static string GenerateTestToken(
        string userId,
        string email,
        string role = "Owner",
        DateTime? expires = null,
        string? secretKey = null)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
            secretKey ?? SpeakHaloWebApplicationFactory.JwtSecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Role, role),
            new Claim(JwtRegisteredClaimNames.Sub, userId),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: SpeakHaloWebApplicationFactory.JwtIssuer,
            audience: SpeakHaloWebApplicationFactory.JwtAudience,
            claims: claims,
            expires: expires ?? DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Create an HttpClient with a pre-generated JWT token (no login endpoint needed).
    /// This is the preferred way to create authenticated clients for most tests.
    /// </summary>
    public static HttpClient CreateAuthenticatedClient(
        SpeakHaloWebApplicationFactory factory,
        string userId,
        string email,
        string role = "Owner")
    {
        var client = factory.CreateClient();
        var token = GenerateTestToken(userId, email, role);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    /// <summary>
    /// Create an HttpClient with an expired JWT token.
    /// </summary>
    public static HttpClient CreateExpiredTokenClient(SpeakHaloWebApplicationFactory factory)
    {
        var client = factory.CreateClient();
        var token = GenerateTestToken(
            SpeakHaloWebApplicationFactory.OwnerUserId,
            SpeakHaloWebApplicationFactory.OwnerEmail,
            expires: DateTime.UtcNow.AddHours(-1));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    /// <summary>
    /// Create an HttpClient with a malformed JWT token.
    /// </summary>
    public static HttpClient CreateMalformedTokenClient(SpeakHaloWebApplicationFactory factory)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "not.a.valid.jwt.token");
        return client;
    }

    /// <summary>
    /// Create an HttpClient with a JWT signed with a wrong key.
    /// </summary>
    public static HttpClient CreateWrongKeyTokenClient(SpeakHaloWebApplicationFactory factory)
    {
        var client = factory.CreateClient();
        var token = GenerateTestToken(
            SpeakHaloWebApplicationFactory.OwnerUserId,
            SpeakHaloWebApplicationFactory.OwnerEmail,
            secretKey: "a_completely_different_secret_key_that_is_long_enough");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    /// <summary>
    /// Create an unauthenticated HttpClient.
    /// </summary>
    public static HttpClient CreateAnonymousClient(SpeakHaloWebApplicationFactory factory)
    {
        return factory.CreateClient();
    }
}
