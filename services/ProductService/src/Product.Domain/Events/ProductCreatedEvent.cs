using Product.Domain.Common;
using Product.Domain.ValueObjects;

namespace Product.Domain.Events;

/// <summary>
/// Domain Event khi Product được tạo mới
/// Business significance: Notify other bounded contexts về new product
/// Integration: OrderService, InventoryService, CatalogService cần biết
/// </summary>
public record ProductCreatedEvent : IDomainEvent
{
    /// <summary>
    /// Event ID cho deduplication
    /// </summary>
    public Guid Id { get; } = Guid.NewGuid();

    /// <summary>
    /// Thời điểm event occurred (UTC)
    /// </summary>
    public DateTime OccurredAt { get; } = DateTime.UtcNow;

    /// <summary>
    /// ID của Product được tạo
    /// </summary>
    public Guid ProductId { get; init; }

    /// <summary>
    /// Tên Product
    /// </summary>
    public string ProductName { get; init; }

    /// <summary>
    /// SKU của Product
    /// </summary>
    public string ProductSku { get; init; }

    /// <summary>
    /// Giá Product - Money Value Object
    /// Business data: Other services cần biết price để tính toán
    /// </summary>
    public Money Price { get; init; }

    /// <summary>
    /// Category ID
    /// </summary>
    public Guid CategoryId { get; init; }

    /// <summary>
    /// Người tạo Product
    /// </summary>
    public string CreatedBy { get; init; }

    /// <summary>
    /// Constructor với Money Value Object
    /// </summary>
    public ProductCreatedEvent(
        Guid productId,
        string productName,
        string productSku,
        Money price,
        Guid categoryId,
        string createdBy)
    {
        ProductId = productId;
        ProductName = productName ?? throw new ArgumentNullException(nameof(productName));
        ProductSku = productSku ?? throw new ArgumentNullException(nameof(productSku));
        Price = price ?? throw new ArgumentNullException(nameof(price));
        CategoryId = categoryId;
        CreatedBy = createdBy ?? throw new ArgumentNullException(nameof(createdBy));
    }

    /// <summary>
    /// Parameterless constructor cho serialization
    /// </summary>
    public ProductCreatedEvent()
    {
        ProductName = string.Empty;
        ProductSku = string.Empty;
        Price = Money.Zero("VND");
        CreatedBy = string.Empty;
    }
}