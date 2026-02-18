namespace SpeakHalo.Application.Interfaces;

public interface ILanguageDetectionService
{
    Task<string> DetectLanguageAsync(string text, CancellationToken cancellationToken = default);
    Task<Dictionary<string, double>> DetectLanguageWithConfidenceAsync(string text, CancellationToken cancellationToken = default);
    Task<bool> IsLanguageSupportedAsync(string languageCode);
    List<SupportedLanguage> GetSupportedLanguages();
}

public class SupportedLanguage
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string NativeName { get; set; } = string.Empty;
}
