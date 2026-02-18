using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace SpeakHalo.API.Handlers;

public class WebSocketHandler
{
    private readonly ILogger<WebSocketHandler> _logger;

    public WebSocketHandler(ILogger<WebSocketHandler> logger)
    {
        _logger = logger;
    }

    public async Task HandleWebSocketAsync(WebSocket webSocket, string userId)
    {
        var buffer = new byte[1024 * 4];

        try
        {
            while (webSocket.State == WebSocketState.Open)
            {
                var result = await webSocket.ReceiveAsync(
                    new ArraySegment<byte>(buffer),
                    CancellationToken.None
                );

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    _logger.LogDebug("Received WebSocket message from {UserId}, length: {Length}", userId, message.Length);

                    await ProcessVoiceDataAsync(webSocket, message, userId);
                }
                else if (result.MessageType == WebSocketMessageType.Close)
                {
                    await webSocket.CloseAsync(
                        WebSocketCloseStatus.NormalClosure,
                        "Closing",
                        CancellationToken.None
                    );
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WebSocket error for user {UserId}", userId);
        }
    }

    private async Task ProcessVoiceDataAsync(WebSocket webSocket, string message, string userId)
    {
        try
        {
            var data = JsonSerializer.Deserialize<VoiceData>(message);

            if (data?.Type == "audio")
            {
                var response = new
                {
                    type = "transcription",
                    text = "Processing audio...",
                    userId = userId,
                    timestamp = DateTime.UtcNow
                };

                var responseBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(response));
                await webSocket.SendAsync(
                    new ArraySegment<byte>(responseBytes),
                    WebSocketMessageType.Text,
                    true,
                    CancellationToken.None
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing voice data");
        }
    }

    private class VoiceData
    {
        public string? Type { get; set; }
        public string? Data { get; set; }
    }
}
