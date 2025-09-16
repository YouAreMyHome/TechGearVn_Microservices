using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Product.Application.Behaviors;

/// <summary>
/// MediatR Pipeline Behavior để log tất cả requests và responses
/// Chạy sau ValidationBehavior, trước Handler chính
/// Quan trọng cho monitoring, debugging và audit trail
/// </summary>
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : class, IRequest<TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var requestGuid = Guid.NewGuid().ToString();

        // Log request bắt đầu với details
        _logger.LogInformation(
            "[{RequestGuid}] Handling request {RequestName} - {RequestData}",
            requestGuid,
            requestName,
            JsonSerializer.Serialize(request, new JsonSerializerOptions
            {
                WriteIndented = false,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }));

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            // Execute Handler chính
            var response = await next();

            stopwatch.Stop();

            // Log thành công
            _logger.LogInformation(
                "[{RequestGuid}] Request {RequestName} handled successfully in {ElapsedMs}ms",
                requestGuid,
                requestName,
                stopwatch.ElapsedMilliseconds);

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            // Log lỗi với full exception details
            _logger.LogError(ex,
                "[{RequestGuid}] Request {RequestName} failed after {ElapsedMs}ms - {ErrorMessage}",
                requestGuid,
                requestName,
                stopwatch.ElapsedMilliseconds,
                ex.Message);

            throw; // Re-throw để không thay đổi exception behavior
        }
    }
}