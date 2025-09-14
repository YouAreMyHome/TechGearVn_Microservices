namespace Product.Domain.Common;

/// <summary>
/// Interface cho tất cả Domain Events trong hệ thống
/// Domain Events được trigger khi có thay đổi quan trọng trong Aggregate
/// </summary>
public interface IDomainEvent
{
    /// <summary>
    /// ID duy nhất của event
    /// </summary>
    Guid Id { get; }

    /// <summary>
    /// Thời điểm event xảy ra
    /// </summary>
    DateTime OccurredAt { get; }
}