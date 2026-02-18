using FluentValidation;
using SpeakHalo.Application.DTOs.Admin;

namespace SpeakHalo.Application.Validators;

public class AdminCreateUserRequestValidator : AbstractValidator<AdminCreateUserRequest>
{
    private static readonly string[] ValidVoiceNames =
        { "Kore", "Puck", "Charon", "Fenrir", "Aoede", "Leda", "Orus", "Zephyr" };

    public AdminCreateUserRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format")
            .MaximumLength(256).WithMessage("Email must not exceed 256 characters");

        RuleFor(x => x.Name)
            .MaximumLength(200).WithMessage("Name must not exceed 200 characters")
            .When(x => x.Name != null);

        RuleFor(x => x.BusinessName)
            .MaximumLength(200).WithMessage("Business name must not exceed 200 characters")
            .When(x => x.BusinessName != null);

        RuleFor(x => x.BusinessPhone)
            .MaximumLength(20).WithMessage("Phone must not exceed 20 characters")
            .Matches(@"^[\+\d\s\-\(\)]+$").WithMessage("Invalid phone number format")
            .When(x => !string.IsNullOrEmpty(x.BusinessPhone));

        RuleFor(x => x.BusinessAddress)
            .MaximumLength(500).WithMessage("Address must not exceed 500 characters")
            .When(x => x.BusinessAddress != null);

        RuleFor(x => x.BusinessWebsite)
            .MaximumLength(500).WithMessage("Website must not exceed 500 characters")
            .When(x => x.BusinessWebsite != null);

        RuleFor(x => x.AIMinutes)
            .InclusiveBetween(0, 999999).WithMessage("AI minutes must be between 0 and 999999")
            .When(x => x.AIMinutes.HasValue);

        RuleFor(x => x.WhatsAppMessages)
            .InclusiveBetween(0, 999999).WithMessage("WhatsApp messages must be between 0 and 999999")
            .When(x => x.WhatsAppMessages.HasValue);

        RuleFor(x => x.SMSMessages)
            .InclusiveBetween(0, 999999).WithMessage("SMS messages must be between 0 and 999999")
            .When(x => x.SMSMessages.HasValue);

        RuleFor(x => x.Images)
            .InclusiveBetween(0, 999999).WithMessage("Images must be between 0 and 999999")
            .When(x => x.Images.HasValue);

        RuleFor(x => x.AdminNotes)
            .MaximumLength(2000).WithMessage("Admin notes must not exceed 2000 characters")
            .When(x => x.AdminNotes != null);

        RuleFor(x => x.AgentName)
            .MaximumLength(100).WithMessage("Agent name must not exceed 100 characters")
            .When(x => x.AgentName != null);

        RuleFor(x => x.VoiceName)
            .Must(v => ValidVoiceNames.Contains(v))
            .WithMessage($"Voice must be one of: {string.Join(", ", ValidVoiceNames)}")
            .When(x => !string.IsNullOrEmpty(x.VoiceName));

        RuleFor(x => x.Greeting)
            .MaximumLength(1000).WithMessage("Greeting must not exceed 1000 characters")
            .When(x => x.Greeting != null);
    }
}

public class AdminUpdateLimitsRequestValidator : AbstractValidator<AdminUpdateLimitsRequest>
{
    public AdminUpdateLimitsRequestValidator()
    {
        RuleFor(x => x.AIMinutes)
            .InclusiveBetween(0, 999999).WithMessage("AI minutes must be between 0 and 999999")
            .When(x => x.AIMinutes.HasValue);

        RuleFor(x => x.WhatsAppMessages)
            .InclusiveBetween(0, 999999).WithMessage("WhatsApp messages must be between 0 and 999999")
            .When(x => x.WhatsAppMessages.HasValue);

        RuleFor(x => x.SMSMessages)
            .InclusiveBetween(0, 999999).WithMessage("SMS messages must be between 0 and 999999")
            .When(x => x.SMSMessages.HasValue);

        RuleFor(x => x.Images)
            .InclusiveBetween(0, 999999).WithMessage("Images must be between 0 and 999999")
            .When(x => x.Images.HasValue);
    }
}

public class AdminSuspendRequestValidator : AbstractValidator<AdminSuspendRequest>
{
    public AdminSuspendRequestValidator()
    {
        RuleFor(x => x.Reason)
            .MaximumLength(500).WithMessage("Reason must not exceed 500 characters")
            .When(x => x.Reason != null);
    }
}

