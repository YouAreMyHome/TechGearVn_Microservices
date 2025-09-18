using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Product.Domain.Repositories;
using Product.Infrastructure.Persistence;
using Product.Infrastructure.Persistence.Repositories;

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

        // Đăng ký Repository implementations
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
    /// Đăng ký Repository implementations
    /// Domain interfaces → Infrastructure implementations
    /// </summary>
    private static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();

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