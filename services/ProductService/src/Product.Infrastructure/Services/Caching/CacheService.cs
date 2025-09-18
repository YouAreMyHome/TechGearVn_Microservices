using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Product.Infrastructure.Services.Caching;

/// <summary>
/// Redis/Memory Caching service cho ProductService
/// Implement caching patterns: Cache-Aside, Write-Through
/// Support cache invalidation và warming strategies
/// </summary>
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class;
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class;
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
    Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default);
    Task ClearAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// In-Memory implementation của ICacheService cho development/testing
/// Production sẽ dùng Redis implementation
/// </summary>
public class MemoryCacheService : ICacheService
{
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<MemoryCacheService> _logger;
    private readonly HashSet<string> _cacheKeys = new();
    private readonly object _lockObject = new();

    public MemoryCacheService(
        IMemoryCache memoryCache,
        ILogger<MemoryCacheService> logger)
    {
        _memoryCache = memoryCache;
        _logger = logger;
    }

    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            if (_memoryCache.TryGetValue(key, out var cachedValue))
            {
                _logger.LogDebug("Cache hit for key: {CacheKey}", key);
                
                if (cachedValue is string json)
                {
                    return Task.FromResult(JsonSerializer.Deserialize<T>(json));
                }
                
                return Task.FromResult(cachedValue as T);
            }

            _logger.LogDebug("Cache miss for key: {CacheKey}", key);
            return Task.FromResult<T?>(null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cache value for key: {CacheKey}", key);
            return Task.FromResult<T?>(null);
        }
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var options = new MemoryCacheEntryOptions();
            
            if (expiration.HasValue)
            {
                options.AbsoluteExpirationRelativeToNow = expiration.Value;
            }
            else
            {
                // Default expiration
                options.SlidingExpiration = TimeSpan.FromMinutes(15);
                options.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
            }

            // Set cache với JSON serialization để đảm bảo consistency
            var json = JsonSerializer.Serialize(value, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            _memoryCache.Set(key, json, options);

            // Track cache keys cho pattern removal
            lock (_lockObject)
            {
                _cacheKeys.Add(key);
            }

            _logger.LogDebug("Cache set for key: {CacheKey}, expiration: {Expiration}", key, expiration);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting cache value for key: {CacheKey}", key);
            return Task.CompletedTask;
        }
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            _memoryCache.Remove(key);
            
            lock (_lockObject)
            {
                _cacheKeys.Remove(key);
            }

            _logger.LogDebug("Cache removed for key: {CacheKey}", key);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache value for key: {CacheKey}", key);
            return Task.CompletedTask;
        }
    }

    public Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        try
        {
            List<string> keysToRemove;
            
            lock (_lockObject)
            {
                keysToRemove = _cacheKeys
                    .Where(key => key.Contains(pattern))
                    .ToList();
            }

            foreach (var key in keysToRemove)
            {
                _memoryCache.Remove(key);
            }

            lock (_lockObject)
            {
                foreach (var key in keysToRemove)
                {
                    _cacheKeys.Remove(key);
                }
            }

            _logger.LogDebug("Cache pattern removal completed. Pattern: {Pattern}, Keys removed: {Count}", pattern, keysToRemove.Count);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache values by pattern: {Pattern}", pattern);
            return Task.CompletedTask;
        }
    }

    public Task ClearAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            List<string> allKeys;
            
            lock (_lockObject)
            {
                allKeys = _cacheKeys.ToList();
                _cacheKeys.Clear();
            }

            foreach (var key in allKeys)
            {
                _memoryCache.Remove(key);
            }

            _logger.LogInformation("Cache cleared. Removed {Count} keys", allKeys.Count);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing cache");
            return Task.CompletedTask;
        }
    }
}

/// <summary>
/// Cache key builder để standardize cache keys
/// Prevent cache key collisions và provide easy invalidation
/// </summary>
public static class CacheKeys
{
    private const string PRODUCT_PREFIX = "product";
    private const string CATEGORY_PREFIX = "category";
    private const string LIST_PREFIX = "list";

    // Product cache keys
    public static string Product(Guid id) => $"{PRODUCT_PREFIX}:id:{id}";
    public static string ProductBySku(string sku) => $"{PRODUCT_PREFIX}:sku:{sku}";
    public static string ProductsByCategory(Guid categoryId) => $"{PRODUCT_PREFIX}:category:{categoryId}";
    public static string ProductsLowStock() => $"{PRODUCT_PREFIX}:lowstock";

    // Category cache keys
    public static string Category(Guid id) => $"{CATEGORY_PREFIX}:id:{id}";
    public static string CategoryBySlug(string slug) => $"{CATEGORY_PREFIX}:slug:{slug}";
    public static string CategoryHierarchy(Guid? parentId = null) => 
        parentId.HasValue ? $"{CATEGORY_PREFIX}:hierarchy:{parentId}" : $"{CATEGORY_PREFIX}:hierarchy:root";
    public static string CategoryChildren(Guid parentId) => $"{CATEGORY_PREFIX}:children:{parentId}";

    // List cache keys
    public static string ProductList(int page, int pageSize, string sortBy, string filters) =>
        $"{LIST_PREFIX}:{PRODUCT_PREFIX}:page:{page}:size:{pageSize}:sort:{sortBy}:filter:{filters.GetHashCode()}";

    public static string CategoryList(int page, int pageSize) =>
        $"{LIST_PREFIX}:{CATEGORY_PREFIX}:page:{page}:size:{pageSize}";

    // Pattern keys cho bulk invalidation
    public static string ProductPattern => $"{PRODUCT_PREFIX}:*";
    public static string CategoryPattern => $"{CATEGORY_PREFIX}:*";
    public static string ListPattern => $"{LIST_PREFIX}:*";
}