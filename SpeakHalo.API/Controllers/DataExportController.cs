using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpeakHalo.Application.Interfaces;

namespace SpeakHalo.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/data-export")]
[Authorize]
public class DataExportController : ControllerBase
{
    private readonly IDataExportService _dataExportService;

    public DataExportController(IDataExportService dataExportService)
    {
        _dataExportService = dataExportService;
    }

    [HttpGet("calls")]
    public async Task<IActionResult> ExportCalls(
        [FromQuery] string format = "csv",
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        NormalizeDates(ref startDate, ref endDate);

        var bytes = format.ToLower() == "excel"
            ? await _dataExportService.ExportCallsToExcelAsync(userId, startDate, endDate, cancellationToken)
            : await _dataExportService.ExportCallsToCsvAsync(userId, startDate, endDate, cancellationToken);

        return File(bytes, GetContentType(format), $"calls.{GetExtension(format)}");
    }

    [HttpGet("appointments")]
    public async Task<IActionResult> ExportAppointments(
        [FromQuery] string format = "csv",
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        NormalizeDates(ref startDate, ref endDate);

        var bytes = format.ToLower() == "excel"
            ? await _dataExportService.ExportAppointmentsToExcelAsync(userId, startDate, endDate, cancellationToken)
            : await _dataExportService.ExportAppointmentsToCsvAsync(userId, startDate, endDate, cancellationToken);

        return File(bytes, GetContentType(format), $"appointments.{GetExtension(format)}");
    }

    [HttpGet("customers")]
    public async Task<IActionResult> ExportCustomers(
        [FromQuery] string format = "csv",
        CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var bytes = format.ToLower() == "excel"
            ? await _dataExportService.ExportCustomersToExcelAsync(userId, cancellationToken)
            : await _dataExportService.ExportCustomersToCsvAsync(userId, cancellationToken);

        return File(bytes, GetContentType(format), $"customers.{GetExtension(format)}");
    }

    [HttpGet("quotes")]
    public async Task<IActionResult> ExportQuotes(
        [FromQuery] string format = "csv",
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        NormalizeDates(ref startDate, ref endDate);

        var bytes = format.ToLower() == "excel"
            ? await _dataExportService.ExportQuotesToExcelAsync(userId, startDate, endDate, cancellationToken)
            : await _dataExportService.ExportQuotesToCsvAsync(userId, startDate, endDate, cancellationToken);

        return File(bytes, GetContentType(format), $"quotes.{GetExtension(format)}");
    }

    [HttpGet("invoices")]
    public async Task<IActionResult> ExportInvoices(
        [FromQuery] string format = "csv",
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        NormalizeDates(ref startDate, ref endDate);

        var bytes = format.ToLower() == "excel"
            ? await _dataExportService.ExportInvoicesToExcelAsync(userId, startDate, endDate, cancellationToken)
            : await _dataExportService.ExportInvoicesToCsvAsync(userId, startDate, endDate, cancellationToken);

        return File(bytes, GetContentType(format), $"invoices.{GetExtension(format)}");
    }

    [HttpGet("all")]
    public async Task<IActionResult> ExportAll(CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var bytes = await _dataExportService.ExportAllDataToZipAsync(userId, cancellationToken);

        return File(bytes, "application/zip", "speakhalo-data-export.zip");
    }

    private static void NormalizeDates(ref DateTime? startDate, ref DateTime? endDate)
    {
        if (startDate.HasValue && startDate.Value.Kind != DateTimeKind.Utc)
            startDate = DateTime.SpecifyKind(startDate.Value, DateTimeKind.Utc);
        if (endDate.HasValue && endDate.Value.Kind != DateTimeKind.Utc)
            endDate = DateTime.SpecifyKind(endDate.Value, DateTimeKind.Utc);
    }

    private static string GetContentType(string format) =>
        format.ToLower() == "excel"
            ? "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
            : "text/csv";

    private static string GetExtension(string format) =>
        format.ToLower() == "excel" ? "xlsx" : "csv";
}
