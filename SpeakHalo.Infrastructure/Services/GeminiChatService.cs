using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SpeakHalo.Application.Interfaces;
using SpeakHalo.Shared.Models;

namespace SpeakHalo.Infrastructure.Services;

public class GeminiChatService : IAiChatService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GeminiChatService> _logger;
    private readonly string _apiKey;
    private readonly string _model;
    private readonly double _temperature;
    private readonly int _maxTokens;

    public GeminiChatService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<GeminiChatService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        _apiKey = configuration["Gemini:ApiKey"]
            ?? throw new ArgumentNullException("Gemini:ApiKey");
        _model = configuration["Gemini:Model"] ?? "gemini-2.0-flash";
        _temperature = double.TryParse(configuration["Gemini:Temperature"], out var temp) ? temp : 0.7;
        _maxTokens = int.TryParse(configuration["Gemini:MaxTokens"], out var max) ? max : 1000;

        _httpClient.BaseAddress = new Uri("https://generativelanguage.googleapis.com/v1beta/");
        _httpClient.DefaultRequestHeaders.Add("x-goog-api-key", _apiKey);
    }

    public async Task<Result<string>> TranscribeAudioAsync(
        Stream audioStream,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var memoryStream = new MemoryStream();
            await audioStream.CopyToAsync(memoryStream, cancellationToken);
            var audioBytes = memoryStream.ToArray();
            var base64Audio = Convert.ToBase64String(audioBytes);

            var requestBody = new
            {
                config = new
                {
                    encoding = "LINEAR16",
                    sampleRateHertz = 16000,
                    languageCode = "en-US"
                },
                audio = new
                {
                    content = base64Audio
                }
            };

            var content = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(
                $"https://speech.googleapis.com/v1/speech:recognize?key={_apiKey}",
                content,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Google STT error: {StatusCode} - {Error}", response.StatusCode, errorContent);
                return Result<string>.Failure($"Transcription error: {errorContent}");
            }

            var responseData = await response.Content.ReadAsStringAsync(cancellationToken);
            var jsonDoc = JsonDocument.Parse(responseData);

            if (jsonDoc.RootElement.TryGetProperty("results", out var results) &&
                results.GetArrayLength() > 0 &&
                results[0].TryGetProperty("alternatives", out var alternatives) &&
                alternatives.GetArrayLength() > 0 &&
                alternatives[0].TryGetProperty("transcript", out var transcriptElement))
            {
                return Result<string>.Success(transcriptElement.GetString() ?? string.Empty);
            }

            return Result<string>.Success(string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to transcribe audio with Google STT");
            return Result<string>.Failure($"Failed to transcribe audio: {ex.Message}");
        }
    }

    public async Task<Result<string>> GenerateSpeechAsync(
        string text,
        string voice = "Kore",
        CancellationToken cancellationToken = default)
    {
        try
        {
            var requestBody = new
            {
                input = new { text },
                voice = new
                {
                    languageCode = "en-US",
                    name = voice
                },
                audioConfig = new
                {
                    audioEncoding = "MP3"
                }
            };

            var content = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(
                $"https://texttospeech.googleapis.com/v1/text:synthesize?key={_apiKey}",
                content,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Google TTS error: {StatusCode} - {Error}", response.StatusCode, errorContent);
                return Result<string>.Failure($"Speech generation error: {errorContent}");
            }

            var responseData = await response.Content.ReadAsStringAsync(cancellationToken);
            var jsonDoc = JsonDocument.Parse(responseData);

            if (!jsonDoc.RootElement.TryGetProperty("audioContent", out var audioElement))
            {
                _logger.LogError("Google TTS response missing audioContent property");
                return Result<string>.Failure("Speech generation returned no audio content");
            }

            return Result<string>.Success(audioElement.GetString() ?? string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate speech with Google TTS");
            return Result<string>.Failure($"Failed to generate speech: {ex.Message}");
        }
    }

    public async Task<Result<string>> ChatCompletionAsync(
        string systemPrompt,
        string userMessage,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        role = "user",
                        parts = new[] { new { text = userMessage } }
                    }
                },
                systemInstruction = new
                {
                    parts = new[] { new { text = systemPrompt } }
                },
                generationConfig = new
                {
                    temperature = _temperature,
                    maxOutputTokens = _maxTokens
                }
            };

            var content = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(
                $"models/{_model}:generateContent",
                content,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Gemini API error: {StatusCode} - {Error}", response.StatusCode, errorContent);
                return Result<string>.Failure($"Chat completion error: {errorContent}");
            }

            var responseData = await response.Content.ReadAsStringAsync(cancellationToken);
            var geminiResponse = JsonSerializer.Deserialize<GeminiResponse>(responseData);

            var text = geminiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;

            return Result<string>.Success(text ?? string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get chat completion from Gemini");
            return Result<string>.Failure($"Failed to get chat completion: {ex.Message}");
        }
    }

    public async Task<Result<string>> FunctionCallingAsync(
        string prompt,
        List<object> functions,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        role = "user",
                        parts = new[] { new { text = prompt } }
                    }
                },
                tools = new[]
                {
                    new { functionDeclarations = functions }
                },
                generationConfig = new
                {
                    temperature = _temperature,
                    maxOutputTokens = _maxTokens
                }
            };

            var content = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(
                $"models/{_model}:generateContent",
                content,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Gemini function calling error: {StatusCode} - {Error}", response.StatusCode, errorContent);
                return Result<string>.Failure($"Function calling error: {errorContent}");
            }

            var responseData = await response.Content.ReadAsStringAsync(cancellationToken);
            return Result<string>.Success(responseData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute function calling with Gemini");
            return Result<string>.Failure($"Failed to execute function calling: {ex.Message}");
        }
    }

    private class GeminiResponse
    {
        [JsonPropertyName("candidates")]
        public List<Candidate>? Candidates { get; set; }
    }

    private class Candidate
    {
        [JsonPropertyName("content")]
        public ContentBlock? Content { get; set; }
    }

    private class ContentBlock
    {
        [JsonPropertyName("parts")]
        public List<Part>? Parts { get; set; }
    }

    private class Part
    {
        [JsonPropertyName("text")]
        public string? Text { get; set; }
    }
}
