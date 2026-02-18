using SpeakHalo.Application.DTOs;
using SpeakHalo.Shared.Models;

namespace SpeakHalo.Application.Interfaces;

public interface IAuditLogService
{
    Task<Result<AuditLogResponse>> CreateAuditLogAsync(CreateAuditLogRequest request, CancellationToken cancellationToken = default);
    Task<Result<AuditLogResponse>> GetAuditLogByIdAsync(string auditLogId, CancellationToken cancellationToken = default);
    Task<Result<PaginatedList<AuditLogResponse>>> GetAuditLogsAsync(AuditLogFilterRequest filter, CancellationToken cancellationToken = default);
    Task<Result<PaginatedList<AuditLogResponse>>> GetUserAuditLogsAsync(string userId, int pageNumber = 1, int pageSize = 20, CancellationToken cancellationToken = default);
    Task<Result<PaginatedList<AuditLogResponse>>> GetEntityAuditLogsAsync(string entityType, string entityId, int pageNumber = 1, int pageSize = 20, CancellationToken cancellationToken = default);
    Task LogActionAsync(string? userId, string action, string entityType, string? entityId = null, object? oldValues = null, object? newValues = null, string? ipAddress = null, string? userAgent = null);
}
