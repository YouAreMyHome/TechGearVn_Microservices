using Product.Domain.Common;

namespace Product.Domain.Events;

/// <summary>
/// Domain Event khi Product bị soft delete
/// Business significance: Thông báo cho other bounded contexts (Order, Inventory, Analytics)
/// Integration: Event này sẽ được publish qua message bus để notify other microservices
/// Use cases: Cancel pending orders, update inventory reports, analytics dashboards
/// </summary>
public record ProductDeletedEvent : IDomainEvent
{
    /// <summary>
    /// Unique identifier cho Domain Event instance
    /// Outbox Pattern: Dùng để deduplication trong event processing
    /// </summary>
    public Guid Id { get; } = Guid.NewGuid();

    /// <summary>
    /// Thời điểm Domain Event được tạo
    /// Business requirement: Audit trail cho business operations
    /// Technical: UTC timestamp để tránh timezone confusion
    /// </summary>
    public DateTime OccurredAt { get; } = DateTime.UtcNow;

    /// <summary>
    /// ID của Product bị delete
    /// Critical business data: Other services cần biết product nào bị ảnh hưởng
    /// </summary>
    public Guid ProductId { get; init; }

    /// <summary>
    /// Tên Product tại thời điểm delete
    /// Business context: Helpful cho logging, notifications, audit reports
    /// </summary>
    public string ProductName { get; init; }

    /// <summary>
    /// SKU của Product (business identifier)
    /// Integration: Other services thường reference product bằng SKU
    /// </summary>
    public string ProductSku { get; init; }

    /// <summary>
    /// User thực hiện delete operation
    /// Audit requirement: Ai đã thực hiện business action này
    /// </summary>
    public string DeletedBy { get; init; }

    /// <summary>
    /// Lý do delete (optional)
    /// Business context: Giải thích business decision
    /// </summary>
    public string? Reason { get; init; }

    /// <summary>
    /// Constructor với required business data
    /// </summary>
    public ProductDeletedEvent(
        Guid productId,
        string productName,
        string productSku,
        string deletedBy,
        string? reason = null)
    {
        ProductId = productId;
        ProductName = productName ?? throw new ArgumentNullException(nameof(productName));
        ProductSku = productSku ?? throw new ArgumentNullException(nameof(productSku));
        DeletedBy = deletedBy ?? throw new ArgumentNullException(nameof(deletedBy));
        Reason = reason;
    }

    /// <summary>
    /// Parameterless constructor cho serialization (JSON, MessagePack)
    /// Technical requirement: Event bus serialization cần default constructor
    /// </summary>
    public ProductDeletedEvent()
    {
        ProductName = string.Empty;
        ProductSku = string.Empty;
        DeletedBy = string.Empty;
    }
}