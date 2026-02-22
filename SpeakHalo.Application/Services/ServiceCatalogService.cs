using Microsoft.Extensions.Logging;
using SpeakHalo.Application.DTOs;
using SpeakHalo.Application.Interfaces;
using SpeakHalo.Domain.Entities;
using SpeakHalo.Domain.Interfaces.Repositories;
using SpeakHalo.Shared.Constants;
using SpeakHalo.Shared.Models;

namespace SpeakHalo.Application.Services;

public class ServiceCatalogService : IServiceCatalogService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ServiceCatalogService> _logger;

    public ServiceCatalogService(IUnitOfWork unitOfWork, ILogger<ServiceCatalogService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<ServiceCatalogItemResponse>> CreateAsync(string userId, CreateServiceCatalogItemRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var item = new ServiceCatalogItem
            {
                UserId = userId,
                Name = request.Name,
                Description = request.Description,
                BasePrice = request.BasePrice,
                Category = request.Category,
                EstimatedDurationMinutes = request.EstimatedDurationMinutes,
                IsActive = true
            };

            await _unitOfWork.Repository<ServiceCatalogItem>().AddAsync(item, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<ServiceCatalogItemResponse>.Success(MapToResponse(item));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create service catalog item for user {UserId}", userId);
            return Result<ServiceCatalogItemResponse>.Failure($"Failed to create service: {ex.Message}");
        }
    }

    public async Task<Result<List<ServiceCatalogItemResponse>>> GetByUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var items = await _unitOfWork.Repository<ServiceCatalogItem>()
                .FindAsync(s => s.UserId == userId, cancellationToken);

            var responses = items.OrderBy(s => s.Name)
                .Select(MapToResponse)
                .ToList();

            return Result<List<ServiceCatalogItemResponse>>.Success(responses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get service catalog for user {UserId}", userId);
            return Result<List<ServiceCatalogItemResponse>>.Failure($"Failed to get services: {ex.Message}");
        }
    }

    public async Task<Result<ServiceCatalogItemResponse>> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            var item = await _unitOfWork.Repository<ServiceCatalogItem>().GetByIdAsync(id, cancellationToken);
            if (item == null)
                return Result<ServiceCatalogItemResponse>.Failure(ErrorMessages.NotFound("Service"));

            return Result<ServiceCatalogItemResponse>.Success(MapToResponse(item));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get service catalog item {Id}", id);
            return Result<ServiceCatalogItemResponse>.Failure($"Failed to get service: {ex.Message}");
        }
    }

    public async Task<Result<ServiceCatalogItemResponse>> UpdateAsync(string id, string userId, UpdateServiceCatalogItemRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var item = await _unitOfWork.Repository<ServiceCatalogItem>().GetByIdAsync(id, cancellationToken);
            if (item == null || item.UserId != userId)
                return Result<ServiceCatalogItemResponse>.Failure(ErrorMessages.NotFound("Service"));

            if (request.Name != null) item.Name = request.Name;
            if (request.Description != null) item.Description = request.Description;
            if (request.BasePrice.HasValue) item.BasePrice = request.BasePrice.Value;
            if (request.Category != null) item.Category = request.Category;
            if (request.EstimatedDurationMinutes.HasValue) item.EstimatedDurationMinutes = request.EstimatedDurationMinutes.Value;
            if (request.IsActive.HasValue) item.IsActive = request.IsActive.Value;

            item.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Repository<ServiceCatalogItem>().Update(item);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<ServiceCatalogItemResponse>.Success(MapToResponse(item));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update service catalog item {Id}", id);
            return Result<ServiceCatalogItemResponse>.Failure($"Failed to update service: {ex.Message}");
        }
    }

    public async Task<Result<bool>> DeleteAsync(string id, string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var item = await _unitOfWork.Repository<ServiceCatalogItem>().GetByIdAsync(id, cancellationToken);
            if (item == null || item.UserId != userId)
                return Result<bool>.Failure(ErrorMessages.NotFound("Service"));

            _unitOfWork.Repository<ServiceCatalogItem>().Remove(item);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete service catalog item {Id}", id);
            return Result<bool>.Failure($"Failed to delete service: {ex.Message}");
        }
    }

    private static ServiceCatalogItemResponse MapToResponse(ServiceCatalogItem item)
    {
        return new ServiceCatalogItemResponse
        {
            Id = item.Id,
            UserId = item.UserId,
            Name = item.Name,
            Description = item.Description,
            BasePrice = item.BasePrice,
            Category = item.Category,
            EstimatedDurationMinutes = item.EstimatedDurationMinutes,
            IsActive = item.IsActive,
            CreatedAt = item.CreatedAt,
            UpdatedAt = item.UpdatedAt
        };
    }
}
