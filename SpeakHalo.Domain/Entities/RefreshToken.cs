namespace SpeakHalo.Domain.Entities;

public class RefreshToken : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public bool Revoked { get; set; } = false;

    public string? DeviceName { get; set; }
    public string? DeviceType { get; set; }
    public string? Browser { get; set; }
    public string? Os { get; set; }
    public string? IpAddress { get; set; }
    public string? Location { get; set; }
    public DateTime LastActive { get; set; } = DateTime.UtcNow;

    public virtual User User { get; set; } = null!;
}
