using FluentValidation;
using SpeakHalo.Application.DTOs;

namespace SpeakHalo.Application.Validators;

public class CreateCustomerRequestValidator : AbstractValidator<CreateCustomerRequest>
{
    public CreateCustomerRequestValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required");

        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("Phone number is required")
            .Matches(@"^\+?[1-9]\d{1,14}$").WithMessage("Invalid phone number format");

        When(x => x.Name != null, () =>
        {
            RuleFor(x => x.Name)
                .MaximumLength(100).WithMessage("Name must not exceed 100 characters");
        });

        When(x => x.Email != null, () =>
        {
            RuleFor(x => x.Email)
                .EmailAddress().WithMessage("Invalid email format")
                .MaximumLength(255).WithMessage("Email must not exceed 255 characters");
        });

        When(x => x.Address != null, () =>
        {
            RuleFor(x => x.Address)
                .MaximumLength(500).WithMessage("Address must not exceed 500 characters");
        });

        When(x => x.Notes != null, () =>
        {
            RuleFor(x => x.Notes)
                .MaximumLength(2000).WithMessage("Notes must not exceed 2000 characters");
        });

        RuleFor(x => x.LeadStatus)
            .IsInEnum().WithMessage("Invalid lead status");
    }
}

public class UpdateCustomerRequestValidator : AbstractValidator<UpdateCustomerRequest>
{
    public UpdateCustomerRequestValidator()
    {
        When(x => x.Name != null, () =>
        {
            RuleFor(x => x.Name)
                .MaximumLength(100).WithMessage("Name must not exceed 100 characters");
        });

        When(x => x.Phone != null, () =>
        {
            RuleFor(x => x.Phone)
                .Matches(@"^\+?[1-9]\d{1,14}$").WithMessage("Invalid phone number format");
        });

        When(x => x.Email != null, () =>
        {
            RuleFor(x => x.Email)
                .EmailAddress().WithMessage("Invalid email format")
                .MaximumLength(255).WithMessage("Email must not exceed 255 characters");
        });

        When(x => x.Address != null, () =>
        {
            RuleFor(x => x.Address)
                .MaximumLength(500).WithMessage("Address must not exceed 500 characters");
        });

        When(x => x.Notes != null, () =>
        {
            RuleFor(x => x.Notes)
                .MaximumLength(2000).WithMessage("Notes must not exceed 2000 characters");
        });

        When(x => x.LeadStatus.HasValue, () =>
        {
            RuleFor(x => x.LeadStatus)
                .IsInEnum().WithMessage("Invalid lead status");
        });

        When(x => x.TotalCalls.HasValue, () =>
        {
            RuleFor(x => x.TotalCalls)
                .GreaterThanOrEqualTo(0).WithMessage("Total calls must be non-negative");
        });

        When(x => x.TotalAppointments.HasValue, () =>
        {
            RuleFor(x => x.TotalAppointments)
                .GreaterThanOrEqualTo(0).WithMessage("Total appointments must be non-negative");
        });

        When(x => x.TotalRevenue.HasValue, () =>
        {
            RuleFor(x => x.TotalRevenue)
                .GreaterThanOrEqualTo(0).WithMessage("Total revenue must be non-negative");
        });
    }
}
