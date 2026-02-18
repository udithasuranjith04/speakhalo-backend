using SpeakHalo.Shared.Models;

namespace SpeakHalo.Application.Interfaces;

public interface IEmailService
{
    Task<Result<bool>> SendEmailAsync(string toEmail, string subject, string htmlContent, string? fromEmail = null, CancellationToken cancellationToken = default);
    Task<Result<bool>> SendTemplateEmailAsync(string toEmail, string templateId, Dictionary<string, string> templateData, CancellationToken cancellationToken = default);
    Task<Result<bool>> SendBulkEmailAsync(List<string> toEmails, string subject, string htmlContent, CancellationToken cancellationToken = default);
    Task<Result<bool>> SendTeamInviteEmailAsync(string toEmail, string teamName, string inviterName, string inviteToken, CancellationToken cancellationToken = default);
}
