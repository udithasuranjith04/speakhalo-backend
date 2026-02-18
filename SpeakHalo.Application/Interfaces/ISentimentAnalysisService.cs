namespace SpeakHalo.Application.Interfaces;

public interface ISentimentAnalysisService
{
    Task<SentimentResult> AnalyzeSentimentAsync(string text, CancellationToken cancellationToken = default);
    Task<List<SentimentResult>> AnalyzeBatchAsync(List<string> texts, CancellationToken cancellationToken = default);
    Task<EmotionAnalysisResult> AnalyzeEmotionsAsync(string text, CancellationToken cancellationToken = default);
}

public class SentimentResult
{
    public string Sentiment { get; set; } = "neutral"; // positive, negative, neutral
    public double PositiveScore { get; set; }
    public double NegativeScore { get; set; }
    public double NeutralScore { get; set; }
    public double ConfidenceScore { get; set; }
}

public class EmotionAnalysisResult
{
    public Dictionary<string, double> Emotions { get; set; } = new();
    public string DominantEmotion { get; set; } = string.Empty;
}
