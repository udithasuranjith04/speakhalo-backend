using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SendGrid;
using SendGrid.Helpers.Mail;
using SpeakHalo.Application.Interfaces;
using SpeakHalo.Shared.Models;

namespace SpeakHalo.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly string _apiKey;
    private readonly string _fromEmail;
    private readonly string _fromName;
    private readonly string _frontendBaseUrl;
    private readonly SendGridClient _client;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _logger = logger;
        _apiKey = configuration["SendGrid:ApiKey"] ?? throw new ArgumentNullException("SendGrid:ApiKey");
        _fromEmail = configuration["SendGrid:FromEmail"] ?? "noreply@speakhalo.com";
        _fromName = configuration["SendGrid:FromName"] ?? "SpeakHalo";
        _frontendBaseUrl = configuration["Frontend:BaseUrl"]?.TrimEnd('/') ?? "https://app.speakhalo.com";

        _client = new SendGridClient(_apiKey);
    }

    public async Task<Result<bool>> SendEmailAsync(string toEmail, string subject, string htmlContent, string? fromEmail = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var from = new EmailAddress(fromEmail ?? _fromEmail, _fromName);
            var to = new EmailAddress(toEmail);
            var msg = MailHelper.CreateSingleEmail(from, to, subject, null, htmlContent);

            var response = await _client.SendEmailAsync(msg, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return Result<bool>.Success(true);
            }

            var errorBody = await response.Body.ReadAsStringAsync(cancellationToken);
            return Result<bool>.Failure($"Failed to send email: {errorBody}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {ToEmail}", toEmail);
            return Result<bool>.Failure($"Failed to send email: {ex.Message}");
        }
    }

    public async Task<Result<bool>> SendTemplateEmailAsync(string toEmail, string templateId, Dictionary<string, string> templateData, CancellationToken cancellationToken = default)
    {
        try
        {
            var from = new EmailAddress(_fromEmail, _fromName);
            var to = new EmailAddress(toEmail);
            var msg = MailHelper.CreateSingleTemplateEmail(from, to, templateId, templateData);

            var response = await _client.SendEmailAsync(msg, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return Result<bool>.Success(true);
            }

            var errorBody = await response.Body.ReadAsStringAsync(cancellationToken);
            return Result<bool>.Failure($"Failed to send template email: {errorBody}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send template email to {ToEmail} with template {TemplateId}", toEmail, templateId);
            return Result<bool>.Failure($"Failed to send template email: {ex.Message}");
        }
    }

    public async Task<Result<bool>> SendBulkEmailAsync(List<string> toEmails, string subject, string htmlContent, CancellationToken cancellationToken = default)
    {
        try
        {
            var from = new EmailAddress(_fromEmail, _fromName);
            var tos = toEmails.Select(email => new EmailAddress(email)).ToList();

            // showAllRecipients: false ensures each recipient only sees their own address
            var msg = MailHelper.CreateSingleEmailToMultipleRecipients(from, tos, subject, null, htmlContent, showAllRecipients: false);

            var response = await _client.SendEmailAsync(msg, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return Result<bool>.Success(true);
            }

            var errorBody = await response.Body.ReadAsStringAsync(cancellationToken);
            return Result<bool>.Failure($"Failed to send bulk email: {errorBody}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send bulk email to {RecipientCount} recipients", toEmails.Count);
            return Result<bool>.Failure($"Failed to send bulk email: {ex.Message}");
        }
    }

    public async Task<Result<bool>> SendTeamInviteEmailAsync(string toEmail, string teamName, string inviterName, string inviteToken, CancellationToken cancellationToken = default)
    {
        var subject = $"You're invited to join {teamName} on SpeakHalo";
        var htmlContent = $@"
            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                <div style='background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); padding: 30px; text-align: center;'>
                    <h1 style='color: white; margin: 0;'>SpeakHalo</h1>
                </div>
                <div style='padding: 30px; background: #ffffff;'>
                    <h2 style='color: #333;'>You've been invited!</h2>
                    <p style='color: #555; font-size: 16px;'>
                        <strong>{System.Net.WebUtility.HtmlEncode(inviterName)}</strong> has invited you to join <strong>{System.Net.WebUtility.HtmlEncode(teamName)}</strong> on SpeakHalo.
                    </p>
                    <p style='color: #555; font-size: 14px;'>
                        As a team member, you'll be able to manage calls, appointments, and customers together.
                    </p>
                    <div style='text-align: center; margin: 30px 0;'>
                        <a href='{_frontendBaseUrl}/invite/{inviteToken}'
                           style='background: #667eea; color: white; padding: 14px 32px; text-decoration: none; border-radius: 8px; font-size: 16px; font-weight: bold;'>
                            Accept Invitation
                        </a>
                    </div>
                    <p style='color: #888; font-size: 12px;'>
                        This invitation expires in 7 days. If you didn't expect this invitation, you can safely ignore this email.
                    </p>
                </div>
                <div style='padding: 20px; text-align: center; background: #f8f9fa;'>
                    <p style='color: #888; font-size: 12px; margin: 0;'>SpeakHalo - AI-Powered Voice Call Management</p>
                </div>
            </div>";

        return await SendEmailAsync(toEmail, subject, htmlContent, null, cancellationToken);
    }
}
