namespace SpeakHalo.Application.Interfaces;

public interface IGeocodingService
{
    Task<(double Latitude, double Longitude)?> GeocodeAddressAsync(
        string? address, string? city, string? state, string? zipCode,
        CancellationToken cancellationToken = default);
}
