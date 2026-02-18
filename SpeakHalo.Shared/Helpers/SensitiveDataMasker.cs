using System.Text.RegularExpressions;

namespace SpeakHalo.Shared.Helpers;

public static partial class SensitiveDataMasker
{
    [GeneratedRegex(@"\b\d{16}\b", RegexOptions.Compiled)]
    private static partial Regex CreditCardRegex();

    [GeneratedRegex(@"\b\d{3}-\d{2}-\d{4}\b", RegexOptions.Compiled)]
    private static partial Regex SsnRegex();

    [GeneratedRegex(@"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b", RegexOptions.Compiled)]
    private static partial Regex EmailRegex();

    [GeneratedRegex(@"\b(?:\+?1[-.]?)?\(?([0-9]{3})\)?[-.]?([0-9]{3})[-.]?([0-9]{4})\b", RegexOptions.Compiled)]
    private static partial Regex PhoneRegex();

    public static string MaskSensitiveData(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return input;

        var masked = MaskCreditCards(input);
        masked = MaskSsn(masked);
        masked = MaskEmails(masked);
        masked = MaskPhoneNumbers(masked);
        masked = MaskPasswords(masked);
        masked = MaskApiKeys(masked);

        return masked;
    }

    public static string MaskCreditCards(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return input;

        return CreditCardRegex().Replace(input, "****-****-****-****");
    }

    public static string MaskSsn(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return input;

        return SsnRegex().Replace(input, "***-**-****");
    }

    public static string MaskEmails(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return input;

        return EmailRegex().Replace(input, match =>
        {
            var parts = match.Value.Split('@');
            if (parts.Length != 2)
                return match.Value;

            var localPart = parts[0].Length > 2 ? $"{parts[0][0]}***{parts[0][^1]}" : "***";
            return $"{localPart}@{parts[1]}";
        });
    }

    public static string MaskPhoneNumbers(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return input;

        return PhoneRegex().Replace(input, "***-***-****");
    }

    public static string MaskPasswords(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return input;

        var passwordPatterns = new[]
        {
            @"(""password""\s*:\s*"")[^""]*(""|$)",
            @"(""pwd""\s*:\s*"")[^""]*(""|$)",
            @"(""secret""\s*:\s*"")[^""]*(""|$)",
            @"(""apikey""\s*:\s*"")[^""]*(""|$)",
            @"(""api_key""\s*:\s*"")[^""]*(""|$)"
        };

        var masked = input;
        foreach (var pattern in passwordPatterns)
        {
            masked = Regex.Replace(masked, pattern, "$1***$2", RegexOptions.IgnoreCase);
        }

        return masked;
    }

    public static string MaskApiKeys(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return input;

        return Regex.Replace(input, @"\bsk_[A-Za-z0-9]{20,}\b", "sk_***", RegexOptions.Compiled);
    }

    public static string MaskToken(string token, int visibleChars = 4)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length <= visibleChars)
            return "***";

        return $"{token[..visibleChars]}{'*'.ToString().PadLeft(token.Length - visibleChars, '*')}";
    }
}
