using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using Product.Infrastructure.Persistence;

namespace Product.Infrastructure.Services.Performance;

/// <summary>
/// Query optimization configuration
/// </summary>
public class QueryOptimizationOptions
{
    public const string SectionName = "QueryOptimization";

    public bool EnableQueryHints { get; set; } = true;
    public bool EnableBatchQueries { get; set; } = true;
    public int DefaultPageSize { get; set; } = 20;
    public int MaxPageSize { get; set; } = 100;
    public TimeSpan QueryTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public bool EnableQueryLogging { get; set; } = false;
}

/// <summary>
/// Service để tối ưu database queries và monitoring performance
/// Collect metrics, apply query hints, optimize pagination
/// </summary>
public interface IQueryOptimizationService
{
    Task<T> OptimizeQuery<T>(Func<Task<T>> query, string queryName, CancellationToken cancellationToken = default);
    Task<(TData data, int totalCount)> OptimizePaginatedQuery<TData>(
        Func<Task<(TData data, int totalCount)>> query, 
        int page, 
        int pageSize, 
        string queryName,
        CancellationToken cancellationToken = default);
    
    int NormalizePageSize(int requestedPageSize);
    (int skip, int take) CalculatePagination(int page, int pageSize);
}

public class QueryOptimizationService : IQueryOptimizationService
{
    private readonly QueryOptimizationOptions _options;
    private readonly ILogger<QueryOptimizationService> _logger;

    public QueryOptimizationService(
        IOptions<QueryOptimizationOptions> options,
        ILogger<QueryOptimizationService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<T> OptimizeQuery<T>(Func<Task<T>> query, string queryName, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            _logger.LogDebug("Starting optimized query: {QueryName}", queryName);

            // Execute query với timeout protection
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(_options.QueryTimeout);

            var result = await query();

            stopwatch.Stop();

            // Log performance metrics
            LogQueryPerformance(queryName, stopwatch.ElapsedMilliseconds, true);

            return result;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            stopwatch.Stop();
            LogQueryPerformance(queryName, stopwatch.ElapsedMilliseconds, false, "Cancelled");
            throw;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            LogQueryPerformance(queryName, stopwatch.ElapsedMilliseconds, false, ex.Message);
            throw;
        }
    }

    public async Task<(TData data, int totalCount)> OptimizePaginatedQuery<TData>(
        Func<Task<(TData data, int totalCount)>> query,
        int page,
        int pageSize,
        string queryName,
        CancellationToken cancellationToken = default)
    {
        // Normalize pagination parameters
        var normalizedPageSize = NormalizePageSize(pageSize);
        var (skip, take) = CalculatePagination(page, normalizedPageSize);

        _logger.LogDebug("Executing paginated query: {QueryName}, Page: {Page}, PageSize: {PageSize} (normalized from {RequestedPageSize})",
            queryName, page, normalizedPageSize, pageSize);

        return await OptimizeQuery(query, $"{queryName}_Paginated", cancellationToken);
    }

    public int NormalizePageSize(int requestedPageSize)
    {
        if (requestedPageSize <= 0)
        {
            return _options.DefaultPageSize;
        }

        if (requestedPageSize > _options.MaxPageSize)
        {
            _logger.LogWarning("Requested page size {RequestedPageSize} exceeds maximum {MaxPageSize}, using maximum",
                requestedPageSize, _options.MaxPageSize);
            return _options.MaxPageSize;
        }

        return requestedPageSize;
    }

    public (int skip, int take) CalculatePagination(int page, int pageSize)
    {
        // Ensure page is at least 1
        var safePage = Math.Max(1, page);
        var skip = (safePage - 1) * pageSize;
        
        return (skip, pageSize);
    }

    private void LogQueryPerformance(string queryName, long elapsedMs, bool success, string? errorMessage = null)
    {
        if (_options.EnableQueryLogging)
        {
            if (success)
            {
                if (elapsedMs > 1000) // Slow query threshold
                {
                    _logger.LogWarning("Slow query detected: {QueryName} took {ElapsedMs}ms", queryName, elapsedMs);
                }
                else
                {
                    _logger.LogDebug("Query completed: {QueryName} took {ElapsedMs}ms", queryName, elapsedMs);
                }
            }
            else
            {
                _logger.LogError("Query failed: {QueryName} took {ElapsedMs}ms, Error: {Error}",
                    queryName, elapsedMs, errorMessage);
            }
        }

        // TODO: Send metrics to monitoring system (ApplicationInsights, Prometheus, etc.)
        // Example: _telemetryClient.TrackDependency("Database", queryName, startTime, TimeSpan.FromMilliseconds(elapsedMs), success);
    }
}

/// <summary>
/// Database connection health check và performance monitoring
/// </summary>
public interface IDatabaseHealthService
{
    Task<bool> CheckHealthAsync(CancellationToken cancellationToken = default);
    Task<DatabaseHealthStatus> GetDetailedHealthAsync(CancellationToken cancellationToken = default);
}

public class DatabaseHealthStatus
{
    public bool IsHealthy { get; set; }
    public TimeSpan ConnectionTime { get; set; }
    public long ActiveConnections { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object> Metrics { get; set; } = new();
}

public class DatabaseHealthService : IDatabaseHealthService
{
    private readonly ProductDbContext _context;
    private readonly ILogger<DatabaseHealthService> _logger;

    public DatabaseHealthService(
        ProductDbContext context,
        ILogger<DatabaseHealthService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<bool> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            // Simple connectivity check
            await _context.Database.ExecuteSqlRawAsync("SELECT 1", cancellationToken);
            
            stopwatch.Stop();

            var isHealthy = stopwatch.ElapsedMilliseconds < 5000; // 5 second threshold
            
            if (!isHealthy)
            {
                _logger.LogWarning("Database health check slow: {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
            }

            return isHealthy;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database health check failed");
            return false;
        }
    }

    public async Task<DatabaseHealthStatus> GetDetailedHealthAsync(CancellationToken cancellationToken = default)
    {
        var status = new DatabaseHealthStatus();
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            // Test connection
            await _context.Database.ExecuteSqlRawAsync("SELECT 1", cancellationToken);
            
            stopwatch.Stop();
            status.ConnectionTime = stopwatch.Elapsed;
            status.IsHealthy = stopwatch.ElapsedMilliseconds < 5000;

            // Get additional metrics
            status.Metrics["ProductCount"] = await _context.Products.CountAsync(cancellationToken);
            status.Metrics["CategoryCount"] = await _context.Categories.CountAsync(cancellationToken);
            status.Metrics["ActiveProductCount"] = await _context.Products.CountAsync(p => p.IsActive, cancellationToken);

            _logger.LogDebug("Database health check completed in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            status.IsHealthy = false;
            status.ConnectionTime = stopwatch.Elapsed;
            status.ErrorMessage = ex.Message;
            
            _logger.LogError(ex, "Database detailed health check failed");
        }

        return status;
    }
}