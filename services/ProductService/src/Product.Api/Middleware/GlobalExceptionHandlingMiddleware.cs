using Microsoft.AspNetCore.Mvc;
using System.Diagnostics; // ← Thêm using này cho Activity
using System.Net;
using System.Text.Json;

namespace Product.Api.Middleware;

/// <summary>
/// Global Exception Handling Middleware cho Product Service
/// Tuân thủ Clean Architecture: API layer handle technical concerns (HTTP errors)
/// Không chứa business logic, chỉ convert exceptions thành proper HTTP responses
/// Structured logging với correlation tracking cho troubleshooting
/// </summary>
public class GlobalExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;

    /// <summary>
    /// Initialize GlobalExceptionHandlingMiddleware
    /// </summary>
    /// <param name="next">Next middleware delegate</param>
    /// <param name="logger">Logger instance</param>
    public GlobalExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// Handle HTTP request và catch exceptions
    /// </summary>
    /// <param name="context">HTTP context</param>
    /// <returns>Task</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception,
                "Unhandled exception occurred. TraceId: {TraceId}, Path: {Path}",
                Activity.Current?.Id ?? context.TraceIdentifier,
                context.Request.Path);

            await HandleExceptionAsync(context, exception);
        }
    }

    /// <summary>
    /// Convert exceptions thành proper HTTP responses
    /// Business exceptions → 400/404/409, Technical exceptions → 500
    /// </summary>
    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var (statusCode, problemDetails) = exception switch
        {
            // Business Logic Exceptions (từ Domain/Application layers)
            ArgumentException argEx => (
                HttpStatusCode.BadRequest,
                CreateProblemDetails("Dữ liệu đầu vào không hợp lệ", argEx.Message, context)
            ),

            InvalidOperationException invalidOpEx => (
                HttpStatusCode.UnprocessableEntity,
                CreateProblemDetails("Thao tác không hợp lệ", invalidOpEx.Message, context)
            ),

            KeyNotFoundException notFoundEx => (
                HttpStatusCode.NotFound,
                CreateProblemDetails("Không tìm thấy", notFoundEx.Message, context)
            ),

            // TODO: Add custom business exceptions
            // ProductNotFoundException => (HttpStatusCode.NotFound, ...),
            // DuplicateSkuException => (HttpStatusCode.Conflict, ...),

            // Technical Exceptions (Database, Network, etc.)
            TimeoutException timeoutEx => (
                HttpStatusCode.RequestTimeout,
                CreateProblemDetails("Timeout", "Yêu cầu mất quá nhiều thời gian", context)
            ),

            // Default: Internal Server Error
            _ => (
                HttpStatusCode.InternalServerError,
                CreateProblemDetails("Lỗi hệ thống", "Đã xảy ra lỗi không mong muốn", context)
            )
        };

        context.Response.StatusCode = (int)statusCode;

        var jsonResponse = JsonSerializer.Serialize(problemDetails, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });

        await context.Response.WriteAsync(jsonResponse);
    }

    /// <summary>
    /// Tạo ProblemDetails theo RFC 7807 standard
    /// Structured error response cho API clients
    /// </summary>
    private static ProblemDetails CreateProblemDetails(
        string title,
        string detail,
        HttpContext context)
    {
        return new ProblemDetails
        {
            Title = title,
            Detail = detail,
            Status = context.Response.StatusCode,
            Instance = context.Request.Path,
            Type = $"https://httpstatuses.com/{context.Response.StatusCode}",
            Extensions = new Dictionary<string, object?>
            {
                ["traceId"] = Activity.Current?.Id ?? context.TraceIdentifier,
                ["timestamp"] = DateTime.UtcNow // ← Fix warning: sử dụng parameter
            }
        };
    }
}

/// <summary>
/// Error response record for consistent API error structure
/// </summary>
public record ApiErrorResponse(
    string Title,
    string Detail,
    int Status,
    string TraceId, // ← Fix warning: sử dụng parameter
    DateTime Timestamp) // ← Fix warning: sử dụng parameter
{
    // Auto-implemented properties sẽ sử dụng parameters từ primary constructor
}