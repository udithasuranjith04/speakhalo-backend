using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace SpeakHalo.IntegrationTests.TestHelpers;

/// <summary>
/// Helper methods for unwrapping the standard API response format:
/// { isSuccess, data, message, errors }
/// </summary>
public static class ApiResponseHelper
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Read and deserialize the full API response wrapper.
    /// </summary>
    public static async Task<ApiResponse<T>> ReadResponseAsync<T>(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonSerializer.Deserialize<JsonElement>(content, JsonOptions);

        var apiResponse = new ApiResponse<T>
        {
            IsSuccess = json.TryGetProperty("isSuccess", out var s) && s.GetBoolean(),
            Message = json.TryGetProperty("message", out var m) ? m.GetString() : null,
            Errors = json.TryGetProperty("errors", out var e) && e.ValueKind == JsonValueKind.Array
                ? e.EnumerateArray().Select(x => x.GetString()!).ToList()
                : new List<string>(),
            RawJson = json
        };

        if (json.TryGetProperty("data", out var data) && data.ValueKind != JsonValueKind.Null)
        {
            apiResponse.Data = JsonSerializer.Deserialize<T>(data.GetRawText(), JsonOptions);
        }

        return apiResponse;
    }

    /// <summary>
    /// Read the response and extract just the data property.
    /// </summary>
    public static async Task<T?> ReadDataAsync<T>(HttpResponseMessage response)
    {
        var apiResponse = await ReadResponseAsync<T>(response);
        return apiResponse.Data;
    }

    /// <summary>
    /// Read the raw JSON response (for non-wrapped responses).
    /// </summary>
    public static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<JsonElement>(content, JsonOptions);
    }

    /// <summary>
    /// Assert that the API response indicates success and return the data.
    /// </summary>
    public static async Task<T> AssertSuccessAndGetDataAsync<T>(HttpResponseMessage response)
    {
        response.IsSuccessStatusCode.Should().BeTrue(
            $"Expected success but got {response.StatusCode}: {await response.Content.ReadAsStringAsync()}");

        var apiResponse = await ReadResponseAsync<T>(response);
        apiResponse.IsSuccess.Should().BeTrue(apiResponse.Message ?? "API returned isSuccess=false");
        apiResponse.Data.Should().NotBeNull();
        return apiResponse.Data!;
    }

    /// <summary>
    /// Assert that the API response indicates failure.
    /// </summary>
    public static async Task<ApiResponse<T>> AssertFailureAsync<T>(HttpResponseMessage response)
    {
        var apiResponse = await ReadResponseAsync<T>(response);
        apiResponse.IsSuccess.Should().BeFalse();
        return apiResponse;
    }

    /// <summary>
    /// Read response as a simple string (for non-JSON responses).
    /// </summary>
    public static async Task<string> ReadStringAsync(HttpResponseMessage response)
    {
        return await response.Content.ReadAsStringAsync();
    }
}

public class ApiResponse<T>
{
    public bool IsSuccess { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }
    public List<string> Errors { get; set; } = new();
    public JsonElement RawJson { get; set; }
}
