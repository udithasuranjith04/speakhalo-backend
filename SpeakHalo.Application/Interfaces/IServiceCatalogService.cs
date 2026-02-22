using SpeakHalo.Application.DTOs;
using SpeakHalo.Shared.Models;

namespace SpeakHalo.Application.Interfaces;

public interface IServiceCatalogService
{
    Task<Result<ServiceCatalogItemResponse>> CreateAsync(string userId, CreateServiceCatalogItemRequest request, CancellationToken cancellationToken = default);
    Task<Result<List<ServiceCatalogItemResponse>>> GetByUserAsync(string userId, CancellationToken cancellationToken = default);
    Task<Result<ServiceCatalogItemResponse>> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<Result<ServiceCatalogItemResponse>> UpdateAsync(string id, string userId, UpdateServiceCatalogItemRequest request, CancellationToken cancellationToken = default);
    Task<Result<bool>> DeleteAsync(string id, string userId, CancellationToken cancellationToken = default);
}
