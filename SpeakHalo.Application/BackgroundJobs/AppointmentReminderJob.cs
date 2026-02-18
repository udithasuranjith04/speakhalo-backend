using System.Net;
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
    private readonly ILogger<AppointmentReminderJob> _logger;

    public AppointmentReminderJob(
        IUnitOfWork unitOfWork,
        INotificationService notificationService,
        IEmailService emailService,
        ILogger<AppointmentReminderJob> logger)
    {
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task SendUpcomingRemindersAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Checking for upcoming appointments");

        var now = DateTime.UtcNow;
        var reminderWindow = now.AddHours(24);

        var upcomingAppointments = await _unitOfWork.Appointments
            .FindAsync(a => a.ScheduledAt >= now && a.ScheduledAt <= reminderWindow && a.Status == AppointmentStatus.Confirmed, cancellationToken);

        foreach (var appointment in upcomingAppointments)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var alreadyReminded = await _unitOfWork.Repository<AppointmentReminder>()
                    .AnyAsync(r => r.AppointmentId == appointment.Id && r.SentAt > now.AddHours(-24), cancellationToken);

                if (alreadyReminded)
                    continue;

                await _notificationService.SendAppointmentReminderAsync(appointment.Id);

                var user = await _unitOfWork.Users.GetByIdAsync(appointment.UserId, cancellationToken);
                if (user != null)
                {
                    var customer = !string.IsNullOrEmpty(appointment.CustomerId)
                        ? await _unitOfWork.Customers.GetByIdAsync(appointment.CustomerId, cancellationToken)
                        : null;
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

                var reminder = new AppointmentReminder
                {
                    AppointmentId = appointment.Id,
                    SentAt = DateTime.UtcNow,
                    ReminderType = "24h"
                };

                await _unitOfWork.Repository<AppointmentReminder>().AddAsync(reminder, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Sent reminder for appointment {AppointmentId}", appointment.Id);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Appointment reminder job cancelled");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending reminder for appointment {AppointmentId}", appointment.Id);
            }
        }

        _logger.LogInformation("Completed appointment reminder check");
    }
}
