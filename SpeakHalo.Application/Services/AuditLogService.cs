using System.Text.Json;
using Microsoft.Extensions.Logging;
using SpeakHalo.Application.DTOs;
using SpeakHalo.Application.Interfaces;
using SpeakHalo.Domain.Entities;
using SpeakHalo.Domain.Interfaces.Repositories;
using SpeakHalo.Shared.Constants;
using SpeakHalo.Shared.Extensions;
using SpeakHalo.Shared.Models;

namespace SpeakHalo.Application.Services;

public class AuditLogService : IAuditLogService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AuditLogService> _logger;

    public AuditLogService(IUnitOfWork unitOfWork, ILogger<AuditLogService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<AuditLogResponse>> CreateAuditLogAsync(CreateAuditLogRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var auditLog = new AuditLog
            {
                UserId = request.UserId,
                Action = request.Action,
                EntityType = request.EntityType,
                EntityId = request.EntityId,
                OldValues = request.OldValues,
                NewValues = request.NewValues,
                IpAddress = request.IpAddress,
                UserAgent = request.UserAgent,
                PerformedAt = DateTime.UtcNow
            };

            await _unitOfWork.Repository<AuditLog>().AddAsync(auditLog, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<AuditLogResponse>.Success(MapToResponse(auditLog));
        }
        catch (Exception ex)
        {
            return Result<AuditLogResponse>.Failure($"Failed to create audit log: {ex.Message}");
        }
    }

    public async Task<Result<AuditLogResponse>> GetAuditLogByIdAsync(string auditLogId, CancellationToken cancellationToken = default)
    {
        try
        {
            var auditLog = await _unitOfWork.Repository<AuditLog>().GetByIdAsync(auditLogId, cancellationToken);
            if (auditLog == null)
                return Result<AuditLogResponse>.Failure(ErrorMessages.NotFound("Audit log"));

            return Result<AuditLogResponse>.Success(MapToResponse(auditLog));
        }
        catch (Exception ex)
        {
            return Result<AuditLogResponse>.Failure($"Failed to get audit log: {ex.Message}");
        }
    }

    public async Task<Result<PaginatedList<AuditLogResponse>>> GetAuditLogsAsync(AuditLogFilterRequest filter, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = await _unitOfWork.Repository<AuditLog>().GetAllAsync(cancellationToken);
            var filteredQuery = query.AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.UserId))
                filteredQuery = filteredQuery.Where(a => a.UserId == filter.UserId);

            if (!string.IsNullOrWhiteSpace(filter.Action))
                filteredQuery = filteredQuery.Where(a => a.Action == filter.Action);

            if (!string.IsNullOrWhiteSpace(filter.EntityType))
                filteredQuery = filteredQuery.Where(a => a.EntityType == filter.EntityType);

            if (!string.IsNullOrWhiteSpace(filter.EntityId))
                filteredQuery = filteredQuery.Where(a => a.EntityId == filter.EntityId);

            if (filter.StartDate.HasValue)
                filteredQuery = filteredQuery.Where(a => a.PerformedAt >= filter.StartDate.Value);

            if (filter.EndDate.HasValue)
                filteredQuery = filteredQuery.Where(a => a.PerformedAt <= filter.EndDate.Value);

            var orderedQuery = filteredQuery.OrderByDescending(a => a.PerformedAt);

            var paginatedLogs = await orderedQuery.ToPaginatedListAsync(filter.PageNumber, filter.PageSize, cancellationToken);

            var responses = paginatedLogs.Items.Select(MapToResponse).ToList();

            var result = new PaginatedList<AuditLogResponse>(
                responses,
                paginatedLogs.TotalCount,
                paginatedLogs.CurrentPage,
                paginatedLogs.PageSize
            );

            return Result<PaginatedList<AuditLogResponse>>.Success(result);
        }
        catch (Exception ex)
        {
            return Result<PaginatedList<AuditLogResponse>>.Failure($"Failed to get audit logs: {ex.Message}");
        }
    }

    public async Task<Result<PaginatedList<AuditLogResponse>>> GetUserAuditLogsAsync(string userId, int pageNumber = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        try
        {
            var auditLogs = await _unitOfWork.Repository<AuditLog>().FindAsync(a => a.UserId == userId, cancellationToken);
            var orderedLogs = auditLogs.OrderByDescending(a => a.PerformedAt);

            var paginatedLogs = await orderedLogs.AsQueryable().ToPaginatedListAsync(pageNumber, pageSize, cancellationToken);

            var responses = paginatedLogs.Items.Select(MapToResponse).ToList();

            var result = new PaginatedList<AuditLogResponse>(
                responses,
                paginatedLogs.TotalCount,
                paginatedLogs.CurrentPage,
                paginatedLogs.PageSize
            );

            return Result<PaginatedList<AuditLogResponse>>.Success(result);
        }
        catch (Exception ex)
        {
            return Result<PaginatedList<AuditLogResponse>>.Failure($"Failed to get user audit logs: {ex.Message}");
        }
    }

    public async Task<Result<PaginatedList<AuditLogResponse>>> GetEntityAuditLogsAsync(string entityType, string entityId, int pageNumber = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        try
        {
            var auditLogs = await _unitOfWork.Repository<AuditLog>()
                .FindAsync(a => a.EntityType == entityType && a.EntityId == entityId, cancellationToken);
            var orderedLogs = auditLogs.OrderByDescending(a => a.PerformedAt);

            var paginatedLogs = await orderedLogs.AsQueryable().ToPaginatedListAsync(pageNumber, pageSize, cancellationToken);

            var responses = paginatedLogs.Items.Select(MapToResponse).ToList();

            var result = new PaginatedList<AuditLogResponse>(
                responses,
                paginatedLogs.TotalCount,
                paginatedLogs.CurrentPage,
                paginatedLogs.PageSize
            );

            return Result<PaginatedList<AuditLogResponse>>.Success(result);
        }
        catch (Exception ex)
        {
            return Result<PaginatedList<AuditLogResponse>>.Failure($"Failed to get entity audit logs: {ex.Message}");
        }
    }

    public async Task LogActionAsync(string? userId, string action, string entityType, string? entityId = null, object? oldValues = null, object? newValues = null, string? ipAddress = null, string? userAgent = null)
    {
        try
        {
            var auditLog = new AuditLog
            {
                UserId = userId,
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                OldValues = oldValues != null ? JsonSerializer.Serialize(oldValues) : null,
                NewValues = newValues != null ? JsonSerializer.Serialize(newValues) : null,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                PerformedAt = DateTime.UtcNow
            };

            await _unitOfWork.Repository<AuditLog>().AddAsync(auditLog);
            await _unitOfWork.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write audit log for action {Action} on {EntityType}", action, entityType);
        }
    }

    private static AuditLogResponse MapToResponse(AuditLog auditLog)
    {
        return new AuditLogResponse
        {
            Id = auditLog.Id,
            UserId = auditLog.UserId,
            Action = auditLog.Action,
            EntityType = auditLog.EntityType,
            EntityId = auditLog.EntityId,
            OldValues = auditLog.OldValues,
            NewValues = auditLog.NewValues,
            IpAddress = auditLog.IpAddress,
            UserAgent = auditLog.UserAgent,
            PerformedAt = auditLog.PerformedAt,
            CreatedAt = auditLog.CreatedAt
        };
    }
}
