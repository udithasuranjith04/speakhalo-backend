using System.Text.RegularExpressions;
using FluentValidation;
using SpeakHalo.Application.DTOs.Auth;

namespace SpeakHalo.Application.Validators;

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required");
    }
}

public class RefreshTokenRequestValidator : AbstractValidator<RefreshTokenRequest>
{
    public RefreshTokenRequestValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("Refresh token is required");
    }
}

public class ForceChangePasswordRequestValidator : AbstractValidator<ForceChangePasswordRequest>
{
    public ForceChangePasswordRequestValidator()
    {
        RuleFor(x => x.PasswordChangeToken)
            .NotEmpty().WithMessage("Password change token is required");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("New password is required")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters")
            .MaximumLength(128).WithMessage("Password must not exceed 128 characters")
            .Must(PasswordRules.ContainUppercase).WithMessage("Password must contain at least one uppercase letter")
            .Must(PasswordRules.ContainLowercase).WithMessage("Password must contain at least one lowercase letter")
            .Must(PasswordRules.ContainDigit).WithMessage("Password must contain at least one digit")
            .Must(PasswordRules.ContainSpecialChar).WithMessage("Password must contain at least one special character (!@#$%^&*)");

        RuleFor(x => x.ConfirmPassword)
            .NotEmpty().WithMessage("Password confirmation is required")
            .Equal(x => x.NewPassword).WithMessage("Passwords do not match");
    }
}

public class ForgotPasswordRequestValidator : AbstractValidator<ForgotPasswordRequest>
{
    public ForgotPasswordRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format")
            .MaximumLength(256).WithMessage("Email must not exceed 256 characters");
    }
}

public class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format");

        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Reset token is required")
            .MaximumLength(256).WithMessage("Invalid token format");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("New password is required")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters")
            .MaximumLength(128).WithMessage("Password must not exceed 128 characters")
            .Must(PasswordRules.ContainUppercase).WithMessage("Password must contain at least one uppercase letter")
            .Must(PasswordRules.ContainLowercase).WithMessage("Password must contain at least one lowercase letter")
            .Must(PasswordRules.ContainDigit).WithMessage("Password must contain at least one digit")
            .Must(PasswordRules.ContainSpecialChar).WithMessage("Password must contain at least one special character (!@#$%^&*)");

        RuleFor(x => x.ConfirmPassword)
            .NotEmpty().WithMessage("Password confirmation is required")
            .Equal(x => x.NewPassword).WithMessage("Passwords do not match");
    }
}

internal static partial class PasswordRules
{
    public static bool ContainUppercase(string password) => UppercaseRegex().IsMatch(password);
    public static bool ContainLowercase(string password) => LowercaseRegex().IsMatch(password);
    public static bool ContainDigit(string password) => DigitRegex().IsMatch(password);
    public static bool ContainSpecialChar(string password) => SpecialCharRegex().IsMatch(password);

    [GeneratedRegex("[A-Z]")]
    private static partial Regex UppercaseRegex();

    [GeneratedRegex("[a-z]")]
    private static partial Regex LowercaseRegex();

    [GeneratedRegex("[0-9]")]
    private static partial Regex DigitRegex();

    [GeneratedRegex("[!@#$%^&*()_+\\-=\\[\\]{};':\"\\\\|,.<>\\/?]")]
    private static partial Regex SpecialCharRegex();
}
