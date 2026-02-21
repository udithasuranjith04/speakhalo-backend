using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SpeakHalo.Application.Interfaces;
using SpeakHalo.Domain.Entities;
using SpeakHalo.Domain.Enums;
using SpeakHalo.Domain.Interfaces.Repositories;

namespace SpeakHalo.Application.BackgroundJobs;

public class AppointmentReminderJob
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationService _notificationService;
    private readonly IEmailService _emailService;
    private readonly ITwilioService _twilioService;
    private readonly ILogger<AppointmentReminderJob> _logger;

    public AppointmentReminderJob(
        IUnitOfWork unitOfWork,
        INotificationService notificationService,
        IEmailService emailService,
        ITwilioService twilioService,
        ILogger<AppointmentReminderJob> logger)
    {
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
        _emailService = emailService;
        _twilioService = twilioService;
        _logger = logger;
    }

    public async Task SendUpcomingRemindersAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Checking for upcoming appointments");

        var now = DateTime.UtcNow;

        // 24h reminders: appointments 23–25h away
        await Send24hRemindersAsync(now, cancellationToken);

        // 2h reminders: appointments 1.5–2.5h away
        await Send2hRemindersAsync(now, cancellationToken);

        _logger.LogInformation("Completed appointment reminder check");
    }

    private async Task Send24hRemindersAsync(DateTime now, CancellationToken cancellationToken)
    {
        var windowStart = now.AddHours(23);
        var windowEnd = now.AddHours(25);

        var appointments = await _unitOfWork.Appointments
            .FindAsync(a => a.ScheduledAt >= windowStart
                && a.ScheduledAt <= windowEnd
                && (a.Status == AppointmentStatus.Scheduled || a.Status == AppointmentStatus.Confirmed)
                && !a.ReminderSent24h, cancellationToken);

        foreach (var appointment in appointments)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var user = await _unitOfWork.Users.GetByIdAsync(appointment.UserId, cancellationToken);
                if (user == null) continue;

                var customer = !string.IsNullOrEmpty(appointment.CustomerId)
                    ? await _unitOfWork.Customers.GetByIdAsync(appointment.CustomerId, cancellationToken)
                    : null;

                // Send in-app notification to business owner
                await _notificationService.SendAppointmentReminderAsync(appointment.Id);

                // Send email to business owner
                await SendOwnerEmailAsync(user, customer, appointment);

                // Send customer reminders (SMS/WhatsApp)
                await SendCustomerRemindersAsync(user, customer, appointment, "24h", cancellationToken);

                // Mark 24h reminder as sent
                appointment.ReminderSent24h = true;
                appointment.Reminder24hSentAt = DateTime.UtcNow;
                _unitOfWork.Appointments.Update(appointment);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Sent 24h reminder for appointment {AppointmentId}", appointment.Id);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Appointment reminder job cancelled");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending 24h reminder for appointment {AppointmentId}", appointment.Id);
            }
        }
    }

    private async Task Send2hRemindersAsync(DateTime now, CancellationToken cancellationToken)
    {
        var windowStart = now.AddHours(1.5);
        var windowEnd = now.AddHours(2.5);

        var appointments = await _unitOfWork.Appointments
            .FindAsync(a => a.ScheduledAt >= windowStart
                && a.ScheduledAt <= windowEnd
                && (a.Status == AppointmentStatus.Scheduled || a.Status == AppointmentStatus.Confirmed)
                && !a.ReminderSent2h, cancellationToken);

        foreach (var appointment in appointments)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var user = await _unitOfWork.Users.GetByIdAsync(appointment.UserId, cancellationToken);
                if (user == null) continue;

                var customer = !string.IsNullOrEmpty(appointment.CustomerId)
                    ? await _unitOfWork.Customers.GetByIdAsync(appointment.CustomerId, cancellationToken)
                    : null;

                // Send customer reminders (SMS/WhatsApp)
                await SendCustomerRemindersAsync(user, customer, appointment, "2h", cancellationToken);

                // Mark 2h reminder as sent
                appointment.ReminderSent2h = true;
                appointment.Reminder2hSentAt = DateTime.UtcNow;
                _unitOfWork.Appointments.Update(appointment);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Sent 2h reminder for appointment {AppointmentId}", appointment.Id);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Appointment reminder job cancelled");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending 2h reminder for appointment {AppointmentId}", appointment.Id);
            }
        }
    }

    private async Task SendCustomerRemindersAsync(
        User user, Customer? customer, Appointment appointment,
        string reminderWindow, CancellationToken cancellationToken)
    {
        if (customer == null || string.IsNullOrEmpty(customer.Phone))
        {
            _logger.LogWarning("No customer phone for appointment {AppointmentId}, skipping customer reminders", appointment.Id);
            return;
        }

        // Load user's reminder settings
        var settings = await GetReminderSettingsAsync(user.Id, cancellationToken);
        var isEnabled = reminderWindow == "24h" ? settings.Reminder24hEnabled : settings.Reminder2hEnabled;
        if (!isEnabled)
            return;

        // Get user's business phone number for sending
        var primaryNumber = await _unitOfWork.PhoneNumbers.GetPrimaryNumberAsync(user.Id, cancellationToken);
        if (primaryNumber == null)
        {
            _logger.LogWarning("No primary phone number for user {UserId}, skipping customer reminders", user.Id);
            return;
        }

        var customerName = customer.Name ?? "there";
        var businessName = user.Name;
        var serviceType = appointment.ServiceType.ToString();
        var time = appointment.ScheduledAt.ToString("h:mm tt");

        // Build message
        string message;
        var customMessage = reminderWindow == "24h" ? settings.CustomMessage24h : settings.CustomMessage2h;
        if (!string.IsNullOrWhiteSpace(customMessage))
        {
            message = customMessage
                .Replace("{CustomerName}", customerName)
                .Replace("{BusinessName}", businessName)
                .Replace("{ServiceType}", serviceType)
                .Replace("{Time}", time);
        }
        else if (reminderWindow == "24h")
        {
            message = $"Hi {customerName}, this is a reminder that your {serviceType} appointment with {businessName} is tomorrow at {time}. Reply STOP to opt out.";
        }
        else
        {
            message = $"Hi {customerName}, just a reminder that your appointment with {businessName} is in 2 hours at {time}. See you soon!";
        }

        // Send SMS
        if (settings.ReminderChannels.Contains("sms") && primaryNumber.SmsEnabled)
        {
            await SendReminderViaSmsAsync(primaryNumber.Number, customer.Phone, message, appointment.Id, reminderWindow, cancellationToken);
        }

        // Send WhatsApp
        if (settings.ReminderChannels.Contains("whatsapp") && primaryNumber.WhatsAppEnabled)
        {
            await SendReminderViaWhatsAppAsync(primaryNumber.Number, customer.Phone, message, appointment.Id, reminderWindow, cancellationToken);
        }
    }

    private async Task SendReminderViaSmsAsync(
        string fromNumber, string toNumber, string message,
        string appointmentId, string reminderWindow, CancellationToken cancellationToken)
    {
        var reminderRecord = new AppointmentReminder
        {
            AppointmentId = appointmentId,
            ScheduledFor = DateTime.UtcNow,
            ReminderType = $"{reminderWindow}_sms",
            IsSent = false
        };

        try
        {
            var result = await _twilioService.SendSmsAsync(toNumber, fromNumber, message, cancellationToken);
            reminderRecord.IsSent = result.IsSuccess;
            reminderRecord.SentAt = DateTime.UtcNow;
            if (!result.IsSuccess)
                reminderRecord.ErrorMessage = result.Errors.FirstOrDefault() ?? "SMS send failed";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send SMS reminder for appointment {AppointmentId}", appointmentId);
            reminderRecord.ErrorMessage = ex.Message;
        }

        await _unitOfWork.Repository<AppointmentReminder>().AddAsync(reminderRecord, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task SendReminderViaWhatsAppAsync(
        string fromNumber, string toNumber, string message,
        string appointmentId, string reminderWindow, CancellationToken cancellationToken)
    {
        var reminderRecord = new AppointmentReminder
        {
            AppointmentId = appointmentId,
            ScheduledFor = DateTime.UtcNow,
            ReminderType = $"{reminderWindow}_whatsapp",
            IsSent = false
        };

        try
        {
            var result = await _twilioService.SendWhatsAppMessageAsync(toNumber, fromNumber, message, cancellationToken);
            reminderRecord.IsSent = result.IsSuccess;
            reminderRecord.SentAt = DateTime.UtcNow;
            if (!result.IsSuccess)
                reminderRecord.ErrorMessage = result.Errors.FirstOrDefault() ?? "WhatsApp send failed";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send WhatsApp reminder for appointment {AppointmentId}", appointmentId);
            reminderRecord.ErrorMessage = ex.Message;
        }

        await _unitOfWork.Repository<AppointmentReminder>().AddAsync(reminderRecord, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<CustomerReminderPreferences> GetReminderSettingsAsync(string userId, CancellationToken cancellationToken)
    {
        var profiles = await _unitOfWork.Repository<UserProfile>()
            .FindAsync(p => p.UserId == userId, cancellationToken);
        var profile = profiles.FirstOrDefault();

        if (profile?.NotificationSettings != null)
        {
            try
            {
                var parsed = JsonSerializer.Deserialize<CustomerReminderPreferences>(profile.NotificationSettings,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (parsed != null) return parsed;
            }
            catch
            {
                // Fall through to defaults
            }
        }

        return CustomerReminderPreferences.Default;
    }

    private async Task SendOwnerEmailAsync(User user, Customer? customer, Appointment appointment)
    {
        var customerName = WebUtility.HtmlEncode(customer?.Name ?? "your customer");
        var location = !string.IsNullOrEmpty(appointment.Location)
            ? $@"<tr><td style=""padding:8px 12px;color:#6b7280;"">Location</td><td style=""padding:8px 12px;font-weight:600;"">{WebUtility.HtmlEncode(appointment.Location)}</td></tr>"
            : "";

        var subject = $"Appointment Reminder - {appointment.ScheduledAt:MMMM dd} at {appointment.ScheduledAt:h:mm tt}";
        var body = $@"<!DOCTYPE html>
<html><head><meta charset=""utf-8""><meta name=""viewport"" content=""width=device-width,initial-scale=1""></head>
<body style=""margin:0;padding:0;background-color:#f3f4f6;font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,sans-serif;"">
<div style=""max-width:600px;margin:0 auto;padding:20px;"">
  <div style=""background:linear-gradient(135deg,#667eea 0%,#764ba2 100%);border-radius:12px 12px 0 0;padding:30px;text-align:center;"">
    <h1 style=""color:white;margin:0;font-size:24px;"">Appointment Reminder</h1>
  </div>
  <div style=""background:white;padding:30px;border-radius:0 0 12px 12px;"">
    <p style=""color:#374151;font-size:16px;"">Hi {WebUtility.HtmlEncode(user.Name)},</p>
    <p style=""color:#374151;font-size:16px;"">You have an upcoming appointment with <strong>{customerName}</strong>:</p>
    <table style=""width:100%;border-collapse:collapse;margin:20px 0;background:#f9fafb;border-radius:8px;"">
      <tr><td style=""padding:8px 12px;color:#6b7280;"">Date</td><td style=""padding:8px 12px;font-weight:600;"">{appointment.ScheduledAt:dddd, MMMM dd, yyyy}</td></tr>
      <tr><td style=""padding:8px 12px;color:#6b7280;"">Time</td><td style=""padding:8px 12px;font-weight:600;"">{appointment.ScheduledAt:h:mm tt}</td></tr>
      <tr><td style=""padding:8px 12px;color:#6b7280;"">Duration</td><td style=""padding:8px 12px;font-weight:600;"">{appointment.DurationMinutes} minutes</td></tr>
      {location}
    </table>
    <p style=""color:#6b7280;font-size:14px;margin-top:20px;"">This is an automated reminder from SpeakHalo.</p>
  </div>
  <p style=""text-align:center;color:#9ca3af;font-size:12px;margin-top:20px;"">&copy; {DateTime.UtcNow.Year} SpeakHalo. All rights reserved.</p>
</div>
</body></html>";
        await _emailService.SendEmailAsync(user.Email, subject, body);
    }
}

public class CustomerReminderPreferences
{
    public bool Reminder24hEnabled { get; set; } = true;
    public bool Reminder2hEnabled { get; set; } = true;
    public List<string> ReminderChannels { get; set; } = new() { "sms" };
    public string? CustomMessage24h { get; set; }
    public string? CustomMessage2h { get; set; }

    public static CustomerReminderPreferences Default => new();
}
