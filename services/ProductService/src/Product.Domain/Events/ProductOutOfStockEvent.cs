using Product.Domain.Common;

namespace Product.Domain.Events;

/// <summary>
/// Domain Event: Sản phẩm hết hàng hoàn toàn
/// Event critical cho business:
/// - Ngừng nhận order cho sản phẩm này
/// - Hide khỏi catalog
/// - Notify customers đang wait-list
/// - Urgent restock action required
/// </summary>
public record ProductOutOfStockEvent(
    Guid ProductId,
    string ProductName,
    string ProductSku,
    string ReportedBy) : IDomainEvent
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
