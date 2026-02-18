using FluentValidation;
using SpeakHalo.Application.DTOs;

namespace SpeakHalo.Application.Validators;

public class CreateCallRequestValidator : AbstractValidator<CreateCallRequest>
{
    public CreateCallRequestValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required");

        RuleFor(x => x.FromNumber)
            .NotEmpty().WithMessage("From number is required")
            .Matches(@"^\+?[1-9]\d{1,14}$").WithMessage("Invalid phone number format");

        RuleFor(x => x.ToNumber)
            .NotEmpty().WithMessage("To number is required")
            .Matches(@"^\+?[1-9]\d{1,14}$").WithMessage("Invalid phone number format");

        RuleFor(x => x.Direction)
            .IsInEnum().WithMessage("Invalid call direction");
    }
}

public class UpdateCallRequestValidator : AbstractValidator<UpdateCallRequest>
{
    public UpdateCallRequestValidator()
    {
        When(x => x.DurationSeconds.HasValue, () =>
        {
            RuleFor(x => x.DurationSeconds)
                .GreaterThanOrEqualTo(0).WithMessage("Duration must be non-negative");
        });

        When(x => x.Outcome.HasValue, () =>
        {
            RuleFor(x => x.Outcome)
                .IsInEnum().WithMessage("Invalid call outcome");
        });

        When(x => x.Sentiment.HasValue, () =>
        {
            RuleFor(x => x.Sentiment)
                .IsInEnum().WithMessage("Invalid call sentiment");
        });

        When(x => x.ServiceType.HasValue, () =>
        {
            RuleFor(x => x.ServiceType)
                .IsInEnum().WithMessage("Invalid service type");
        });

        When(x => x.Summary != null, () =>
        {
            RuleFor(x => x.Summary)
                .MaximumLength(2000).WithMessage("Summary must not exceed 2000 characters");
        });

        When(x => x.ActionItems != null, () =>
        {
            RuleFor(x => x.ActionItems)
                .MaximumLength(2000).WithMessage("Action items must not exceed 2000 characters");
        });
    }
}
