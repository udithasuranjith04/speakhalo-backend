using System.Text.Json;
using Microsoft.Extensions.Logging;
using SpeakHalo.Application.Interfaces;

namespace SpeakHalo.Infrastructure.Services;

public class NominatimGeocodingService : IGeocodingService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<NominatimGeocodingService> _logger;
    private static readonly SemaphoreSlim _rateLimiter = new(1, 1);

    public NominatimGeocodingService(HttpClient httpClient, ILogger<NominatimGeocodingService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("SpeakHalo/1.0 (contact@speakhalo.com)");
    }

    public async Task<(double Latitude, double Longitude)?> GeocodeAddressAsync(
        string? address, string? city, string? state, string? zipCode,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Build fallback queries from most specific to least specific
            var queries = BuildFallbackQueries(address, city, state, zipCode);
            if (queries.Count == 0)
                return null;

            foreach (var query in queries)
            {
                var result = await TryGeocodeQueryAsync(query, cancellationToken);
                if (result != null)
                {
                    _logger.LogDebug("Geocoded '{Query}' to ({Lat}, {Lon})", query, result.Value.Latitude, result.Value.Longitude);
                    return result;
                }

                _logger.LogDebug("No results for query: {Query}, trying next fallback...", query);
            }

            _logger.LogDebug("All geocoding fallbacks exhausted for: {Address}, {City}, {State}, {ZipCode}",
                address, city, state, zipCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Geocoding failed for address components: {Address}, {City}, {State}, {ZipCode}",
                address, city, state, zipCode);
            return null;
        }
    }

    private static List<string> BuildFallbackQueries(string? address, string? city, string? state, string? zipCode)
    {
        var queries = new List<string>();

        // 1. Full address: street, city, state, zip
        var fullParts = new[] { address, city, state, zipCode }.Where(p => !string.IsNullOrWhiteSpace(p));
        var full = string.Join(", ", fullParts);
        if (!string.IsNullOrWhiteSpace(full))
            queries.Add(full);

        // 2. Without street address: city, state, zip
        var noStreetParts = new[] { city, state, zipCode }.Where(p => !string.IsNullOrWhiteSpace(p));
        var noStreet = string.Join(", ", noStreetParts);
        if (!string.IsNullOrWhiteSpace(noStreet) && noStreet != full)
            queries.Add(noStreet);

        // 3. City + state only
        var cityStateParts = new[] { city, state }.Where(p => !string.IsNullOrWhiteSpace(p));
        var cityState = string.Join(", ", cityStateParts);
        if (!string.IsNullOrWhiteSpace(cityState) && !queries.Contains(cityState))
            queries.Add(cityState);

        // 4. City only (useful for international addresses)
        if (!string.IsNullOrWhiteSpace(city) && !queries.Contains(city))
            queries.Add(city);

        return queries;
    }

    private async Task<(double Latitude, double Longitude)?> TryGeocodeQueryAsync(
        string query, CancellationToken cancellationToken)
    {
        await _rateLimiter.WaitAsync(cancellationToken);
        try
        {
            var encodedQuery = Uri.EscapeDataString(query);
            var url = $"https://nominatim.openstreetmap.org/search?q={encodedQuery}&format=json&limit=1";

            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var results = JsonSerializer.Deserialize<JsonElement[]>(json);

            if (results == null || results.Length == 0)
                return null;

            var first = results[0];
            var lat = double.Parse(first.GetProperty("lat").GetString()!);
            var lon = double.Parse(first.GetProperty("lon").GetString()!);

            return (lat, lon);
        }
        finally
        {
            // Nominatim TOS: max 1 request/second
            _ = Task.Delay(1000, CancellationToken.None).ContinueWith(_ => _rateLimiter.Release());
        }
    }
}
