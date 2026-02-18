using FluentValidation;
using SpeakHalo.Application.DTOs;

namespace SpeakHalo.Application.Validators;

public class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserRequestValidator()
    {
        When(x => x.Name != null, () =>
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name cannot be empty")
                .MaximumLength(100).WithMessage("Name must not exceed 100 characters");
        });

        When(x => x.BusinessName != null, () =>
        {
            RuleFor(x => x.BusinessName)
                .NotEmpty().WithMessage("Business name cannot be empty")
                .MaximumLength(200).WithMessage("Business name must not exceed 200 characters");
        });
    }
}

public class UpdatePasswordRequestValidator : AbstractValidator<UpdatePasswordRequest>
{
    public UpdatePasswordRequestValidator()
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty().WithMessage("Current password is required");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("New password is required")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters")
            .MaximumLength(100).WithMessage("Password must not exceed 100 characters")
            .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter")
            .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter")
            .Matches(@"[0-9]").WithMessage("Password must contain at least one digit")
            .Matches(@"[!@#$%^&*(),.?""':;{}\[\]|<>_\-+=~`]").WithMessage("Password must contain at least one special character (!@#$%^&* etc.)")
            .NotEqual(x => x.CurrentPassword).WithMessage("New password must be different from current password");
    }
}
