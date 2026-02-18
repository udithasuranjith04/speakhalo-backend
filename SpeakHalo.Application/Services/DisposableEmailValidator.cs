using SpeakHalo.Application.Data;
using SpeakHalo.Application.Interfaces;

namespace SpeakHalo.Application.Services;

public class DisposableEmailValidator : IDisposableEmailValidator
{
    public bool IsDisposableEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        var atIndex = email.LastIndexOf('@');
        if (atIndex < 0 || atIndex >= email.Length - 1)
            return false;

        var domain = email[(atIndex + 1)..].Trim().ToLowerInvariant();
        return DisposableEmailDomains.Domains.Contains(domain);
    }
}
