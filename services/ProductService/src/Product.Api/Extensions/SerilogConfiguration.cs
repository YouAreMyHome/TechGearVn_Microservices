using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;

namespace Product.Api.Extensions;

/// <summary>
/// Extension methods để cấu hình Serilog với structured logging
/// Production-ready configuration với multiple sinks và enrichment
/// </summary>
public static class SerilogConfiguration
{
    /// <summary>
    /// Configure Serilog với structured logging cho Production
    /// - File logging với rolling policy
    /// - Console logging cho development
    /// - Structured JSON format
    /// - Request enrichment với traceId, userId, etc.
    /// </summary>
    public static IServiceCollection ConfigureSerilog(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        // Base configuration
        var loggerConfig = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Information)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Service", "ProductService")
            .Enrich.WithProperty("Version", GetAssemblyVersion());

        // Development logging
        if (environment.IsDevelopment())
        {
            loggerConfig
                .MinimumLevel.Debug()
                .WriteTo.Console(
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
                .WriteTo.Debug();
        }

        // Production/Staging logging
        if (environment.IsProduction() || environment.IsStaging())
        {
            var logPath = configuration.GetValue<string>("Logging:FilePath") ?? "logs/product-service-.log";

            loggerConfig
                .WriteTo.File(
                    new JsonFormatter(), // Structured JSON logs
                    logPath,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 30,
                    shared: true,
                    flushToDiskInterval: TimeSpan.FromSeconds(1))
                .WriteTo.Console(new JsonFormatter()); // JSON console in production
        }

        // HTTP request logging
        loggerConfig.WriteTo.Logger(lc => lc
            .Filter.ByIncludingOnly(evt => evt.Properties.ContainsKey("RequestPath"))
            .WriteTo.File(
                "logs/product-service-requests-.log",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7));

        Log.Logger = loggerConfig.CreateLogger();

        // Add Serilog to DI container
        services.AddLogging(loggingBuilder =>
            loggingBuilder.ClearProviders().AddSerilog(dispose: true));

        return services;
    }

    /// <summary>
    /// Configure Serilog request logging middleware
    /// Logs HTTP requests với timing, status codes, user info
    /// </summary>
    public static IApplicationBuilder UseSerilogRequestLogging(this IApplicationBuilder app)
    {
        return app.UseSerilogRequestLogging(options =>
        {
            options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000}ms";

            // Enrich logs với additional context
            options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
            {
                diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
                diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
                diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent.FirstOrDefault());
                diagnosticContext.Set("ClientIP", httpContext.Connection.RemoteIpAddress?.ToString());

                if (httpContext.User.Identity?.IsAuthenticated == true)
                {
                    diagnosticContext.Set("UserId", httpContext.User.FindFirst("sub")?.Value);
                    diagnosticContext.Set("UserName", httpContext.User.Identity.Name);
                }

                // Add correlation ID if available
                var correlationId = httpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault()
                                   ?? httpContext.TraceIdentifier;
                diagnosticContext.Set("CorrelationId", correlationId);
            };

            // Log level rules
            options.GetLevel = (httpContext, elapsed, ex) =>
            {
                if (ex != null) return LogEventLevel.Error;
                if (httpContext.Response.StatusCode >= 500) return LogEventLevel.Error;
                if (httpContext.Response.StatusCode >= 400) return LogEventLevel.Warning;
                if (elapsed > 10000) return LogEventLevel.Warning; // Slow requests
                return LogEventLevel.Information;
            };
        });
    }

    private static string GetAssemblyVersion()
    {
        return System.Reflection.Assembly.GetExecutingAssembly()
            .GetName().Version?.ToString() ?? "1.0.0";
    }
}