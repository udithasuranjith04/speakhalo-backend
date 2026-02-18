using FluentValidation;
using SpeakHalo.Application.DTOs;

namespace SpeakHalo.Application.Validators;

public class CreateAppointmentRequestValidator : AbstractValidator<CreateAppointmentRequest>
{
    public CreateAppointmentRequestValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required");

        RuleFor(x => x.CustomerId)
            .NotEmpty().WithMessage("Customer ID is required");

        RuleFor(x => x.ScheduledAt)
            .NotEmpty().WithMessage("Scheduled date/time is required")
            .GreaterThan(DateTime.UtcNow).WithMessage("Scheduled date/time must be in the future");

        RuleFor(x => x.DurationMinutes)
            .GreaterThan(0).WithMessage("Duration must be greater than 0")
            .LessThanOrEqualTo(480).WithMessage("Duration must not exceed 8 hours");

        When(x => x.ServiceType.HasValue, () =>
        {
            RuleFor(x => x.ServiceType)
                .IsInEnum().WithMessage("Invalid service type");
        });

        When(x => x.Location != null, () =>
        {
            RuleFor(x => x.Location)
                .MaximumLength(500).WithMessage("Location must not exceed 500 characters");
        });

        When(x => x.Notes != null, () =>
        {
            RuleFor(x => x.Notes)
                .MaximumLength(2000).WithMessage("Notes must not exceed 2000 characters");
        });
    }
}

public class UpdateAppointmentRequestValidator : AbstractValidator<UpdateAppointmentRequest>
{
    public UpdateAppointmentRequestValidator()
    {
        When(x => x.ScheduledAt.HasValue, () =>
        {
            RuleFor(x => x.ScheduledAt)
                .GreaterThan(DateTime.UtcNow).WithMessage("Scheduled date/time must be in the future");
        });

        When(x => x.DurationMinutes.HasValue, () =>
        {
            RuleFor(x => x.DurationMinutes)
                .GreaterThan(0).WithMessage("Duration must be greater than 0")
                .LessThanOrEqualTo(480).WithMessage("Duration must not exceed 8 hours");
        });

        When(x => x.Status.HasValue, () =>
        {
            RuleFor(x => x.Status)
                .IsInEnum().WithMessage("Invalid appointment status");
        });

        When(x => x.ServiceType.HasValue, () =>
        {
            RuleFor(x => x.ServiceType)
                .IsInEnum().WithMessage("Invalid service type");
        });

        When(x => x.Location != null, () =>
        {
            RuleFor(x => x.Location)
                .MaximumLength(500).WithMessage("Location must not exceed 500 characters");
        });

        When(x => x.Notes != null, () =>
        {
            RuleFor(x => x.Notes)
                .MaximumLength(2000).WithMessage("Notes must not exceed 2000 characters");
        });

        When(x => x.CompletionNotes != null, () =>
        {
            RuleFor(x => x.CompletionNotes)
                .MaximumLength(2000).WithMessage("Completion notes must not exceed 2000 characters");
        });
    }
}
