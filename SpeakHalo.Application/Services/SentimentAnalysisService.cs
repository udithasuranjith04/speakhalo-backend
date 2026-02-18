using System.Text.Json;
using Microsoft.Extensions.Logging;
using SpeakHalo.Application.Interfaces;

namespace SpeakHalo.Application.Services;

public class SentimentAnalysisService : ISentimentAnalysisService
{
    private readonly IAiChatService _chatService;
    private readonly ILogger<SentimentAnalysisService> _logger;

    public SentimentAnalysisService(IAiChatService chatService, ILogger<SentimentAnalysisService> logger)
    {
        _chatService = chatService;
        _logger = logger;
    }

    public async Task<SentimentResult> AnalyzeSentimentAsync(string text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new SentimentResult
            {
                Sentiment = "neutral",
                NeutralScore = 1.0,
                ConfidenceScore = 1.0
            };
        }

        try
        {
            var systemPrompt = @"You are a sentiment analysis engine for a cleaning service business call center. Analyze the given text and return ONLY a JSON object with these fields:
- sentiment: ""positive"", ""negative"", or ""neutral""
- positiveScore: 0.0 to 1.0
- negativeScore: 0.0 to 1.0
- neutralScore: 0.0 to 1.0
- confidenceScore: 0.0 to 1.0
The three scores must sum to 1.0. Return ONLY the JSON, no other text.";

            // Truncate very long texts to save tokens
            var truncated = text.Length > 2000 ? text[..2000] : text;
            var result = await _chatService.ChatCompletionAsync(systemPrompt, truncated, cancellationToken);

            if (result.IsSuccess && !string.IsNullOrEmpty(result.Data))
            {
                return ParseSentimentJson(result.Data);
            }

            _logger.LogWarning("AI sentiment analysis returned no result, using neutral default");
            return new SentimentResult { Sentiment = "neutral", NeutralScore = 1.0, ConfidenceScore = 0.5 };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AI sentiment analysis failed, using neutral default");
            return new SentimentResult { Sentiment = "neutral", NeutralScore = 1.0, ConfidenceScore = 0.5 };
        }
    }

    public async Task<List<SentimentResult>> AnalyzeBatchAsync(List<string> texts, CancellationToken cancellationToken = default)
    {
        var results = new List<SentimentResult>();
        foreach (var text in texts)
        {
            var result = await AnalyzeSentimentAsync(text, cancellationToken);
            results.Add(result);
        }
        return results;
    }

    public async Task<EmotionAnalysisResult> AnalyzeEmotionsAsync(string text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new EmotionAnalysisResult
            {
                DominantEmotion = "neutral",
                Emotions = new Dictionary<string, double> { { "neutral", 1.0 } }
            };
        }

        try
        {
            var systemPrompt = @"You are an emotion analysis engine for a cleaning service business call center. Analyze the given text and return ONLY a JSON object with these fields:
- dominantEmotion: one of ""joy"", ""sadness"", ""anger"", ""fear"", ""surprise"", ""neutral""
- emotions: object with keys ""joy"", ""sadness"", ""anger"", ""fear"", ""surprise"", ""neutral"" each with a 0.0 to 1.0 score
The emotion scores must sum to 1.0. Return ONLY the JSON, no other text.";

            var truncated = text.Length > 2000 ? text[..2000] : text;
            var result = await _chatService.ChatCompletionAsync(systemPrompt, truncated, cancellationToken);

            if (result.IsSuccess && !string.IsNullOrEmpty(result.Data))
            {
                return ParseEmotionJson(result.Data);
            }

            _logger.LogWarning("AI emotion analysis returned no result, using neutral default");
            return new EmotionAnalysisResult
            {
                DominantEmotion = "neutral",
                Emotions = new Dictionary<string, double> { { "neutral", 1.0 } }
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AI emotion analysis failed, using neutral default");
            return new EmotionAnalysisResult
            {
                DominantEmotion = "neutral",
                Emotions = new Dictionary<string, double> { { "neutral", 1.0 } }
            };
        }
    }

    private static SentimentResult ParseSentimentJson(string json)
    {
        // Strip markdown code fences if present
        json = json.Trim();
        if (json.StartsWith("```"))
        {
            var startIdx = json.IndexOf('{');
            var endIdx = json.LastIndexOf('}');
            if (startIdx >= 0 && endIdx > startIdx)
                json = json[startIdx..(endIdx + 1)];
        }

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        return new SentimentResult
        {
            Sentiment = root.TryGetProperty("sentiment", out var s) ? s.GetString() ?? "neutral" : "neutral",
            PositiveScore = root.TryGetProperty("positiveScore", out var ps) ? ps.GetDouble() : 0,
            NegativeScore = root.TryGetProperty("negativeScore", out var ns) ? ns.GetDouble() : 0,
            NeutralScore = root.TryGetProperty("neutralScore", out var nts) ? nts.GetDouble() : 1.0,
            ConfidenceScore = root.TryGetProperty("confidenceScore", out var cs) ? cs.GetDouble() : 0.5
        };
    }

    private static EmotionAnalysisResult ParseEmotionJson(string json)
    {
        // Strip markdown code fences if present
        json = json.Trim();
        if (json.StartsWith("```"))
        {
            var startIdx = json.IndexOf('{');
            var endIdx = json.LastIndexOf('}');
            if (startIdx >= 0 && endIdx > startIdx)
                json = json[startIdx..(endIdx + 1)];
        }

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var dominantEmotion = root.TryGetProperty("dominantEmotion", out var de) ? de.GetString() ?? "neutral" : "neutral";
        var emotions = new Dictionary<string, double>();

        if (root.TryGetProperty("emotions", out var emotionsElement))
        {
            foreach (var prop in emotionsElement.EnumerateObject())
            {
                if (prop.Value.TryGetDouble(out var score))
                    emotions[prop.Name] = score;
            }
        }

        if (emotions.Count == 0)
            emotions["neutral"] = 1.0;

        return new EmotionAnalysisResult
        {
            DominantEmotion = dominantEmotion,
            Emotions = emotions
        };
    }
}
