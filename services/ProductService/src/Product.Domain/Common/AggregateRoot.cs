namespace Product.Domain.Common;

/// <summary>
/// Aggregate Root = Entity chính quản lý toàn bộ Aggregate
/// Chỉ Aggregate Root mới được access từ bên ngoài
/// Chịu trách nhiệm duy trì business invariants
/// </summary>
public abstract class AggregateRoot<TId> : Entity<TId>
{
    private readonly List<IDomainEvent> _domainEvents = new();

    /// <summary>
    /// Danh sách Domain Events đã xảy ra trong Aggregate
    /// ReadOnly để không cho bên ngoài modify trực tiếp
    /// </summary>
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected AggregateRoot() { }

    protected AggregateRoot(TId id) : base(id) { }

    /// <summary>
    /// Thêm Domain Event khi có business action quan trọng
    /// Protected để chỉ cho phép chính Aggregate thêm event
    /// </summary>
    protected void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    /// <summary>
    /// Xóa tất cả Domain Events sau khi đã publish
    /// Được gọi bởi Infrastructure layer sau SaveChanges
    /// </summary>
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}