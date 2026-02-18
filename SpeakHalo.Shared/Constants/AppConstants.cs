namespace SpeakHalo.Shared.Constants;

public static class AppConstants
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;
    public const int AccessTokenExpirationMinutes = 30;
    public const int RefreshTokenExpirationDays = 30;
    public const int PasswordResetTokenExpirationHours = 1;
    public const int BcryptWorkFactor = 12;
}

public static class ErrorMessages
{
    public const string InvalidCredentials = "Invalid email or password";
    public const string EmailAlreadyExists = "Email already exists";
    public const string UserNotFound = "User not found";
    public const string UnauthorizedAccess = "Unauthorized access";
    public const string InvalidToken = "Invalid or expired token";
    public const string TwoFactorRequired = "Two-factor authentication required";
    public const string InvalidTwoFactorCode = "Invalid two-factor code";

    public static string NotFound(string entityName) => $"{entityName} not found";
    public static string AlreadyExists(string entityName) => $"{entityName} already exists";
    public static string ValidationFailed(string message) => $"Validation failed: {message}";
}

public static class CacheKeys
{
    public static string UserById(string id) => $"user:{id}";
    public static string UserByEmail(string email) => $"user:email:{email}";
}
