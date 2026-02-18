using SpeakHalo.Shared.Models;

namespace SpeakHalo.Application.Interfaces;

public interface IAiChatService
{
    Task<Result<string>> TranscribeAudioAsync(Stream audioStream, CancellationToken cancellationToken = default);
    Task<Result<string>> GenerateSpeechAsync(string text, string voice = "Kore", CancellationToken cancellationToken = default);
    Task<Result<string>> ChatCompletionAsync(string systemPrompt, string userMessage, CancellationToken cancellationToken = default);
    Task<Result<string>> FunctionCallingAsync(string prompt, List<object> functions, CancellationToken cancellationToken = default);
}
