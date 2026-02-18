using System.Text.RegularExpressions;
using System.Web;

namespace SpeakHalo.Shared.Helpers;

public static partial class InputSanitizer
{
    [GeneratedRegex(@"<[^>]*>", RegexOptions.Compiled)]
    private static partial Regex HtmlTagRegex();

    [GeneratedRegex(@"on\w+\s*=", RegexOptions.IgnoreCase)]
    private static partial Regex EventHandlerRegex();

    [GeneratedRegex(@"javascript:", RegexOptions.IgnoreCase)]
    private static partial Regex JavaScriptProtocolRegex();

    /// <summary>
    /// Strips ALL HTML tags and removes event handlers / javascript: URIs.
    /// For display contexts, prefer SanitizeForDisplay (HtmlEncode) instead.
    /// </summary>
    public static string SanitizeHtml(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return input;

        // Strip ALL HTML tags (not just script)
        var sanitized = HtmlTagRegex().Replace(input, string.Empty);
        sanitized = EventHandlerRegex().Replace(sanitized, string.Empty);
        sanitized = JavaScriptProtocolRegex().Replace(sanitized, string.Empty);

        return sanitized;
    }

    public static string SanitizeForDisplay(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return input;

        return HttpUtility.HtmlEncode(input);
    }

    public static string RemoveControlCharacters(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return input;

        return Regex.Replace(input, @"[\x00-\x08\x0B\x0C\x0E-\x1F\x7F]", string.Empty);
    }

    public static string TruncateWithEllipsis(string input, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(input) || input.Length <= maxLength)
            return input;

        return string.Concat(input.AsSpan(0, maxLength - 3), "...");
    }

    public static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    public static bool ContainsXssPatterns(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return false;

        var patterns = new[]
        {
            @"<script",
            @"javascript:",
            @"onerror\s*=",
            @"onclick\s*=",
            @"onload\s*="
        };

        return patterns.Any(pattern => Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase));
    }

    /// <summary>
    /// Sanitizes transcript text - preserves readability while removing malicious content
    /// </summary>
    /// <param name="input">The transcript text to sanitize</param>
    /// <param name="maxLength">Maximum length (default 10000 characters)</param>
    /// <returns>Sanitized transcript text</returns>
    public static string SanitizeTranscriptText(string? input, int maxLength = 10000)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        // Remove script tags and malicious content
        var sanitized = SanitizeHtml(input);

        // Remove control characters except newline, carriage return, and tab
        sanitized = Regex.Replace(sanitized, @"[\x00-\x08\x0B\x0C\x0E-\x1F\x7F]", string.Empty);

        // Trim each line
        var lines = sanitized.Split(new[] { '\r', '\n' }, StringSplitOptions.None);
        sanitized = string.Join("\n", lines.Select(line => line.Trim()));

        // Remove excessive consecutive newlines (more than 2)
        sanitized = Regex.Replace(sanitized, @"\n{3,}", "\n\n");

        // Trim
        sanitized = sanitized.Trim();

        // Apply max length
        if (sanitized.Length > maxLength)
        {
            sanitized = sanitized[..maxLength];
        }

        return sanitized;
    }

    /// <summary>
    /// Sanitizes speaker name - allows only alphanumeric, spaces, hyphens, and basic punctuation
    /// </summary>
    /// <param name="input">The speaker name to sanitize</param>
    /// <param name="maxLength">Maximum length (default 100)</param>
    /// <returns>Sanitized speaker name</returns>
    public static string SanitizeSpeakerName(string? input, int maxLength = 100)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        // Remove HTML and malicious content
        var sanitized = SanitizeHtml(input);

        // Remove control characters
        sanitized = RemoveControlCharacters(sanitized);

        // Allow only alphanumeric, spaces, hyphens, apostrophes, periods, and underscores
        sanitized = Regex.Replace(sanitized, @"[^a-zA-Z0-9\s\-'._]", string.Empty);

        // Normalize whitespace
        sanitized = Regex.Replace(sanitized, @"\s+", " ");

        // Trim
        sanitized = sanitized.Trim();

        // Apply max length
        if (sanitized.Length > maxLength)
        {
            sanitized = sanitized[..maxLength];
        }

        return sanitized;
    }
}
