namespace SpeakHalo.Application.Interfaces;

public interface IDataExportService
{
    Task<byte[]> ExportCallsToExcelAsync(string userId, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default);
    Task<byte[]> ExportCallsToCsvAsync(string userId, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default);
    Task<byte[]> ExportAppointmentsToExcelAsync(string userId, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default);
    Task<byte[]> ExportAppointmentsToCsvAsync(string userId, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default);
    Task<byte[]> ExportCustomersToExcelAsync(string userId, CancellationToken cancellationToken = default);
    Task<byte[]> ExportCustomersToCsvAsync(string userId, CancellationToken cancellationToken = default);
    Task<byte[]> ExportQuotesToExcelAsync(string userId, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default);
    Task<byte[]> ExportQuotesToCsvAsync(string userId, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default);
    Task<byte[]> ExportInvoicesToExcelAsync(string userId, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default);
    Task<byte[]> ExportInvoicesToCsvAsync(string userId, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default);
    Task<byte[]> ExportAllDataToZipAsync(string userId, CancellationToken cancellationToken = default);
}