public class AdminUpdateProfileRequestValidator : AbstractValidator<AdminUpdateProfileRequest>
{
    public AdminUpdateProfileRequestValidator()
    {
        RuleFor(x => x.Name)
            .MaximumLength(200).WithMessage("Name must not exceed 200 characters")
            .When(x => x.Name != null);

        RuleFor(x => x.BusinessName)
            .MaximumLength(200).WithMessage("Business name must not exceed 200 characters")
            .When(x => x.BusinessName != null);

        RuleFor(x => x.BusinessPhone)
            .MaximumLength(20).WithMessage("Phone must not exceed 20 characters")
            .Matches(@"^[\+\d\s\-\(\)]+$").WithMessage("Invalid phone number format")
            .When(x => !string.IsNullOrEmpty(x.BusinessPhone));

        RuleFor(x => x.BusinessAddress)
            .MaximumLength(500).WithMessage("Address must not exceed 500 characters")
            .When(x => x.BusinessAddress != null);

        RuleFor(x => x.BusinessWebsite)
            .MaximumLength(500).WithMessage("Website must not exceed 500 characters")
            .When(x => x.BusinessWebsite != null);

        RuleFor(x => x.IndustrySpecialization)
            .MaximumLength(200).WithMessage("Industry specialization must not exceed 200 characters")
            .When(x => x.IndustrySpecialization != null);
    }
}

public class AdminUpdateFeaturesRequestValidator : AbstractValidator<AdminUpdateFeaturesRequest>
{
    public AdminUpdateFeaturesRequestValidator()
    {
        // All fields are nullable booleans — no specific validation needed beyond type safety.
        // Validator exists for consistency and future extensibility.
    }
}

public class AdminUpdateAiConfigRequestValidator : AbstractValidator<AdminUpdateAiConfigRequest>
{
    private static readonly string[] ValidVoiceNames =
        { "Kore", "Puck", "Charon", "Fenrir", "Aoede", "Leda", "Orus", "Zephyr" };

    private static readonly string[] ValidPersonalityTones =
        { "professional", "friendly", "casual", "warm", "formal", "empathetic" };

    public AdminUpdateAiConfigRequestValidator()
    {
        RuleFor(x => x.AgentName)
            .MaximumLength(100).WithMessage("Agent name must not exceed 100 characters")
            .When(x => x.AgentName != null);

        RuleFor(x => x.VoiceName)
            .Must(v => ValidVoiceNames.Contains(v))
            .WithMessage($"Voice must be one of: {string.Join(", ", ValidVoiceNames)}")
            .When(x => !string.IsNullOrEmpty(x.VoiceName));

        RuleFor(x => x.Greeting)
            .MaximumLength(1000).WithMessage("Greeting must not exceed 1000 characters")
            .When(x => x.Greeting != null);

        RuleFor(x => x.SystemPrompt)
            .MaximumLength(5000).WithMessage("System prompt must not exceed 5000 characters")
            .When(x => x.SystemPrompt != null);

        RuleFor(x => x.PersonalityTone)
            .Must(t => ValidPersonalityTones.Contains(t))
            .WithMessage($"Personality tone must be one of: {string.Join(", ", ValidPersonalityTones)}")
            .When(x => !string.IsNullOrEmpty(x.PersonalityTone));

        RuleFor(x => x.Language)
            .MaximumLength(10).WithMessage("Language code must not exceed 10 characters")
            .When(x => x.Language != null);

        RuleFor(x => x.SpecialInstructions)
            .Must(list => list!.Count <= 20).WithMessage("Maximum 20 special instructions allowed")
            .When(x => x.SpecialInstructions != null);

        RuleForEach(x => x.SpecialInstructions)
            .MaximumLength(500).WithMessage("Each instruction must not exceed 500 characters")
            .When(x => x.SpecialInstructions != null);

        RuleFor(x => x.BusinessHoursMessage)
            .MaximumLength(1000).WithMessage("Business hours message must not exceed 1000 characters")
            .When(x => x.BusinessHoursMessage != null);

        RuleFor(x => x.AfterHoursMessage)
            .MaximumLength(1000).WithMessage("After hours message must not exceed 1000 characters")
            .When(x => x.AfterHoursMessage != null);
    }
}
