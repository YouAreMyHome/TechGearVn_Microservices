namespace Product.Infrastructure.Persistence.Outbox;

/// <summary>
/// Outbox Message entity cho Outbox Pattern implementation
/// Đảm bảo Domain Events được publish reliable trong microservices architecture
/// Events được persist vào database trước, sau đó publish async bởi background service
/// </summary>
public class OutboxMessage
{
    /// <summary>
    /// Unique identifier cho message
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Type của Domain Event (FullName của class)
    /// Dùng để deserialize lại correct type khi processing
    /// Ví dụ: "Product.Domain.Events.ProductCreatedEvent"
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Serialized Domain Event content (JSON format)
    /// Chứa toàn bộ data của Domain Event để có thể reconstruct khi publish
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Thời điểm Domain Event được tạo ra (event occurred time)
    /// Important cho event ordering và debugging
    /// </summary>
    public DateTime OccurredOn { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Thời điểm event được process thành công (published time)
    /// Null = chưa được process
    /// Not null = đã publish thành công
    /// </summary>
    public DateTime? ProcessedOn { get; set; }

    /// <summary>
    /// Error message nếu có lỗi khi publish event
    /// Null = không có lỗi
    /// Not null = có lỗi, chứa error details để debugging
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// Số lần đã retry publish event này
    /// Dùng để implement retry policy với max attempts
    /// </summary>
    public int RetryCount { get; set; } = 0;

    /// <summary>
    /// Số lần retry tối đa cho event này
    /// Sau khi vượt quá sẽ mark as failed và cần manual intervention
    /// </summary>
    public int MaxRetryCount { get; set; } = 3;

    /// <summary>
    /// Kiểm tra event đã được process thành công chưa
    /// Business logic để filter processed vs unprocessed events
    /// </summary>
    public bool IsProcessed => ProcessedOn.HasValue;

    /// <summary>
    /// Kiểm tra event có thể retry không
    /// Business logic để implement retry policy
    /// </summary>
    public bool CanRetry => RetryCount < MaxRetryCount && !IsProcessed;

    /// <summary>
    /// Kiểm tra event đã failed permanently
    /// Events vượt quá max retry count cần manual handling
    /// </summary>
    public bool IsPermanentlyFailed => RetryCount >= MaxRetryCount && !IsProcessed;
}