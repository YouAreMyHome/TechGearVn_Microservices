using Product.Api.Mappings;
using Product.Api.Middleware;
using Product.Application.DependencyInjection;
using Product.Infrastructure.DependencyInjection;
using Product.Infrastructure.Persistence;
using Serilog;
using System.Reflection;
using Microsoft.EntityFrameworkCore;

// =====================================================
// 🌍 ENVIRONMENT VARIABLES SETUP
// =====================================================

DotNetEnv.Env.Load($".env.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")?.ToLower() ?? "development"}");

if (!File.Exists($".env.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")?.ToLower() ?? "development"}"))
{
    DotNetEnv.Env.Load(".env");
}

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();

// =====================================================
// 🚀 DEPENDENCY INJECTION SETUP
// =====================================================

// 1. Serilog Configuration
builder.Host.UseSerilog((context, config) =>
{
    var logLevel = Environment.GetEnvironmentVariable("SERILOG_MIN_LEVEL") ?? "Information";
    var filePath = Environment.GetEnvironmentVariable("SERILOG_FILE_PATH") ?? "logs/product-service-.txt";

    config.ReadFrom.Configuration(context.Configuration)
          .Enrich.FromLogContext()
          .Enrich.WithProperty("ApplicationName", "ProductService")
          .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName)
          .WriteTo.Console()
          .WriteTo.File(filePath, rollingInterval: RollingInterval.Day);

    config.MinimumLevel.Is(Enum.Parse<Serilog.Events.LogEventLevel>(logLevel));
});

// 2. Controllers và API Services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// 3. Swagger Configuration
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Product Service API",
        Version = "v1",
        Description = "Product Service API trong TechGear Microservices Architecture",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "TechGear Development Team",
            Email = "dev@techgear.vn"
        }
    });

    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
});

// 4. AutoMapper cho API Layer
builder.Services.AddAutoMapper(typeof(ApiMappingProfile));

// 5. Clean Architecture Layers Registration
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// 6. Health Checks - MICROSERVICES PATTERN: Comprehensive health monitoring
var healthChecksBuilder = builder.Services.AddHealthChecks();

// Database Health Check
healthChecksBuilder.AddDbContextCheck<ProductDbContext>(
    name: "ProductDatabase",
    failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy,
    tags: new[] { "database", "postgresql", "product", "critical" },
    customTestQuery: async (context, cancellationToken) =>
    {
        // Business-aware health check: Test actual business operations
        try
        {
            // Test database connection
            var canConnect = await context.Database.CanConnectAsync(cancellationToken);
            if (!canConnect) return false;

            // Test basic query (lightweight business operation)
            var productCount = await context.Products.CountAsync(cancellationToken);
            return true; // Database healthy nếu có thể query
        }
        catch
        {
            return false;
        }
    });

// Memory Health Check
healthChecksBuilder.AddCheck("Memory", () =>
{
    var memoryUsed = GC.GetTotalMemory(false);
    var memoryLimit = 500_000_000; // 500MB limit for container

    return memoryUsed < memoryLimit
        ? Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy($"Memory usage: {memoryUsed / 1024 / 1024}MB")
        : Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy($"Memory usage too high: {memoryUsed / 1024 / 1024}MB");
}, tags: new[] { "memory", "performance" });

// TODO: Thêm health checks cho external dependencies khi implement
// healthChecksBuilder.AddRabbitMQ(connectionString, tags: new[] { "messaging", "rabbitmq" });
// healthChecksBuilder.AddRedis(connectionString, tags: new[] { "cache", "redis" });

// 7. CORS cho development
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("DevelopmentCors", policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
    });
}

// =====================================================
// 🏗️ MIDDLEWARE PIPELINE SETUP
// =====================================================

var app = builder.Build();

// Log environment info
app.Logger.LogInformation("Environment: {Environment}", app.Environment.EnvironmentName);
app.Logger.LogInformation("Database Host: {DbHost}", Environment.GetEnvironmentVariable("DB_HOST"));
app.Logger.LogInformation("Database Name: {DbName}", Environment.GetEnvironmentVariable("DB_NAME"));

// Development tools
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Product Service API v1");
        options.RoutePrefix = string.Empty;
    });

    app.UseCors("DevelopmentCors");
}

// Middleware pipeline
app.UseSerilogRequestLogging();
app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
app.UseRouting();
app.UseAuthorization();

// Health checks endpoints - MICROSERVICES PATTERN: Multiple endpoints cho different purposes
app.UseHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";

        var response = new
        {
            status = report.Status.ToString(),
            timestamp = DateTime.UtcNow,
            service = new
            {
                name = "Product Service",
                version = "1.0.0",
                environment = app.Environment.EnvironmentName
            },
            totalDuration = Math.Round(report.TotalDuration.TotalMilliseconds, 2),
            checks = report.Entries.Select(x => new
            {
                name = x.Key,
                status = x.Value.Status.ToString(),
                description = x.Value.Description ?? "No description",
                duration = Math.Round(x.Value.Duration.TotalMilliseconds, 2),
                tags = x.Value.Tags,
                exception = x.Value.Exception?.Message
            }).ToList()
        };

        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response, new System.Text.Json.JsonSerializerOptions
        {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
            WriteIndented = true
        }));
    }
});

// Kubernetes Liveness probe (simple check)
app.UseHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("critical")
});

// Kubernetes Readiness probe (comprehensive check)
app.UseHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => true // All health checks
});

app.MapControllers();

// =====================================================
// 🗄️ DATABASE MIGRATION (Development Only)
// =====================================================

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<ProductDbContext>();

    try
    {
        var canConnect = await context.Database.CanConnectAsync();
        if (!canConnect)
        {
            app.Logger.LogError("Cannot connect to database!");
            throw new InvalidOperationException("Database connection failed");
        }

        app.Logger.LogInformation("Database connection successful");

        await context.Database.MigrateAsync();
        app.Logger.LogInformation("Database migration completed successfully");
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Database connection or migration failed: {Message}", ex.Message);
        throw;
    }
}

// =====================================================
// 🚀 APPLICATION STARTUP
// =====================================================

app.Logger.LogInformation("Product Service starting up...");
if (app.Environment.IsDevelopment())
{
    app.Logger.LogInformation("Swagger UI available at: {SwaggerUrl}",
        Environment.GetEnvironmentVariable("ASPNETCORE_URLS"));
}

await app.RunAsync();

// =====================================================
// 🧪 FOR INTEGRATION TESTING
// =====================================================

/// <summary>
/// Program class để support Integration Testing
/// WebApplicationFactory cần access class này để start test host
/// </summary>
public partial class Program { }