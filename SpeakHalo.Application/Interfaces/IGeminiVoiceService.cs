using System.Net.WebSockets;

namespace SpeakHalo.Application.Interfaces;

/// <summary>
/// Orchestrates real-time voice calls: Twilio Media Streams -> Google STT -> Gemini -> Google TTS
/// </summary>
public interface IGeminiVoiceService
{
    /// <summary>
    /// Handle a Twilio Media Stream WebSocket connection for a voice call.
    /// Manages the full audio pipeline: STT -> Gemini chat -> TTS -> audio back to caller.
    /// </summary>
    Task HandleMediaStreamAsync(
        WebSocket webSocket,
        string callSid,
        string agentId,
        string? callerPhone,
        CancellationToken cancellationToken);
}
