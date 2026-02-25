using Microsoft.Extensions.Logging;
using SpeakHalo.Application.DTOs;
using SpeakHalo.Application.Interfaces;
using SpeakHalo.Domain.Entities;
using SpeakHalo.Domain.Enums;
using SpeakHalo.Domain.Interfaces.Repositories;
using SpeakHalo.Shared.Constants;
using SpeakHalo.Shared.Extensions;
using SpeakHalo.Shared.Models;

namespace SpeakHalo.Application.Services;

public class AppointmentService : IAppointmentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailService _emailService;
    private readonly ILogger<AppointmentService> _logger;

    public AppointmentService(IUnitOfWork unitOfWork, IEmailService emailService, ILogger<AppointmentService> logger)
    {
        _unitOfWork = unitOfWork;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<Result<AppointmentResponse>> CreateAppointmentAsync(CreateAppointmentRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var appointment = new Appointment
            {
                UserId = request.UserId,
                CustomerId = request.CustomerId,
                ScheduledAt = request.ScheduledAt,
                DurationMinutes = request.DurationMinutes,
                ServiceType = Enum.TryParse<ServiceType>(request.ServiceType, true, out var st) ? st : default,
                Location = request.Location,
                Notes = request.Notes,
                Status = AppointmentStatus.Scheduled
            };

            await _unitOfWork.Appointments.AddAsync(appointment, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<AppointmentResponse>.Success(MapToResponse(appointment));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create appointment for user {UserId}", request.UserId);
            return Result<AppointmentResponse>.Failure($"Failed to create appointment: {ex.Message}");
        }
    }

    public async Task<Result<AppointmentResponse>> GetAppointmentByIdAsync(string appointmentId, CancellationToken cancellationToken = default)
    {
        try
        {
            var appointment = await _unitOfWork.Appointments.GetByIdAsync(appointmentId, cancellationToken);

            if (appointment == null)
                return Result<AppointmentResponse>.Failure(ErrorMessages.NotFound("Appointment"));

            return Result<AppointmentResponse>.Success(MapToResponse(appointment));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get appointment {AppointmentId}", appointmentId);
            return Result<AppointmentResponse>.Failure($"Failed to get appointment: {ex.Message}");
        }
    }

    public async Task<Result<PaginatedList<AppointmentResponse>>> GetUserAppointmentsAsync(string userId, int pageNumber = 1, int pageSize = 10, string? search = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var appointments = await _unitOfWork.Appointments.FindAsync(a => a.UserId == userId, cancellationToken);
            var filtered = appointments.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                filtered = filtered.Where(a =>
                    (a.Customer?.Name != null && a.Customer.Name.Contains(s, StringComparison.OrdinalIgnoreCase)) ||
                    (a.Customer?.Phone != null && a.Customer.Phone.Contains(s, StringComparison.OrdinalIgnoreCase)) ||
                    a.ServiceType.ToString().Contains(s, StringComparison.OrdinalIgnoreCase) ||
                    (a.Notes != null && a.Notes.Contains(s, StringComparison.OrdinalIgnoreCase))
                );
            }

            var ordered = filtered.OrderByDescending(a => a.ScheduledAt).ToList();

            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;

            var totalCount = ordered.Count;
            var items = ordered.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

            var responses = items.Select(MapToResponse).ToList();

            var result = new PaginatedList<AppointmentResponse>(
                responses,
                totalCount,
                pageNumber,
                pageSize
            );

            return Result<PaginatedList<AppointmentResponse>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get appointments for user {UserId}", userId);
            return Result<PaginatedList<AppointmentResponse>>.Failure($"Failed to get appointments: {ex.Message}");
        }
    }

    public async Task<Result<AppointmentResponse>> UpdateAppointmentAsync(string appointmentId, UpdateAppointmentRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var appointment = await _unitOfWork.Appointments.GetByIdAsync(appointmentId, cancellationToken);

            if (appointment == null)
                return Result<AppointmentResponse>.Failure(ErrorMessages.NotFound("Appointment"));

            if (request.ScheduledAt.HasValue)
                appointment.ScheduledAt = request.ScheduledAt.Value;

            if (request.DurationMinutes.HasValue)
                appointment.DurationMinutes = request.DurationMinutes.Value;

            if (request.Status.HasValue)
            {
                appointment.Status = request.Status.Value;
                if (request.Status.Value == AppointmentStatus.Completed)
                    appointment.CompletedAt = DateTime.UtcNow;
            }

            if (request.ServiceType != null && Enum.TryParse<ServiceType>(request.ServiceType, true, out var serviceType))
                appointment.ServiceType = serviceType;

            if (request.Location != null)
                appointment.Location = request.Location;

            if (request.Notes != null)
                appointment.Notes = request.Notes;

            if (request.CompletionNotes != null)
                appointment.CompletionNotes = request.CompletionNotes;

            _unitOfWork.Appointments.Update(appointment);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<AppointmentResponse>.Success(MapToResponse(appointment));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update appointment {AppointmentId}", appointmentId);
            return Result<AppointmentResponse>.Failure($"Failed to update appointment: {ex.Message}");
        }
    }

    public async Task<Result<bool>> CancelAppointmentAsync(string appointmentId, string reason, CancellationToken cancellationToken = default)
    {
        try
        {
            var appointment = await _unitOfWork.Appointments.GetByIdAsync(appointmentId, cancellationToken);

            if (appointment == null)
                return Result<bool>.Failure(ErrorMessages.NotFound("Appointment"));

            appointment.Status = AppointmentStatus.Cancelled;
            appointment.CancelledAt = DateTime.UtcNow;
            appointment.CancellationReason = reason;

            _unitOfWork.Appointments.Update(appointment);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cancel appointment {AppointmentId}", appointmentId);
            return Result<bool>.Failure($"Failed to cancel appointment: {ex.Message}");
        }
    }

    public async Task<Result<List<AppointmentResponse>>> GetAppointmentsByDateRangeAsync(string userId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        try
        {
            var appointments = await _unitOfWork.Appointments.FindAsync(
                a => a.UserId == userId && a.ScheduledAt >= startDate && a.ScheduledAt <= endDate,
                cancellationToken
            );

            var responses = appointments
                .OrderBy(a => a.ScheduledAt)
                .Select(MapToResponse)
                .ToList();

            return Result<List<AppointmentResponse>>.Success(responses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get appointments by date range for user {UserId}", userId);
            return Result<List<AppointmentResponse>>.Failure($"Failed to get appointments: {ex.Message}");
        }
    }

    public async Task<Result<bool>> SendReminderAsync(string appointmentId, CancellationToken cancellationToken = default)
    {
        try
        {
            var appointment = await _unitOfWork.Appointments.GetByIdAsync(appointmentId, cancellationToken);

            if (appointment == null)
                return Result<bool>.Failure(ErrorMessages.NotFound("Appointment"));

            var customer = appointment.Customer;
            if (customer?.Email == null)
                return Result<bool>.Failure("Customer email not found");

            var locationHtml = !string.IsNullOrEmpty(appointment.Location)
                ? $@"<tr><td style=""padding:8px 12px;color:#6b7280;"">Location</td><td style=""padding:8px 12px;font-weight:600;"">{System.Net.WebUtility.HtmlEncode(appointment.Location)}</td></tr>"
                : "";

            var subject = $"Appointment Reminder - {appointment.ScheduledAt:MMMM dd} at {appointment.ScheduledAt:h:mm tt}";
            var htmlContent = $@"<!DOCTYPE html>
<html><head><meta charset=""utf-8""><meta name=""viewport"" content=""width=device-width,initial-scale=1""></head>
<body style=""margin:0;padding:0;background-color:#f3f4f6;font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,sans-serif;"">
<div style=""max-width:600px;margin:0 auto;padding:20px;"">
  <div style=""background:linear-gradient(135deg,#667eea 0%,#764ba2 100%);border-radius:12px 12px 0 0;padding:30px;text-align:center;"">
    <h1 style=""color:white;margin:0;font-size:24px;"">Appointment Reminder</h1>
  </div>
  <div style=""background:white;padding:30px;border-radius:0 0 12px 12px;"">
    <p style=""color:#374151;font-size:16px;"">Hi {System.Net.WebUtility.HtmlEncode(customer.Name)},</p>
    <p style=""color:#374151;font-size:16px;"">This is a reminder for your upcoming appointment:</p>
    <table style=""width:100%;border-collapse:collapse;margin:20px 0;background:#f9fafb;border-radius:8px;"">
      <tr><td style=""padding:8px 12px;color:#6b7280;"">Date</td><td style=""padding:8px 12px;font-weight:600;"">{appointment.ScheduledAt:dddd, MMMM dd, yyyy}</td></tr>
      <tr><td style=""padding:8px 12px;color:#6b7280;"">Time</td><td style=""padding:8px 12px;font-weight:600;"">{appointment.ScheduledAt:h:mm tt}</td></tr>
      <tr><td style=""padding:8px 12px;color:#6b7280;"">Duration</td><td style=""padding:8px 12px;font-weight:600;"">{appointment.DurationMinutes} minutes</td></tr>
      {locationHtml}
    </table>
    <p style=""color:#6b7280;font-size:14px;"">If you need to reschedule or cancel, please contact us.</p>
  </div>
  <p style=""text-align:center;color:#9ca3af;font-size:12px;margin-top:20px;"">Powered by SpeakHalo</p>
</div>
</body></html>";

            await _emailService.SendEmailAsync(customer.Email, subject, htmlContent, cancellationToken: cancellationToken);

            appointment.ReminderSent = true;
            _unitOfWork.Appointments.Update(appointment);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send reminder for appointment {AppointmentId}", appointmentId);
            return Result<bool>.Failure($"Failed to send reminder: {ex.Message}");
        }
    }

    private static AppointmentResponse MapToResponse(Appointment appointment)
    {
        return new AppointmentResponse
        {
            Id = appointment.Id,
            UserId = appointment.UserId,
            CustomerId = appointment.CustomerId,
            ScheduledAt = appointment.ScheduledAt,
            DurationMinutes = appointment.DurationMinutes,
            Status = appointment.Status,
            ServiceType = appointment.ServiceType == default ? null : appointment.ServiceType.ToString(),
            Location = appointment.Location,
            Notes = appointment.Notes,
            CompletedAt = appointment.CompletedAt,
            CompletionNotes = appointment.CompletionNotes,
            CancelledAt = appointment.CancelledAt,
            CancellationReason = appointment.CancellationReason,
            ReminderSent = appointment.ReminderSent,
            CreatedAt = appointment.CreatedAt
        };
    }
}
