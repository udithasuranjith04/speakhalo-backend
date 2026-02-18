namespace SpeakHalo.Application.Exceptions;

/// <summary>
/// Thrown when Twilio rate limit (429) is hit
/// </summary>
public class TwilioRateLimitException : Exception
{
    public TwilioRateLimitException(string message) : base(message) { }
    public TwilioRateLimitException(string message, Exception inner) : base(message, inner) { }
}

/// <summary>
/// Thrown when no phone numbers are available in requested country
/// </summary>
public class TwilioNoNumbersAvailableException : Exception
{
    public TwilioNoNumbersAvailableException(string message) : base(message) { }
    public TwilioNoNumbersAvailableException(string message, Exception inner) : base(message, inner) { }
}

/// <summary>
/// Thrown when regulatory compliance is required
/// </summary>
public class TwilioRegulatoryComplianceException : Exception
{
    public TwilioRegulatoryComplianceException(string message) : base(message) { }
    public TwilioRegulatoryComplianceException(string message, Exception inner) : base(message, inner) { }
}

/// <summary>
/// Thrown when Twilio service is unavailable (503)
/// </summary>
public class TwilioServiceUnavailableException : Exception
{
    public TwilioServiceUnavailableException(string message) : base(message) { }
    public TwilioServiceUnavailableException(string message, Exception inner) : base(message, inner) { }
}
