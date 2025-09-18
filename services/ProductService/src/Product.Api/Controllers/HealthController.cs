using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Product.Api.Controllers;

/// <summary>
/// Health Check Controller cho Product Service
/// Microservices pattern: Cung cấp detailed health information cho service discovery và monitoring
/// Kubernetes: Readiness và Liveness probes sẽ call endpoints này
/// </summary>
[ApiController]
[Route("api/[controller]")]
[ApiExplorerSettings(IgnoreApi = true)] // Không hiển thị trong Swagger
public class HealthController : ControllerBase
{
    private readonly HealthCheckService _healthCheckService;
    private readonly ILogger<HealthController> _logger;

    /// <summary>
    /// Initialize HealthController với dependencies
    /// </summary>
    /// <param name="healthCheckService">Health check service</param>
    /// <param name="logger">Logger instance</param>
    public HealthController(
        HealthCheckService healthCheckService,
        ILogger<HealthController> logger)
    {
        _healthCheckService = healthCheckService;
        _logger = logger;
    }

    /// <summary>
    /// Basic health check endpoint
    /// Kubernetes liveness probe: GET /api/health/alive
    /// </summary>
    [HttpGet("alive")]
    public IActionResult Alive()
    {
        _logger.LogDebug("Liveness probe called");

        return Ok(new
        {
            status = "Alive",
            timestamp = DateTime.UtcNow,
            service = "Product Service",
            version = "1.0.0"
        });
    }

    /// <summary>
    /// Detailed readiness check
    /// Kubernetes readiness probe: GET /api/health/ready
    /// Kiểm tra database connection, dependencies
    /// </summary>
    [HttpGet("ready")]
    public async Task<IActionResult> Ready()
    {
        _logger.LogDebug("Readiness probe called");

        try
        {
            var healthReport = await _healthCheckService.CheckHealthAsync();

            var response = new
            {
                status = healthReport.Status.ToString(),
                timestamp = DateTime.UtcNow,
                service = "Product Service",
                version = "1.0.0",
                totalDuration = healthReport.TotalDuration.TotalMilliseconds,
                checks = healthReport.Entries.Select(x => new
                {
                    name = x.Key,
                    status = x.Value.Status.ToString(),
                    description = x.Value.Description,
                    duration = x.Value.Duration.TotalMilliseconds,
                    exception = x.Value.Exception?.Message
                })
            };

            var statusCode = healthReport.Status == HealthStatus.Healthy ? 200 : 503;
            return StatusCode(statusCode, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed with exception");

            return StatusCode(503, new
            {
                status = "Unhealthy",
                timestamp = DateTime.UtcNow,
                service = "Product Service",
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Service info endpoint
    /// Service discovery: Metadata về service capabilities
    /// </summary>
    [HttpGet("info")]
    public IActionResult Info()
    {
        var response = new
        {
            service = new
            {
                name = "Product Service",
                version = "1.0.0",
                description = "Product management service trong TechGear Microservices Architecture",
                capabilities = new[]
                {
                    "Product CRUD operations",
                    "Product catalog browsing",
                    "Inventory management",
                    "Price management",
                    "SKU-based product lookup"
                }
            },
            environment = new
            {
                name = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
                dotnetVersion = Environment.Version.ToString(),
                machineName = Environment.MachineName,
                timestamp = DateTime.UtcNow
            },
            dependencies = new
            {
                database = new
                {
                    type = "PostgreSQL",
                    host = Environment.GetEnvironmentVariable("DB_HOST"),
                    database = Environment.GetEnvironmentVariable("DB_NAME")
                },
                messaging = new
                {
                    type = "RabbitMQ", // TODO: Implement when add message bus
                    status = "Not configured yet"
                }
            }
        };

        return Ok(response);
    }
}