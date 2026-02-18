using FluentValidation;
using SpeakHalo.Application.DTOs;

namespace SpeakHalo.Application.Validators;

public class SendSMSRequestValidator : AbstractValidator<SendSMSRequest>
{
    public SendSMSRequestValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required");

        RuleFor(x => x.ToNumber)
            .NotEmpty().WithMessage("Phone number is required")
            .Matches(@"^\+?[1-9]\d{1,14}$").WithMessage("Invalid phone number format. Use E.164 format (e.g., +14155551234)");

        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Message content is required")
            .MaximumLength(1600).WithMessage("SMS content must be 1600 characters or less");
    }
}

public class SendWhatsAppMessageRequestValidator : AbstractValidator<SendWhatsAppMessageRequest>
{
    public SendWhatsAppMessageRequestValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required");

        RuleFor(x => x.ToNumber)
            .NotEmpty().WithMessage("Phone number is required")
            .Must(BeValidWhatsAppNumber).WithMessage("Invalid phone number format. Use E.164 format (e.g., +14155551234)");

        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Message content is required")
            .MaximumLength(4096).WithMessage("WhatsApp content must be 4096 characters or less");

        RuleFor(x => x.MediaUrl)
            .Must(BeValidUrl).When(x => !string.IsNullOrEmpty(x.MediaUrl))
            .WithMessage("Invalid media URL format");
    }

    private static bool BeValidWhatsAppNumber(string number)
    {
        // Strip whatsapp: prefix if present
        var cleaned = number.StartsWith("whatsapp:", StringComparison.OrdinalIgnoreCase)
            ? number.Substring(9)
            : number;
        return System.Text.RegularExpressions.Regex.IsMatch(cleaned, @"^\+?[1-9]\d{1,14}$");
    }

    private static bool BeValidUrl(string? url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uri)
            && (uri.Scheme == "https" || uri.Scheme == "http");
    }
}
