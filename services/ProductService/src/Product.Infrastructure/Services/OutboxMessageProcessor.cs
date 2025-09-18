using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Product.Infrastructure.Persistence;
using System.Text.Json;

namespace Product.Infrastructure.Services;

/// <summary>
/// Background Service để process Outbox Messages
/// Outbox Pattern: Reliable Domain Events publishing trong Microservices
/// Chạy continuously để publish events từ database ra Message Bus
/// </summary>
public class OutboxMessageProcessor : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxMessageProcessor> _logger;
    private readonly TimeSpan _processingInterval = TimeSpan.FromSeconds(10); // Process mỗi 10 giây

    public OutboxMessageProcessor(
        IServiceProvider serviceProvider,
        ILogger<OutboxMessageProcessor> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Outbox Message Processor started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessUnprocessedMessagesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing outbox messages");
            }

            // Đợi interval trước khi process lần tiếp theo
            await Task.Delay(_processingInterval, stoppingToken);
        }

        _logger.LogInformation("Outbox Message Processor stopped");
    }

    /// <summary>
    /// Process tất cả unprocessed outbox messages
    /// </summary>
    private async Task ProcessUnprocessedMessagesAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ProductDbContext>();

        // Lấy tất cả messages chưa process hoặc cần retry
        var unprocessedMessages = await context.OutboxMessages
            .Where(m => m.ProcessedOn == null && m.CanRetry)
            .OrderBy(m => m.OccurredOn)
            .Take(100) // Process tối đa 100 messages mỗi lần
            .ToListAsync(cancellationToken);

        if (!unprocessedMessages.Any())
        {
            _logger.LogDebug("No unprocessed outbox messages found");
            return;
        }

        _logger.LogInformation("Processing {MessageCount} outbox messages", unprocessedMessages.Count);

        foreach (var message in unprocessedMessages)
        {
            try
            {
                await ProcessSingleMessageAsync(message, cancellationToken);

                // Mark as processed
                message.ProcessedOn = DateTime.UtcNow;
                message.Error = null;

                _logger.LogDebug("Successfully processed outbox message {MessageId} of type {MessageType}",
                    message.Id, message.Type);
            }
            catch (Exception ex)
            {
                // Mark retry
                message.RetryCount++;
                message.Error = ex.Message;

                _logger.LogWarning(ex,
                    "Failed to process outbox message {MessageId} of type {MessageType}. Retry count: {RetryCount}",
                    message.Id, message.Type, message.RetryCount);
            }
        }

        // Save tất cả changes (processed status, retry counts, errors)
        await context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Finished processing outbox messages");
    }

    /// <summary>
    /// Process single outbox message
    /// Deserialize và publish Domain Event
    /// </summary>
    private async Task ProcessSingleMessageAsync(
        Persistence.Outbox.OutboxMessage message,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Processing outbox message {MessageId} of type {MessageType}",
            message.Id, message.Type);

        // Deserialize Domain Event từ JSON
        var eventType = Type.GetType(message.Type);
        if (eventType == null)
        {
            throw new InvalidOperationException($"Could not resolve event type: {message.Type}");
        }

        var domainEvent = JsonSerializer.Deserialize(message.Content, eventType);
        if (domainEvent == null)
        {
            throw new InvalidOperationException($"Could not deserialize event content for type: {message.Type}");
        }

        // TODO: Publish Domain Event ra Message Bus (RabbitMQ/Kafka)
        // Tạm thời chỉ log để test Outbox Pattern
        _logger.LogInformation("Publishing domain event {EventType} with content: {EventContent}",
            eventType.Name, message.Content);

        // Simulate async publishing
        await Task.Delay(100, cancellationToken);

        // Trong thực tế sẽ publish ra RabbitMQ/Kafka:
        // await _messageBus.PublishAsync(domainEvent, cancellationToken);
    }
}