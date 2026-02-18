using SpeakHalo.Application.DTOs;
using SpeakHalo.Shared.Helpers;
using SpeakHalo.Shared.Models;

namespace SpeakHalo.Application.Interfaces;

public interface IAppointmentService
{
    Task<Result<AppointmentResponse>> CreateAppointmentAsync(CreateAppointmentRequest request, CancellationToken cancellationToken = default);
    Task<Result<AppointmentResponse>> GetAppointmentByIdAsync(string appointmentId, CancellationToken cancellationToken = default);
    Task<Result<PaginatedList<AppointmentResponse>>> GetUserAppointmentsAsync(string userId, int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default);
    Task<Result<AppointmentResponse>> UpdateAppointmentAsync(string appointmentId, UpdateAppointmentRequest request, CancellationToken cancellationToken = default);
    Task<Result<bool>> CancelAppointmentAsync(string appointmentId, string reason, CancellationToken cancellationToken = default);
    Task<Result<List<AppointmentResponse>>> GetAppointmentsByDateRangeAsync(string userId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    Task<Result<bool>> SendReminderAsync(string appointmentId, CancellationToken cancellationToken = default);
}
