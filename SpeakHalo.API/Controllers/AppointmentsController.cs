using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpeakHalo.API.Filters;
using SpeakHalo.Application.DTOs;
using SpeakHalo.Application.Interfaces;
using SpeakHalo.Domain.Entities;
using SpeakHalo.Domain.Enums;
using SpeakHalo.Domain.Interfaces.Repositories;

namespace SpeakHalo.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
[RequireFeature(PlanFeature.Calendar)]
[RequireApiScope("appointments")]
public class AppointmentsController : ControllerBase
{
    private readonly IAppointmentService _appointmentService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICalendarService _calendarService;

    public AppointmentsController(IAppointmentService appointmentService, IUnitOfWork unitOfWork, ICalendarService calendarService)
    {
        _appointmentService = appointmentService;
        _unitOfWork = unitOfWork;
        _calendarService = calendarService;
    }

    private async Task<bool> UserOwnsAppointmentAsync(string appointmentId, string userId)
    {
        var items = await _unitOfWork.Repository<Appointment>().FindAsync(a => a.Id == appointmentId && a.UserId == userId);
        return items.Any();
    }

    [HttpPost]
    public async Task<IActionResult> CreateAppointment([FromBody] CreateAppointmentRequest request, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var securedRequest = request with { UserId = userId };
        var result = await _appointmentService.CreateAppointmentAsync(securedRequest, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(result);

        return CreatedAtAction(nameof(GetAppointmentById), new { id = result.Data!.Id }, result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetAppointmentById(string id, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        if (!await UserOwnsAppointmentAsync(id, userId))
            return NotFound();

        var result = await _appointmentService.GetAppointmentByIdAsync(id, cancellationToken);

        if (!result.IsSuccess)
            return NotFound(result);

        return Ok(result);
    }

    [HttpGet("user")]
    public async Task<IActionResult> GetUserAppointments([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null, CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _appointmentService.GetUserAppointmentsAsync(userId, pageNumber, pageSize, search, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateAppointment(string id, [FromBody] UpdateAppointmentRequest request, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        if (!await UserOwnsAppointmentAsync(id, userId))
            return NotFound();

        var result = await _appointmentService.UpdateAppointmentAsync(id, request, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> CancelAppointment(string id, [FromBody] CancelAppointmentRequest request, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        if (!await UserOwnsAppointmentAsync(id, userId))
            return NotFound();

        var result = await _appointmentService.CancelAppointmentAsync(id, request.Reason, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAppointment(string id, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        if (!await UserOwnsAppointmentAsync(id, userId))
            return NotFound();

        var appointment = await _unitOfWork.Repository<Appointment>().GetByIdAsync(id, cancellationToken);
        if (appointment == null)
            return NotFound();

        // Delete from Google Calendar if event exists
        if (!string.IsNullOrEmpty(appointment.GoogleCalendarEventId))
        {
            try
            {
                await _calendarService.DeleteEventAsync(userId, appointment.GoogleCalendarEventId, cancellationToken);
            }
            catch
            {
                // Don't fail the delete if calendar sync fails
            }
        }

        _unitOfWork.Repository<Appointment>().HardDelete(appointment);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(new { isSuccess = true, message = "Appointment deleted successfully." });
    }

    [HttpGet("user/date-range")]
    public async Task<IActionResult> GetAppointmentsByDateRange([FromQuery] DateTime startDate, [FromQuery] DateTime endDate, CancellationToken cancellationToken)
    {
        // Npgsql 6+ requires DateTime with DateTimeKind.Utc for timestamptz columns.
        // Query params parse as DateTimeKind.Unspecified, so convert to UTC.
        if (startDate.Kind != DateTimeKind.Utc)
            startDate = DateTime.SpecifyKind(startDate, DateTimeKind.Utc);
        if (endDate.Kind != DateTimeKind.Utc)
            endDate = DateTime.SpecifyKind(endDate, DateTimeKind.Utc);

        if (startDate > endDate)
            return BadRequest(new { error = "Start date must be before end date" });

        if ((endDate - startDate).TotalDays > 365)
            return BadRequest(new { error = "Date range must not exceed 1 year" });

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _appointmentService.GetAppointmentsByDateRangeAsync(userId, startDate, endDate, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPost("{id}/reminder")]
    public async Task<IActionResult> SendReminder(string id, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        if (!await UserOwnsAppointmentAsync(id, userId))
            return NotFound();

        var result = await _appointmentService.SendReminderAsync(id, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(result);

        return Ok(result);
    }
}

public record CancelAppointmentRequest(string Reason);
