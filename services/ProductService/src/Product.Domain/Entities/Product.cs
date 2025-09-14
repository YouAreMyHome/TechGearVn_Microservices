using Product.Domain.Common;
using Product.Domain.Events;
using Product.Domain.ValueObjects;

namespace Product.Domain.Entities;

/// <summary>
/// Product Aggregate Root - quản lý toàn bộ lifecycle của sản phẩm
/// Chứa business rules quan trọng: giá cả, tồn kho, trạng thái
/// </summary>
public class Product : AggregateRoot<Guid>
{
    // Properties chính của sản phẩm
    public ProductName Name { get; private set; } = default!;
    public ProductSku Sku { get; private set; } = default!;
    public string Description { get; private set; } = string.Empty;
    public Money Price { get; private set; } = default!;
    public int StockQuantity { get; private set; }
    public Guid CategoryId { get; private set; }
    public bool IsActive { get; private set; }

    // Audit fields
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public string CreatedBy { get; private set; } = default!;
    public string? UpdatedBy { get; private set; }

    // Constructor for EF Core (private để không dùng từ bên ngoài)
    private Product() { }

    /// <summary>
    /// Factory method để tạo Product mới với validation đầy đủ
    /// Đây là cách CHÍNH để tạo Product trong domain
    /// </summary>
    public static Product Create(
        string name,
        string? sku,
        string description,
        decimal price,
        string currency,
        int initialStock,
        Guid categoryId,
        string createdBy)
    {
        // Validation business rules
        if (initialStock < 0)
            throw new ArgumentException("Số lượng tồn kho ban đầu không được âm", nameof(initialStock));

        if (categoryId == Guid.Empty)
            throw new ArgumentException("CategoryId không được rỗng", nameof(categoryId));

        if (string.IsNullOrWhiteSpace(createdBy))
            throw new ArgumentException("Người tạo không được để trống", nameof(createdBy));

        // Tạo Value Objects với validation
        var productName = ProductName.Create(name);
        var productSku = string.IsNullOrWhiteSpace(sku)
            ? ProductSku.Generate()
            : ProductSku.Create(sku);
        var productPrice = Money.Create(price, currency);

        // Tạo Product instance
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = productName,
            Sku = productSku,
            Description = description?.Trim() ?? string.Empty,
            Price = productPrice,
            StockQuantity = initialStock,
            CategoryId = categoryId,
            IsActive = true, // Mặc định active khi tạo mới
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };

        // Phát Domain Event: Sản phẩm đã được tạo
        product.AddDomainEvent(new ProductCreatedEvent(
            product.Id,
            product.Name.Value,
            product.Sku.Value,
            product.Price,
            product.CategoryId,
            createdBy));

        return product;
    }

    /// <summary>
    /// Cập nhật giá sản phẩm với business validation
    /// Chỉ cho phép tăng/giảm giá trong khoảng hợp lý
    /// </summary>
    public void UpdatePrice(decimal newPrice, string updatedBy)
    {
        if (string.IsNullOrWhiteSpace(updatedBy))
            throw new ArgumentException("Người cập nhật không được để trống", nameof(updatedBy));

        var oldPrice = Price;
        var newPriceValue = Money.Create(newPrice, Price.Currency);

        // Business rule: Không cho phép thay đổi giá quá 50% trong 1 lần
        var changePercentage = Math.Abs(newPrice - oldPrice.Amount) / oldPrice.Amount;
        if (changePercentage > 0.5m)
            throw new InvalidOperationException($"Không thể thay đổi giá quá 50% trong một lần. Thay đổi hiện tại: {changePercentage:P}");

        Price = newPriceValue;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = updatedBy;

        // Phát Domain Event: Giá đã thay đổi
        AddDomainEvent(new ProductPriceChangedEvent(
            Id,
            Name.Value,
            oldPrice,
            newPriceValue,
            updatedBy));
    }

    /// <summary>
    /// Cập nhật tồn kho với business validation
    /// </summary>
    public void UpdateStock(int newQuantity, string updatedBy, string reason = "Manual Update")
    {
        if (newQuantity < 0)
            throw new ArgumentException("Số lượng tồn kho không được âm", nameof(newQuantity));

        if (string.IsNullOrWhiteSpace(updatedBy))
            throw new ArgumentException("Người cập nhật không được để trống", nameof(updatedBy));

        var oldQuantity = StockQuantity;
        StockQuantity = newQuantity;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = updatedBy;

        // Phát event khi tồn kho thay đổi
        AddDomainEvent(new ProductStockChangedEvent(
            Id,
            Name.Value,
            oldQuantity,
            newQuantity,
            reason,
            updatedBy));

        // Cảnh báo low stock (business rule: dưới 10 là low stock)
        if (newQuantity <= 10 && newQuantity > 0)
        {
            AddDomainEvent(new ProductLowStockEvent(
                Id,
                Name.Value,
                Sku.Value,
                newQuantity,
                updatedBy));
        }

        // Cảnh báo out of stock
        if (newQuantity == 0)
        {
            AddDomainEvent(new ProductOutOfStockEvent(
                Id,
                Name.Value,
                Sku.Value,
                updatedBy));
        }
    }

    /// <summary>
    /// Giảm tồn kho khi bán hàng
    /// Đảm bảo không bán quá số lượng có sẵn
    /// </summary>
    public void ReduceStock(int quantity, string reason = "Sale")
    {
        if (quantity <= 0)
            throw new ArgumentException("Số lượng giảm phải lớn hơn 0", nameof(quantity));

        if (StockQuantity < quantity)
            throw new InvalidOperationException($"Không đủ tồn kho. Hiện có: {StockQuantity}, yêu cầu: {quantity}");

        UpdateStock(StockQuantity - quantity, "System", reason);
    }

    /// <summary>
    /// Kích hoạt sản phẩm
    /// </summary>
    public void Activate(string updatedBy)
    {
        if (IsActive) return; // Đã active rồi

        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = updatedBy;

        AddDomainEvent(new ProductActivatedEvent(Id, Name.Value, updatedBy));
    }

    /// <summary>
    /// Vô hiệu hóa sản phẩm (soft delete)
    /// </summary>
    public void Deactivate(string updatedBy, string reason = "Manual")
    {
        if (!IsActive) return; // Đã inactive rồi

        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = updatedBy;

        AddDomainEvent(new ProductDeactivatedEvent(Id, Name.Value, reason, updatedBy));
    }

    /// <summary>
    /// Kiểm tra sản phẩm có thể bán không
    /// </summary>
    public bool CanBeSold()
    {
        return IsActive && StockQuantity > 0;
    }

    /// <summary>
    /// Kiểm tra có đủ tồn kho để bán số lượng yêu cầu không
    /// </summary>
    public bool HasSufficientStock(int requestedQuantity)
    {
        return IsActive && StockQuantity >= requestedQuantity;
    }
}