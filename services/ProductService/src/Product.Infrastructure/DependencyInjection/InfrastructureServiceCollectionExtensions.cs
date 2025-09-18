using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Product.Domain.Repositories;
using Product.Infrastructure.Persistence;
using Product.Infrastructure.Persistence.Repositories;
using Product.Infrastructure.Services.Caching;
using Product.Infrastructure.Services.Decorators;
using Product.Infrastructure.Services.Performance;

namespace Product.Infrastructure.DependencyInjection;

/// <summary>
/// Infrastructure Layer Dependency Injection Extensions
/// Đăng ký tất cả Infrastructure services vào DI container
/// Tuân thủ Clean Architecture: Infrastructure implement Domain interfaces
/// </summary>
public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Đăng ký Database services
        services.AddDatabaseServices(configuration);

        // Đăng ký Caching services
        services.AddCachingServices(configuration);

        // Đăng ký Performance services
        services.AddPerformanceServices(configuration);

        // Đăng ký Repository implementations với caching decorators
        services.AddRepositories();

        // Đăng ký Background services (Outbox Pattern)
        services.AddBackgroundServices();

        return services;
    }

    /// <summary>
    /// Đăng ký EF Core DbContext với PostgreSQL
    /// Connection string được build từ environment variables
    /// </summary>
    private static IServiceCollection AddDatabaseServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Build connection string từ environment variables
        var connectionString = BuildConnectionStringFromEnv();

        services.AddDbContext<ProductDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                // Migration assembly
                npgsqlOptions.MigrationsAssembly(typeof(ProductDbContext).Assembly.FullName);

                // Retry policy cho production stability
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: int.Parse(Environment.GetEnvironmentVariable("DB_MAX_RETRY_COUNT") ?? "3"),
                    maxRetryDelay: TimeSpan.FromSeconds(int.Parse(Environment.GetEnvironmentVariable("DB_RETRY_DELAY_SECONDS") ?? "5")),
                    errorCodesToAdd: null);

                // Command timeout
                npgsqlOptions.CommandTimeout(int.Parse(Environment.GetEnvironmentVariable("DB_COMMAND_TIMEOUT") ?? "30"));
            });

            // Development settings
            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
            {
                // Enable sensitive data logging nếu env var cho phép
                if (bool.Parse(Environment.GetEnvironmentVariable("DB_ENABLE_SENSITIVE_DATA_LOGGING") ?? "false"))
                {
                    options.EnableSensitiveDataLogging();
                }

                // Include error detail nếu env var cho phép
                if (bool.Parse(Environment.GetEnvironmentVariable("DB_INCLUDE_ERROR_DETAIL") ?? "false"))
                {
                    options.EnableDetailedErrors();
                }
            }
        });

        return services;
    }

    /// <summary>
    /// Đăng ký Repository implementations với caching decorators
    /// Domain interfaces → Infrastructure implementations với cache layer
    /// </summary>
    private static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        // Base repositories
        services.AddScoped<ProductRepository>();
        services.AddScoped<CategoryRepository>();

        // Cached decorators
        services.AddScoped<IProductRepository>(provider =>
        {
            var baseRepo = provider.GetRequiredService<ProductRepository>();
            var cacheService = provider.GetRequiredService<ICacheService>();
            var logger = provider.GetRequiredService<ILogger<CachedProductRepository>>();
            return new CachedProductRepository(baseRepo, cacheService, logger);
        });

        services.AddScoped<ICategoryRepository>(provider =>
        {
            var baseRepo = provider.GetRequiredService<CategoryRepository>();
            var cacheService = provider.GetRequiredService<ICacheService>();
            var logger = provider.GetRequiredService<ILogger<CachedCategoryRepository>>();
            return new CachedCategoryRepository(baseRepo, cacheService, logger);
        });

        return services;
    }

    /// <summary>
    /// Đăng ký Caching services
    /// Development: MemoryCache, Production: Redis (future)
    /// </summary>
    private static IServiceCollection AddCachingServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Add memory cache
        services.AddMemoryCache();

        // Register cache service (MemoryCache cho development)
        services.AddSingleton<ICacheService, MemoryCacheService>();

        return services;
    }

    /// <summary>
    /// Đăng ký Performance optimization services
    /// Query optimization, health checks, monitoring
    /// </summary>
    private static IServiceCollection AddPerformanceServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configure query optimization với default options
        services.Configure<QueryOptimizationOptions>(options =>
        {
            // Use default values từ class
        });

        // Register performance services
        services.AddScoped<IQueryOptimizationService, QueryOptimizationService>();
        services.AddScoped<IDatabaseHealthService, DatabaseHealthService>();

        return services;
    }

    /// <summary>
    /// Đăng ký Background services cho Outbox Pattern
    /// </summary>
    private static IServiceCollection AddBackgroundServices(this IServiceCollection services)
    {
        // TODO: Add OutboxMessageProcessor background service
        // services.AddHostedService<OutboxMessageProcessor>();

        return services;
    }

    /// <summary>
    /// Build PostgreSQL connection string từ environment variables
    /// Centralized logic để đảm bảo consistency
    /// </summary>
    private static string BuildConnectionStringFromEnv()
    {
        var host = Environment.GetEnvironmentVariable("DB_HOST")
            ?? throw new InvalidOperationException("DB_HOST environment variable is required");

        var port = Environment.GetEnvironmentVariable("DB_PORT") ?? "5432";

        var database = Environment.GetEnvironmentVariable("DB_NAME")
            ?? throw new InvalidOperationException("DB_NAME environment variable is required");

        var username = Environment.GetEnvironmentVariable("DB_USERNAME")
            ?? throw new InvalidOperationException("DB_USERNAME environment variable is required");

        var password = Environment.GetEnvironmentVariable("DB_PASSWORD")
            ?? throw new InvalidOperationException("DB_PASSWORD environment variable is required");

        // Optional settings với default values
        var pooling = Environment.GetEnvironmentVariable("DB_POOLING") ?? "true";
        var minPoolSize = Environment.GetEnvironmentVariable("DB_MIN_POOL_SIZE") ?? "1";
        var maxPoolSize = Environment.GetEnvironmentVariable("DB_MAX_POOL_SIZE") ?? "20";
        var connectionTimeout = Environment.GetEnvironmentVariable("DB_CONNECTION_TIMEOUT") ?? "30";
        var sslMode = Environment.GetEnvironmentVariable("DB_SSL_MODE") ?? "Prefer";
        var trustServerCertificate = Environment.GetEnvironmentVariable("DB_TRUST_SERVER_CERTIFICATE") ?? "true";

        return $"Host={host};Port={port};Database={database};Username={username};Password={password};" +
               $"Pooling={pooling};Minimum Pool Size={minPoolSize};Maximum Pool Size={maxPoolSize};" +
               $"Timeout={connectionTimeout};SSL Mode={sslMode};Trust Server Certificate={trustServerCertificate}";
    }
}