namespace SpeakHalo.Application.DTOs.Auth;

public record TotpVerifyRequest
{
    public required string Code { get; init; }
}

public record TotpLoginVerifyRequest
{
    public required string TwoFactorToken { get; init; }
    public required string Code { get; init; }
}

public record TotpSetupResponse
{
    public required string QrCodeUri { get; init; }
    public required string Secret { get; init; }
    public required List<string> BackupCodes { get; init; }
}
