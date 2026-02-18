using System.IO.Compression;
using System.Text;
using ClosedXML.Excel;
using Microsoft.Extensions.Logging;
using SpeakHalo.Application.Interfaces;
using SpeakHalo.Domain.Entities;
using SpeakHalo.Domain.Interfaces.Repositories;

namespace SpeakHalo.Application.Services;

public class DataExportService : IDataExportService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DataExportService> _logger;

    public DataExportService(IUnitOfWork unitOfWork, ILogger<DataExportService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<byte[]> ExportCallsToCsvAsync(string userId, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default)
    {
        var calls = await GetCallsAsync(userId, startDate, endDate, cancellationToken);

        var csv = new StringBuilder();
        csv.AppendLine("Date,Caller Name,Caller Phone,Duration (seconds),Cost,Summary");

        foreach (var call in calls)
        {
            csv.AppendLine($"\"{call.CreatedAt:yyyy-MM-dd HH:mm:ss}\",\"{EscapeCsv(call.CallerName)}\",\"{call.CallerPhone}\",{call.DurationSeconds},{call.Cost:F2},\"{EscapeCsv(call.Summary)}\"");
        }

        return Encoding.UTF8.GetBytes(csv.ToString());
    }

    public async Task<byte[]> ExportCallsToExcelAsync(string userId, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default)
    {
        var calls = await GetCallsAsync(userId, startDate, endDate, cancellationToken);

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Calls");

        ws.Cell(1, 1).Value = "Date";
        ws.Cell(1, 2).Value = "Caller Name";
        ws.Cell(1, 3).Value = "Caller Phone";
        ws.Cell(1, 4).Value = "Duration (seconds)";
        ws.Cell(1, 5).Value = "Cost";
        ws.Cell(1, 6).Value = "Summary";
        StyleHeaderRow(ws, 6);

        var row = 2;
        foreach (var call in calls)
        {
            ws.Cell(row, 1).Value = call.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");
            ws.Cell(row, 2).Value = call.CallerName ?? "";
            ws.Cell(row, 3).Value = call.CallerPhone;
            ws.Cell(row, 4).Value = call.DurationSeconds;
            ws.Cell(row, 5).Value = call.Cost;
            ws.Cell(row, 5).Style.NumberFormat.Format = "$#,##0.00";
            ws.Cell(row, 6).Value = call.Summary ?? "";
            row++;
        }

        ws.Columns().AdjustToContents();
        return WorkbookToBytes(workbook);
    }

    public async Task<byte[]> ExportAppointmentsToCsvAsync(string userId, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default)
    {
        var appointments = await GetAppointmentsAsync(userId, startDate, endDate, cancellationToken);

        var csv = new StringBuilder();
        csv.AppendLine("Date,Customer ID,Status,Service Type,Notes");

        foreach (var apt in appointments)
        {
            csv.AppendLine($"\"{apt.ScheduledAt:yyyy-MM-dd HH:mm:ss}\",\"{apt.CustomerId}\",\"{apt.Status}\",\"{apt.ServiceType}\",\"{EscapeCsv(apt.Notes)}\"");
        }

        return Encoding.UTF8.GetBytes(csv.ToString());
    }

    public async Task<byte[]> ExportAppointmentsToExcelAsync(string userId, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default)
    {
        var appointments = await GetAppointmentsAsync(userId, startDate, endDate, cancellationToken);

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Appointments");

        ws.Cell(1, 1).Value = "Date";
        ws.Cell(1, 2).Value = "Customer ID";
        ws.Cell(1, 3).Value = "Status";
        ws.Cell(1, 4).Value = "Service Type";
        ws.Cell(1, 5).Value = "Notes";
        StyleHeaderRow(ws, 5);

        var row = 2;
        foreach (var apt in appointments)
        {
            ws.Cell(row, 1).Value = apt.ScheduledAt.ToString("yyyy-MM-dd HH:mm:ss");
            ws.Cell(row, 2).Value = apt.CustomerId ?? "";
            ws.Cell(row, 3).Value = apt.Status.ToString();
            ws.Cell(row, 4).Value = apt.ServiceType.ToString();
            ws.Cell(row, 5).Value = apt.Notes ?? "";
            row++;
        }

        ws.Columns().AdjustToContents();
        return WorkbookToBytes(workbook);
    }

    public async Task<byte[]> ExportCustomersToCsvAsync(string userId, CancellationToken cancellationToken = default)
    {
        var customers = await _unitOfWork.Repository<Customer>()
            .FindAsync(c => c.UserId == userId, cancellationToken);

        var csv = new StringBuilder();
        csv.AppendLine("Name,Phone,Email,Total Jobs,Created Date");

        foreach (var customer in customers)
        {
            csv.AppendLine($"\"{EscapeCsv(customer.Name)}\",\"{customer.Phone}\",\"{EscapeCsv(customer.Email)}\",{customer.JobCount},\"{customer.CreatedAt:yyyy-MM-dd}\"");
        }

        return Encoding.UTF8.GetBytes(csv.ToString());
    }

    public async Task<byte[]> ExportCustomersToExcelAsync(string userId, CancellationToken cancellationToken = default)
    {
        var customers = await _unitOfWork.Repository<Customer>()
            .FindAsync(c => c.UserId == userId, cancellationToken);

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Customers");

        ws.Cell(1, 1).Value = "Name";
        ws.Cell(1, 2).Value = "Phone";
        ws.Cell(1, 3).Value = "Email";
        ws.Cell(1, 4).Value = "Total Jobs";
        ws.Cell(1, 5).Value = "Created Date";
        StyleHeaderRow(ws, 5);

        var row = 2;
        foreach (var customer in customers)
        {
            ws.Cell(row, 1).Value = customer.Name ?? "";
            ws.Cell(row, 2).Value = customer.Phone;
            ws.Cell(row, 3).Value = customer.Email ?? "";
            ws.Cell(row, 4).Value = customer.JobCount;
            ws.Cell(row, 5).Value = customer.CreatedAt.ToString("yyyy-MM-dd");
            row++;
        }

        ws.Columns().AdjustToContents();
        return WorkbookToBytes(workbook);
    }

    public async Task<byte[]> ExportQuotesToCsvAsync(string userId, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default)
    {
        var quotes = await GetQuotesAsync(userId, startDate, endDate, cancellationToken);

        var csv = new StringBuilder();
        csv.AppendLine("Quote Number,Date,Customer ID,Total Amount,Status,Expires At");

        foreach (var quote in quotes)
        {
            csv.AppendLine($"\"{quote.QuoteNumber}\",\"{quote.CreatedAt:yyyy-MM-dd HH:mm:ss}\",\"{quote.CustomerId}\",{quote.Total:F2},\"{quote.Status}\",\"{quote.ExpiresAt?.ToString("yyyy-MM-dd") ?? "N/A"}\"");
        }

        return Encoding.UTF8.GetBytes(csv.ToString());
    }

    public async Task<byte[]> ExportQuotesToExcelAsync(string userId, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default)
    {
        var quotes = await GetQuotesAsync(userId, startDate, endDate, cancellationToken);

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Quotes");

        ws.Cell(1, 1).Value = "Quote Number";
        ws.Cell(1, 2).Value = "Date";
        ws.Cell(1, 3).Value = "Customer ID";
        ws.Cell(1, 4).Value = "Total Amount";
        ws.Cell(1, 5).Value = "Status";
        ws.Cell(1, 6).Value = "Expires At";
        StyleHeaderRow(ws, 6);

        var row = 2;
        foreach (var quote in quotes)
        {
            ws.Cell(row, 1).Value = quote.QuoteNumber ?? "";
            ws.Cell(row, 2).Value = quote.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");
            ws.Cell(row, 3).Value = quote.CustomerId ?? "";
            ws.Cell(row, 4).Value = quote.Total;
            ws.Cell(row, 4).Style.NumberFormat.Format = "$#,##0.00";
            ws.Cell(row, 5).Value = quote.Status ?? "";
            ws.Cell(row, 6).Value = quote.ExpiresAt?.ToString("yyyy-MM-dd") ?? "N/A";
            row++;
        }

        ws.Columns().AdjustToContents();
        return WorkbookToBytes(workbook);
    }

    public async Task<byte[]> ExportInvoicesToCsvAsync(string userId, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default)
    {
        var invoices = await GetInvoicesAsync(userId, startDate, endDate, cancellationToken);

        var csv = new StringBuilder();
        csv.AppendLine("Invoice Number,Date,Period Start,Period End,Subtotal,Tax,Total,Status,Paid At");

        foreach (var invoice in invoices)
        {
            csv.AppendLine($"\"{invoice.InvoiceNumber}\",\"{invoice.CreatedAt:yyyy-MM-dd}\",\"{invoice.PeriodStart:yyyy-MM-dd}\",\"{invoice.PeriodEnd:yyyy-MM-dd}\",{invoice.Subtotal:F2},{invoice.Tax:F2},{invoice.Total:F2},\"{invoice.Status}\",\"{invoice.PaidAt?.ToString("yyyy-MM-dd") ?? "N/A"}\"");
        }

        return Encoding.UTF8.GetBytes(csv.ToString());
    }

    public async Task<byte[]> ExportInvoicesToExcelAsync(string userId, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default)
    {
        var invoices = await GetInvoicesAsync(userId, startDate, endDate, cancellationToken);

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Invoices");

        ws.Cell(1, 1).Value = "Invoice Number";
        ws.Cell(1, 2).Value = "Date";
        ws.Cell(1, 3).Value = "Period Start";
        ws.Cell(1, 4).Value = "Period End";
        ws.Cell(1, 5).Value = "Subtotal";
        ws.Cell(1, 6).Value = "Tax";
        ws.Cell(1, 7).Value = "Total";
        ws.Cell(1, 8).Value = "Status";
        ws.Cell(1, 9).Value = "Paid At";
        StyleHeaderRow(ws, 9);

        var row = 2;
        foreach (var invoice in invoices)
        {
            ws.Cell(row, 1).Value = invoice.InvoiceNumber ?? "";
            ws.Cell(row, 2).Value = invoice.CreatedAt.ToString("yyyy-MM-dd");
            ws.Cell(row, 3).Value = invoice.PeriodStart.ToString("yyyy-MM-dd");
            ws.Cell(row, 4).Value = invoice.PeriodEnd.ToString("yyyy-MM-dd");
            ws.Cell(row, 5).Value = invoice.Subtotal;
            ws.Cell(row, 5).Style.NumberFormat.Format = "$#,##0.00";
            ws.Cell(row, 6).Value = invoice.Tax;
            ws.Cell(row, 6).Style.NumberFormat.Format = "$#,##0.00";
            ws.Cell(row, 7).Value = invoice.Total;
            ws.Cell(row, 7).Style.NumberFormat.Format = "$#,##0.00";
            ws.Cell(row, 8).Value = invoice.Status ?? "";
            ws.Cell(row, 9).Value = invoice.PaidAt?.ToString("yyyy-MM-dd") ?? "N/A";
            row++;
        }

        ws.Columns().AdjustToContents();
        return WorkbookToBytes(workbook);
    }

    public async Task<byte[]> ExportAllDataToZipAsync(string userId, CancellationToken cancellationToken = default)
    {
        using var memoryStream = new MemoryStream();
        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
        {
            var callsEntry = archive.CreateEntry("calls.csv");
            using (var entryStream = callsEntry.Open())
            {
                var callsData = await ExportCallsToCsvAsync(userId, null, null, cancellationToken);
                await entryStream.WriteAsync(callsData, cancellationToken);
            }

            var appointmentsEntry = archive.CreateEntry("appointments.csv");
            using (var entryStream = appointmentsEntry.Open())
            {
                var appointmentsData = await ExportAppointmentsToCsvAsync(userId, null, null, cancellationToken);
                await entryStream.WriteAsync(appointmentsData, cancellationToken);
            }

            var customersEntry = archive.CreateEntry("customers.csv");
            using (var entryStream = customersEntry.Open())
            {
                var customersData = await ExportCustomersToCsvAsync(userId, cancellationToken);
                await entryStream.WriteAsync(customersData, cancellationToken);
            }

            var quotesEntry = archive.CreateEntry("quotes.csv");
            using (var entryStream = quotesEntry.Open())
            {
                var quotesData = await ExportQuotesToCsvAsync(userId, null, null, cancellationToken);
                await entryStream.WriteAsync(quotesData, cancellationToken);
            }

            var invoicesEntry = archive.CreateEntry("invoices.csv");
            using (var entryStream = invoicesEntry.Open())
            {
                var invoicesData = await ExportInvoicesToCsvAsync(userId, null, null, cancellationToken);
                await entryStream.WriteAsync(invoicesData, cancellationToken);
            }
        }

        return memoryStream.ToArray();
    }

    private async Task<IEnumerable<Call>> GetCallsAsync(string userId, DateTime? startDate, DateTime? endDate, CancellationToken cancellationToken)
    {
        var query = await _unitOfWork.Repository<Call>()
            .FindAsync(c => c.UserId == userId, cancellationToken);

        if (startDate.HasValue)
            query = query.Where(c => c.CreatedAt >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(c => c.CreatedAt <= endDate.Value);

        return query.OrderByDescending(c => c.CreatedAt);
    }

    private async Task<IEnumerable<Appointment>> GetAppointmentsAsync(string userId, DateTime? startDate, DateTime? endDate, CancellationToken cancellationToken)
    {
        var query = await _unitOfWork.Repository<Appointment>()
            .FindAsync(a => a.UserId == userId, cancellationToken);

        if (startDate.HasValue)
            query = query.Where(a => a.ScheduledAt >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(a => a.ScheduledAt <= endDate.Value);

        return query.OrderByDescending(a => a.ScheduledAt);
    }

    private async Task<IEnumerable<Quote>> GetQuotesAsync(string userId, DateTime? startDate, DateTime? endDate, CancellationToken cancellationToken)
    {
        var query = await _unitOfWork.Repository<Quote>()
            .FindAsync(q => q.UserId == userId, cancellationToken);

        if (startDate.HasValue)
            query = query.Where(q => q.CreatedAt >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(q => q.CreatedAt <= endDate.Value);

        return query.OrderByDescending(q => q.CreatedAt);
    }

    private async Task<IEnumerable<Invoice>> GetInvoicesAsync(string userId, DateTime? startDate, DateTime? endDate, CancellationToken cancellationToken)
    {
        var query = await _unitOfWork.Repository<Invoice>()
            .FindAsync(i => i.UserId == userId, cancellationToken);

        if (startDate.HasValue)
            query = query.Where(i => i.CreatedAt >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(i => i.CreatedAt <= endDate.Value);

        return query.OrderByDescending(i => i.CreatedAt);
    }

    private static string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        return value.Replace("\"", "\"\"");
    }

    private static void StyleHeaderRow(IXLWorksheet ws, int columnCount)
    {
        var headerRange = ws.Range(1, 1, 1, columnCount);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#4472C4");
        headerRange.Style.Font.FontColor = XLColor.White;
        headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
    }

    private static byte[] WorkbookToBytes(XLWorkbook workbook)
    {
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
