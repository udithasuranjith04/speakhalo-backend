using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using SpeakHalo.Application.Interfaces;

namespace SpeakHalo.Infrastructure.Services;

public class CacheService : ICacheService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<CacheService> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    public CacheService(IDistributedCache cache, ILogger<CacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var cached = await _cache.GetStringAsync(key, cancellationToken);
            if (string.IsNullOrEmpty(cached))
                return null;

            return JsonSerializer.Deserialize<T>(cached, JsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cache for key {Key}", key);
            return null;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var json = JsonSerializer.Serialize(value, JsonOptions);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration ?? TimeSpan.FromMinutes(15)
            };

            await _cache.SetStringAsync(key, json, options, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting cache for key {Key}", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            await _cache.RemoveAsync(key, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache for key {Key}", key);
        }
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var cached = await _cache.GetStringAsync(key, cancellationToken);
            return !string.IsNullOrEmpty(cached);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking cache existence for key {Key}", key);
            return false;
        }
    }

    public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var cached = await GetAsync<T>(key, cancellationToken);
            if (cached != null)
                return cached;

            var value = await factory();
            if (value != null)
            {
                await SetAsync(key, value, expiration, cancellationToken);
            }

            return value!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetOrSetAsync for key {Key}", key);
            return (await factory())!;
        }
    }
}
