using Product.Domain.Common;
using Product.Domain.ValueObjects;

namespace Product.Domain.Events;

/// <summary>
/// Domain Event: Giá sản phẩm đã thay đổi
/// Event quan trọng cho business vì ảnh hưởng cart, pricing strategy
/// </summary>
public record ProductPriceChangedEvent(
    Guid ProductId,
    string ProductName,
    Money OldPrice,
    Money NewPrice,
    string UpdatedBy) : IDomainEvent
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}