using Product.Domain.Common;

namespace Product.Domain.Events;

/// <summary>
/// Domain Event: Tồn kho sản phẩm đã thay đổi
/// Event này quan trọng cho supply chain management
/// </summary>
public record ProductStockChangedEvent(
    Guid ProductId,
    string ProductName,
    int OldQuantity,
    int NewQuantity,
    string Reason,
    string UpdatedBy) : IDomainEvent
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}