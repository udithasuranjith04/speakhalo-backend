namespace SpeakHalo.Application.DTOs.Payment;

public class PaddleSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;
    public string Environment { get; set; } = "sandbox";
    public string SandboxApiUrl { get; set; } = "https://sandbox-api.paddle.com";
    public string LiveApiUrl { get; set; } = "https://api.paddle.com";
    public string ClientSideToken { get; set; } = string.Empty;

    public string ApiUrl => Environment == "sandbox" ? SandboxApiUrl : LiveApiUrl;
}
