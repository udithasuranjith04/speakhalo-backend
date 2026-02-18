using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using SpeakHalo.Application.Interfaces;

namespace SpeakHalo.Application.Services;

public class LanguageDetectionService : ILanguageDetectionService
{
    private readonly ILogger<LanguageDetectionService> _logger;
    private readonly List<SupportedLanguage> _supportedLanguages;

    public LanguageDetectionService(ILogger<LanguageDetectionService> logger)
    {
        _logger = logger;
        _supportedLanguages = InitializeSupportedLanguages();
    }

    public async Task<string> DetectLanguageAsync(string text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "en"; // Default to English

        // Simple language detection based on character patterns
        // In production, use a library like NTextCat or call Azure Cognitive Services
        var result = await Task.Run(() => DetectLanguageByPattern(text), cancellationToken);
        return result;
    }

    public async Task<Dictionary<string, double>> DetectLanguageWithConfidenceAsync(string text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new Dictionary<string, double> { { "en", 1.0 } };
        }

        var detectedLanguage = await DetectLanguageAsync(text, cancellationToken);

        return new Dictionary<string, double>
        {
            { detectedLanguage, 0.95 },
            { "en", 0.05 }
        };
    }

    public Task<bool> IsLanguageSupportedAsync(string languageCode)
    {
        var isSupported = _supportedLanguages.Any(l => l.Code.Equals(languageCode, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(isSupported);
    }

    public List<SupportedLanguage> GetSupportedLanguages()
    {
        return _supportedLanguages;
    }

    private string DetectLanguageByPattern(string text)
    {
        // Simple pattern-based detection
        // In production, use a proper language detection library

        // Check for Chinese characters
        if (Regex.IsMatch(text, @"[\u4e00-\u9fff]"))
            return "zh";

        // Check for Japanese characters
        if (Regex.IsMatch(text, @"[\u3040-\u309f\u30a0-\u30ff]"))
            return "ja";

        // Check for Korean characters
        if (Regex.IsMatch(text, @"[\uac00-\ud7af]"))
            return "ko";

        // Check for Arabic characters
        if (Regex.IsMatch(text, @"[\u0600-\u06ff]"))
            return "ar";

        // Check for Hebrew characters
        if (Regex.IsMatch(text, @"[\u0590-\u05ff]"))
            return "he";

        // Check for Cyrillic characters (Russian)
        if (Regex.IsMatch(text, @"[\u0400-\u04ff]"))
            return "ru";

        // Check for common Spanish words
        if (ContainsCommonWords(text, new[] { "el", "la", "de", "que", "en", "es", "por", "un", "con", "año" }))
            return "es";

        // Check for common French words
        if (ContainsCommonWords(text, new[] { "le", "de", "un", "être", "et", "à", "il", "avoir", "ne", "je" }))
            return "fr";

        // Check for common German words
        if (ContainsCommonWords(text, new[] { "der", "die", "und", "in", "den", "von", "zu", "das", "mit", "sich" }))
            return "de";

        // Check for common Portuguese words
        if (ContainsCommonWords(text, new[] { "o", "a", "de", "que", "e", "do", "da", "em", "um", "para" }))
            return "pt";

        // Default to English
        return "en";
    }

    private bool ContainsCommonWords(string text, string[] commonWords)
    {
        var lowerText = text.ToLowerInvariant();
        var words = lowerText.Split(new[] { ' ', ',', '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);
        var matchCount = words.Count(w => commonWords.Contains(w));

        return matchCount >= 2; // At least 2 common words found
    }

    private List<SupportedLanguage> InitializeSupportedLanguages()
    {
        return new List<SupportedLanguage>
        {
            new SupportedLanguage { Code = "en", Name = "English", NativeName = "English" },
            new SupportedLanguage { Code = "es", Name = "Spanish", NativeName = "Español" },
            new SupportedLanguage { Code = "fr", Name = "French", NativeName = "Français" },
            new SupportedLanguage { Code = "de", Name = "German", NativeName = "Deutsch" },
            new SupportedLanguage { Code = "pt", Name = "Portuguese", NativeName = "Português" },
            new SupportedLanguage { Code = "zh", Name = "Chinese", NativeName = "中文" },
            new SupportedLanguage { Code = "ja", Name = "Japanese", NativeName = "日本語" },
            new SupportedLanguage { Code = "ko", Name = "Korean", NativeName = "한국어" },
            new SupportedLanguage { Code = "ar", Name = "Arabic", NativeName = "العربية" },
            new SupportedLanguage { Code = "ru", Name = "Russian", NativeName = "Русский" },
            new SupportedLanguage { Code = "it", Name = "Italian", NativeName = "Italiano" },
            new SupportedLanguage { Code = "nl", Name = "Dutch", NativeName = "Nederlands" },
            new SupportedLanguage { Code = "pl", Name = "Polish", NativeName = "Polski" },
            new SupportedLanguage { Code = "tr", Name = "Turkish", NativeName = "Türkçe" },
            new SupportedLanguage { Code = "he", Name = "Hebrew", NativeName = "עברית" }
        };
    }
}
