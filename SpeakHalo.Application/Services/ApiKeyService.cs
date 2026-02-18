using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using SpeakHalo.Application.DTOs;
using SpeakHalo.Application.Interfaces;
using SpeakHalo.Domain.Entities;
using SpeakHalo.Domain.Interfaces.Repositories;
using SpeakHalo.Shared.Constants;
using SpeakHalo.Shared.Models;

namespace SpeakHalo.Application.Services;

public class ApiKeyService : IApiKeyService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cacheService;
    private readonly ILogger<ApiKeyService> _logger;
    private const int ApiKeyLength = 32;
    private static readonly TimeSpan ApiKeyCacheDuration = TimeSpan.FromMinutes(10);

    public ApiKeyService(IUnitOfWork unitOfWork, ICacheService cacheService, ILogger<ApiKeyService> logger)
    {
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<Result<CreateApiKeyResponse>> CreateApiKeyAsync(string userId, CreateApiKeyRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
            if (user == null)
                return Result<CreateApiKeyResponse>.Failure(ErrorMessages.NotFound("User"));

            var (apiKey, keyHash, prefix) = GenerateApiKey();

            var apiKeyEntity = new ApiKey
            {
                UserId = userId,
                Name = request.Name,
                KeyHash = keyHash,
                Prefix = prefix,
                Scopes = request.Scopes,
                IsActive = true,
                ExpiresAt = request.ExpiresAt
            };

            await _unitOfWork.Repository<ApiKey>().AddAsync(apiKeyEntity, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var response = new CreateApiKeyResponse
            {
                Id = apiKeyEntity.Id,
                Name = apiKeyEntity.Name,
                ApiKey = apiKey,
                Prefix = prefix,
                Scopes = apiKeyEntity.Scopes,
                ExpiresAt = apiKeyEntity.ExpiresAt,
                CreatedAt = apiKeyEntity.CreatedAt
            };

            return Result<CreateApiKeyResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create API key for user {UserId}", userId);
            return Result<CreateApiKeyResponse>.Failure($"Failed to create API key: {ex.Message}");
        }
    }

    public async Task<Result<ApiKeyResponse>> GetApiKeyByIdAsync(string apiKeyId, CancellationToken cancellationToken = default)
    {
        try
        {
            var apiKey = await _unitOfWork.Repository<ApiKey>().GetByIdAsync(apiKeyId, cancellationToken);
            if (apiKey == null)
                return Result<ApiKeyResponse>.Failure(ErrorMessages.NotFound("API key"));

            return Result<ApiKeyResponse>.Success(MapToResponse(apiKey));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get API key {ApiKeyId}", apiKeyId);
            return Result<ApiKeyResponse>.Failure($"Failed to get API key: {ex.Message}");
        }
    }

    public async Task<Result<List<ApiKeyResponse>>> GetUserApiKeysAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var apiKeys = await _unitOfWork.Repository<ApiKey>().FindAsync(k => k.UserId == userId, cancellationToken);
            var responses = apiKeys.OrderByDescending(k => k.CreatedAt).Select(MapToResponse).ToList();

            return Result<List<ApiKeyResponse>>.Success(responses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get API keys for user {UserId}", userId);
            return Result<List<ApiKeyResponse>>.Failure($"Failed to get API keys: {ex.Message}");
        }
    }

    public async Task<Result<ApiKeyResponse>> UpdateApiKeyAsync(string apiKeyId, UpdateApiKeyRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var apiKey = await _unitOfWork.Repository<ApiKey>().GetByIdAsync(apiKeyId, cancellationToken);
            if (apiKey == null)
                return Result<ApiKeyResponse>.Failure(ErrorMessages.NotFound("API key"));

            if (request.Name != null) apiKey.Name = request.Name;
            if (request.IsActive.HasValue) apiKey.IsActive = request.IsActive.Value;
            if (request.Scopes != null) apiKey.Scopes = request.Scopes;

            _unitOfWork.Repository<ApiKey>().Update(apiKey);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Note: We can't invalidate the specific cache entry since we don't have the original API key value
            // The cache will expire naturally after the TTL (10 minutes)
            // For immediate invalidation, the user would need to use a new API key

            return Result<ApiKeyResponse>.Success(MapToResponse(apiKey));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update API key {ApiKeyId}", apiKeyId);
            return Result<ApiKeyResponse>.Failure($"Failed to update API key: {ex.Message}");
        }
    }

    public async Task<Result<bool>> DeleteApiKeyAsync(string apiKeyId, CancellationToken cancellationToken = default)
    {
        try
        {
            var apiKey = await _unitOfWork.Repository<ApiKey>().GetByIdAsync(apiKeyId, cancellationToken);
            if (apiKey == null)
                return Result<bool>.Failure(ErrorMessages.NotFound("API key"));

            _unitOfWork.Repository<ApiKey>().Remove(apiKey);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete API key {ApiKeyId}", apiKeyId);
            return Result<bool>.Failure($"Failed to delete API key: {ex.Message}");
        }
    }

    public async Task<Result<string>> ValidateApiKeyAsync(string apiKey, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(apiKey) || apiKey.Length < 10)
                return Result<string>.Failure("Invalid API key format");

            var prefix = apiKey[..8];

            var apiKeys = await _unitOfWork.Repository<ApiKey>()
                .FindAsync(k => k.Prefix == prefix && k.IsActive, cancellationToken);

            foreach (var key in apiKeys)
            {
                if (BCrypt.Net.BCrypt.Verify(apiKey, key.KeyHash))
                {
                    if (key.ExpiresAt.HasValue && key.ExpiresAt.Value < DateTime.UtcNow)
                        return Result<string>.Failure("API key has expired");

                    return Result<string>.Success(key.UserId);
                }
            }

            return Result<string>.Failure("Invalid API key");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate API key");
            return Result<string>.Failure($"Failed to validate API key: {ex.Message}");
        }
    }

    public async Task<Result<ApiKeyValidationResult>> ValidateApiKeyWithScopesAsync(string apiKey, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(apiKey) || apiKey.Length < 10)
                return Result<ApiKeyValidationResult>.Failure("Invalid API key format");

            // Use SHA256 hash of the API key for cache key (secure and consistent)
            var cacheKey = $"apikey:validation:{ComputeHash(apiKey)}";

            // Try to get from cache first
            var cachedResult = await _cacheService.GetAsync<ApiKeyValidationResult>(cacheKey, cancellationToken);
            if (cachedResult != null)
            {
                return Result<ApiKeyValidationResult>.Success(cachedResult);
            }

            var prefix = apiKey[..8];

            var apiKeys = await _unitOfWork.Repository<ApiKey>()
                .FindAsync(k => k.Prefix == prefix && k.IsActive, cancellationToken);

            foreach (var key in apiKeys)
            {
                if (BCrypt.Net.BCrypt.Verify(apiKey, key.KeyHash))
                {
                    if (key.ExpiresAt.HasValue && key.ExpiresAt.Value < DateTime.UtcNow)
                        return Result<ApiKeyValidationResult>.Failure("API key has expired");

                    // Parse scopes from comma-separated string
                    var scopes = string.IsNullOrWhiteSpace(key.Scopes)
                        ? new List<string>()
                        : key.Scopes.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                   .Select(s => s.Trim())
                                   .ToList();

                    var result = new ApiKeyValidationResult
                    {
                        UserId = key.UserId,
                        Scopes = scopes
                    };

                    // Cache the successful validation result
                    await _cacheService.SetAsync(cacheKey, result, ApiKeyCacheDuration, cancellationToken);

                    return Result<ApiKeyValidationResult>.Success(result);
                }
            }

            return Result<ApiKeyValidationResult>.Failure("Invalid API key");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate API key with scopes");
            return Result<ApiKeyValidationResult>.Failure($"Failed to validate API key: {ex.Message}");
        }
    }

    public async Task UpdateLastUsedAsync(string apiKeyId, string ipAddress, CancellationToken cancellationToken = default)
    {
        try
        {
            var apiKey = await _unitOfWork.Repository<ApiKey>().GetByIdAsync(apiKeyId, cancellationToken);
            if (apiKey != null)
            {
                apiKey.LastUsedAt = DateTime.UtcNow;
                apiKey.LastUsedIp = ipAddress;
                _unitOfWork.Repository<ApiKey>().Update(apiKey);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update API key last used for {ApiKeyId}", apiKeyId);
        }
    }

    private static (string apiKey, string keyHash, string prefix) GenerateApiKey()
    {
        var randomBytes = new byte[ApiKeyLength];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }

        var apiKey = $"sk_{Convert.ToBase64String(randomBytes).Replace("+", "").Replace("/", "").Replace("=", "")}";
        var prefix = apiKey[..8];
        var keyHash = BCrypt.Net.BCrypt.HashPassword(apiKey, workFactor: 12);

        return (apiKey, keyHash, prefix);
    }

    private static string ComputeHash(string input)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
        return Convert.ToBase64String(hashBytes);
    }

    private static ApiKeyResponse MapToResponse(ApiKey apiKey)
    {
        return new ApiKeyResponse
        {
            Id = apiKey.Id,
            Name = apiKey.Name,
            Prefix = apiKey.Prefix,
            Scopes = apiKey.Scopes,
            IsActive = apiKey.IsActive,
            ExpiresAt = apiKey.ExpiresAt,
            LastUsedAt = apiKey.LastUsedAt,
            LastUsedIp = apiKey.LastUsedIp,
            CreatedAt = apiKey.CreatedAt,
            UpdatedAt = apiKey.UpdatedAt
        };
    }
}
