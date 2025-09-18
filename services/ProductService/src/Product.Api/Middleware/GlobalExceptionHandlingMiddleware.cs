using Microsoft.AspNetCore.Mvc;
using System.Diagnostics; // ← Thêm using này cho Activity
using System.Net;
using System.Text.Json;
using Product.Domain.Exceptions;
using Product.Application.Exceptions;

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
            // Application Layer Exceptions
            ValidationException validationEx => (
                HttpStatusCode.BadRequest,
                CreateValidationProblemDetails(validationEx, context)
            ),

            // Domain Layer Exceptions (Business Logic)
            ProductNotFoundException notFoundEx => (
                HttpStatusCode.NotFound,
                CreateProblemDetails("Sản phẩm không tồn tại", notFoundEx.Message, context)
            ),

            ProductSkuAlreadyExistsException duplicateSkuEx => (
                HttpStatusCode.Conflict,
                CreateProblemDetails("SKU đã tồn tại", duplicateSkuEx.Message, context)
            ),

            InvalidStockOperationException stockEx => (
                HttpStatusCode.UnprocessableEntity,
                CreateProblemDetails("Thao tác stock không hợp lệ", stockEx.Message, context)
            ),

            DomainException domainEx => (
                HttpStatusCode.BadRequest,
                CreateProblemDetails("Lỗi business logic", domainEx.Message, context)
            ),

            // Standard .NET Exceptions
            ArgumentException argEx => (
                HttpStatusCode.BadRequest,
                CreateProblemDetails("Dữ liệu đầu vào không hợp lệ", argEx.Message, context)
            ),

            InvalidOperationException invalidOpEx => (
                HttpStatusCode.UnprocessableEntity,
                CreateProblemDetails("Thao tác không hợp lệ", invalidOpEx.Message, context)
            ),

            KeyNotFoundException keyNotFoundEx => (
                HttpStatusCode.NotFound,
                CreateProblemDetails("Không tìm thấy", keyNotFoundEx.Message, context)
            ),

            // Technical Exceptions (Database, Network, etc.)
            TimeoutException timeoutEx => (
                HttpStatusCode.RequestTimeout,
                CreateProblemDetails("Timeout", "Yêu cầu mất quá nhiều thời gian", context)
            ),

            UnauthorizedAccessException => (
                HttpStatusCode.Unauthorized,
                CreateProblemDetails("Không có quyền truy cập", "Bạn không có quyền thực hiện thao tác này", context)
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

    /// <summary>
    /// Tạo ValidationProblemDetails cho FluentValidation errors
    /// Structured validation errors cho API clients
    /// </summary>
    private static ValidationProblemDetails CreateValidationProblemDetails(
        ValidationException validationException,
        HttpContext context)
    {
        var problemDetails = new ValidationProblemDetails(validationException.Errors)
        {
            Title = "Lỗi validation dữ liệu",
            Detail = "Một hoặc nhiều trường dữ liệu không hợp lệ",
            Status = StatusCodes.Status400BadRequest,
            Instance = context.Request.Path,
            Type = "https://httpstatuses.com/400",
            Extensions = new Dictionary<string, object?>
            {
                ["traceId"] = Activity.Current?.Id ?? context.TraceIdentifier,
                ["timestamp"] = DateTime.UtcNow
            }
        };

        return problemDetails;
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