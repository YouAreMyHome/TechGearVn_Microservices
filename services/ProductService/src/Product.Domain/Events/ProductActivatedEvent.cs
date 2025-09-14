using Product.Domain.Common;

namespace Product.Domain.Events;

/// <summary>
/// Domain Event: Sản phẩm đã được kích hoạt
/// </summary>
public record ProductActivatedEvent(
    Guid ProductId,
    string ProductName,
    string ActivatedBy) : IDomainEvent
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}