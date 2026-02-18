using SpeakHalo.Application.DTOs;
using SpeakHalo.Shared.Models;

namespace SpeakHalo.Application.Interfaces;

public interface IApiKeyService
{
    Task<Result<CreateApiKeyResponse>> CreateApiKeyAsync(string userId, CreateApiKeyRequest request, CancellationToken cancellationToken = default);
    Task<Result<ApiKeyResponse>> GetApiKeyByIdAsync(string apiKeyId, CancellationToken cancellationToken = default);
    Task<Result<List<ApiKeyResponse>>> GetUserApiKeysAsync(string userId, CancellationToken cancellationToken = default);
    Task<Result<ApiKeyResponse>> UpdateApiKeyAsync(string apiKeyId, UpdateApiKeyRequest request, CancellationToken cancellationToken = default);
    Task<Result<bool>> DeleteApiKeyAsync(string apiKeyId, CancellationToken cancellationToken = default);
    Task<Result<string>> ValidateApiKeyAsync(string apiKey, CancellationToken cancellationToken = default);
    Task<Result<ApiKeyValidationResult>> ValidateApiKeyWithScopesAsync(string apiKey, CancellationToken cancellationToken = default);
    Task UpdateLastUsedAsync(string apiKeyId, string ipAddress, CancellationToken cancellationToken = default);
}

public record ApiKeyValidationResult
{
    public required string UserId { get; init; }
    public required List<string> Scopes { get; init; }
}
