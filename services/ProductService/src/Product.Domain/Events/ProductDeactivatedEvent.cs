using Product.Domain.Common;

namespace Product.Domain.Events;

/// <summary>
/// Domain Event: Sản phẩm đã bị vô hiệu hóa
/// </summary>
public record ProductDeactivatedEvent(
    Guid ProductId,
    string ProductName,
    string Reason,
    string DeactivatedBy) : IDomainEvent
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}