using Product.Domain.Common;
using Product.Domain.ValueObjects;

namespace Product.Domain.Events;

/// <summary>
/// Domain Event: Sản phẩm mới đã được tạo
/// Event này trigger khi có sản phẩm mới trong hệ thống
/// </summary>
public record ProductCreatedEvent(
    Guid ProductId,
    string ProductName,
    string ProductSku,
    Money Price,
    Guid CategoryId,
    string CreatedBy) : IDomainEvent
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}