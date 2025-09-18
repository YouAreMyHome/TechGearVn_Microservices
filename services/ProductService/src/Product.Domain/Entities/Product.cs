using Product.Domain.Common;
using Product.Domain.Events;
using Product.Domain.ValueObjects;

namespace Product.Domain.Entities;

/// <summary>
/// Product Aggregate Root - quản lý toàn bộ lifecycle của sản phẩm
/// DDD Pattern: Chứa business rules quan trọng về giá cả, tồn kho, trạng thái
/// Clean Architecture: Không depend vào technical concerns (EF Core, HTTP, etc.)
/// </summary>
public class Product : AuditableEntity<Guid>
{
    #region Properties - Domain Concepts

    /// <summary>
    /// Tên sản phẩm - Value Object đảm bảo business rules
    /// </summary>
    public ProductName Name { get; private set; } = default!;

    /// <summary>
    /// SKU - Business identifier duy nhất cho sản phẩm
    /// </summary>
    public ProductSku Sku { get; private set; } = default!;

    /// <summary>
    /// Mô tả chi tiết sản phẩm
    /// </summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>
    /// Giá sản phẩm - Money Value Object với currency và business validation
    /// </summary>
    public Money Price { get; private set; } = default!;

    /// <summary>
    /// Số lượng tồn kho hiện tại
    /// Business rule: Không được âm, có cảnh báo low stock
    /// </summary>
    public int StockQuantity { get; private set; }

    /// <summary>
    /// Category ID - Reference đến Category aggregate
    /// </summary>
    public Guid CategoryId { get; private set; }

    /// <summary>
    /// Trạng thái active/inactive của sản phẩm
    /// Business logic: Chỉ active products mới có thể bán
    /// </summary>
    public bool IsActive { get; private set; }

    #endregion

    #region Constructors

    /// <summary>
    /// Private constructor cho EF Core
    /// Infrastructure concern: EF Core cần parameterless constructor
    /// </summary>
    private Product() { }

    /// <summary>
    /// Private constructor với full parameters
    /// Domain logic: Chỉ factory method mới có thể tạo Product
    /// </summary>
    private Product(
        ProductName name,
        ProductSku sku,
        string description,
        Money price,
        int stockQuantity,
        Guid categoryId,
        string createdBy)
    {
        Id = Guid.NewGuid();
        Name = name;
        Sku = sku;
        Description = description;
        Price = price;
        StockQuantity = stockQuantity;
        CategoryId = categoryId;
        IsActive = true;

        // Audit trail từ base class
        SetCreatedAudit(createdBy);
    }

    #endregion

    #region Factory Methods

    /// <summary>
    /// Factory method chính để tạo Product với Money Value Object
    /// Business orchestration: Validate tất cả business rules và tạo domain events
    /// </summary>
    /// <param name="name">Tên sản phẩm</param>
    /// <param name="sku">SKU (optional - sẽ auto-generate nếu null)</param>
    /// <param name="description">Mô tả sản phẩm</param>
    /// <param name="price">Money object với amount và currency</param>
    /// <param name="initialStock">Số lượng tồn kho ban đầu</param>
    /// <param name="categoryId">ID của category</param>
    /// <param name="createdBy">Người tạo sản phẩm</param>
    /// <returns>Product entity với domain events</returns>
    public static Product Create(
        string name,
        string? sku,
        string description,
        Money price,
        int initialStock,
        Guid categoryId,
        string createdBy)
    {
        // Business validation: Input parameters
        if (price == null)
            throw new ArgumentNullException(nameof(price), "Price không được null");

        if (initialStock < 0)
            throw new ArgumentException("Số lượng tồn kho ban đầu không được âm", nameof(initialStock));

        if (categoryId == Guid.Empty)
            throw new ArgumentException("CategoryId không được rỗng", nameof(categoryId));

        if (string.IsNullOrWhiteSpace(createdBy))
            throw new ArgumentException("CreatedBy không được để trống", nameof(createdBy));

        // Tạo Value Objects với business validation
        var productName = ProductName.Create(name);
        var productSku = string.IsNullOrWhiteSpace(sku)
            ? ProductSku.Generate()
            : ProductSku.Create(sku);

        // Tạo Product instance
        var product = new Product(
            productName,
            productSku,
            description?.Trim() ?? string.Empty,
            price,
            initialStock,
            categoryId,
            createdBy);

        // Domain Event: Sản phẩm đã được tạo
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
    /// Factory method với primitive price parameters
    /// Convenience method: Convert primitives thành Money object
    /// </summary>
    public static Product Create(
        string name,
        string? sku,
        string description,
        decimal priceAmount,
        string currency,
        int initialStock,
        Guid categoryId,
        string createdBy)
    {
        // Tạo Money Value Object từ primitives
        var price = Money.Create(priceAmount, currency);

        // Delegate về main factory method
        return Create(name, sku, description, price, initialStock, categoryId, createdBy);
    }

    #endregion

    #region Business Methods - Product Information

    /// <summary>
    /// Cập nhật thông tin cơ bản của sản phẩm
    /// Business operation: Name và Description changes với audit trail
    /// </summary>
    public void UpdateDetails(string name, string description, string updatedBy)
    {
        // Business validation
        if (string.IsNullOrWhiteSpace(updatedBy))
            throw new ArgumentException("Người cập nhật không được để trống", nameof(updatedBy));

        var oldName = Name.Value;

        // Update domain properties
        Name = ProductName.Create(name);
        Description = description?.Trim() ?? string.Empty;

        // Audit trail
        SetUpdatedAudit(updatedBy);

        // Domain Event: Thông tin sản phẩm đã thay đổi
        if (oldName != Name.Value)
        {
            AddDomainEvent(new ProductDetailsChangedEvent(
                Id,
                oldName,
                Name.Value,
                updatedBy));
        }
    }

    #endregion

    #region Business Methods - Price Management

    /// <summary>
    /// Cập nhật giá sản phẩm với Money Value Object
    /// Business rules: Validation trong Money object, price change limits
    /// </summary>
    /// <param name="newPrice">Money object với amount và currency mới</param>
    /// <param name="updatedBy">Người thực hiện update</param>
    public void UpdatePrice(Money newPrice, string updatedBy)
    {
        // Business validation: Input parameters
        if (newPrice == null)
            throw new ArgumentNullException(nameof(newPrice), "Price mới không được null");

        if (string.IsNullOrWhiteSpace(updatedBy))
            throw new ArgumentException("UpdatedBy không được để trống", nameof(updatedBy));

        // Business rule: Chỉ có thể update price nếu product active
        if (!IsActive)
            throw new InvalidOperationException("Không thể update price cho inactive product");

        var oldPrice = Price;

        // Business rule: Currency phải consistent (có thể relax rule này tùy business)
        if (oldPrice.Currency != newPrice.Currency)
            throw new InvalidOperationException($"Currency mismatch: {oldPrice.Currency} vs {newPrice.Currency}");

        // Business rule: Không cho phép thay đổi giá quá 50% trong 1 lần
        var changePercentage = Math.Abs(newPrice.Amount - oldPrice.Amount) / oldPrice.Amount;
        if (changePercentage > 0.5m)
            throw new InvalidOperationException($"Không thể thay đổi giá quá 50% trong một lần. Thay đổi hiện tại: {changePercentage:P}");

        // Update domain state
        Price = newPrice;
        SetUpdatedAudit(updatedBy);

        // Domain Event: Giá đã thay đổi
        AddDomainEvent(new ProductPriceChangedEvent(
            Id,
            Name.Value,
            oldPrice,
            newPrice,
            updatedBy));
    }

    /// <summary>
    /// Convenience method: Update price với primitive parameters
    /// Application convenience: Convert primitives thành Money object
    /// </summary>
    public void UpdatePrice(decimal newAmount, string currency, string updatedBy)
    {
        var newPrice = Money.Create(newAmount, currency);
        UpdatePrice(newPrice, updatedBy);
    }

    /// <summary>
    /// Convenience method: Update price amount, giữ nguyên currency
    /// Common use case: Chỉ thay đổi amount, không đổi currency
    /// </summary>
    public void UpdatePriceAmount(decimal newAmount, string updatedBy)
    {
        var newPrice = Money.Create(newAmount, Price.Currency);
        UpdatePrice(newPrice, updatedBy);
    }

    #endregion

    #region Business Methods - Stock Management

    /// <summary>
    /// Cập nhật tồn kho với business validation và events
    /// Business operation: Stock management với low stock warnings
    /// </summary>
    public void UpdateStock(int newQuantity, string updatedBy, string reason = "Manual Update")
    {
        // Business validation
        if (newQuantity < 0)
            throw new ArgumentException("Số lượng tồn kho không được âm", nameof(newQuantity));

        if (string.IsNullOrWhiteSpace(updatedBy))
            throw new ArgumentException("UpdatedBy không được để trống", nameof(updatedBy));

        var oldQuantity = StockQuantity;

        // Update domain state
        StockQuantity = newQuantity;
        SetUpdatedAudit(updatedBy);

        // Domain Event: Stock changed
        AddDomainEvent(new ProductStockChangedEvent(
            Id,
            Name.Value,
            oldQuantity,
            newQuantity,
            reason,
            updatedBy));

        // Business rule: Low stock warning (threshold = 10)
        if (newQuantity <= 10 && newQuantity > 0)
        {
            AddDomainEvent(new ProductLowStockEvent(
                Id,
                Name.Value,
                Sku.Value,
                newQuantity,
                updatedBy));
        }

        // Business rule: Out of stock warning
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
    /// Business operation: Sales transaction với oversell protection
    /// </summary>
    public void ReduceStock(int quantity, string reason = "Sale")
    {
        // Business validation
        if (quantity <= 0)
            throw new ArgumentException("Số lượng giảm phải lớn hơn 0", nameof(quantity));

        if (!CanBeSold())
            throw new InvalidOperationException("Sản phẩm không thể bán (inactive hoặc out of stock)");

        if (StockQuantity < quantity)
            throw new InvalidOperationException($"Không đủ tồn kho. Hiện có: {StockQuantity}, yêu cầu: {quantity}");

        // Delegate về UpdateStock method
        UpdateStock(StockQuantity - quantity, "System", reason);
    }

    /// <summary>
    /// Tăng tồn kho khi nhập hàng
    /// Business operation: Inventory receiving
    /// </summary>
    public void IncreaseStock(int quantity, string updatedBy, string reason = "Stock Receiving")
    {
        if (quantity <= 0)
            throw new ArgumentException("Số lượng tăng phải lớn hơn 0", nameof(quantity));

        UpdateStock(StockQuantity + quantity, updatedBy, reason);
    }

    #endregion

    #region Business Methods - Product Lifecycle

    /// <summary>
    /// Kích hoạt sản phẩm
    /// Business operation: Product activation với domain event
    /// </summary>
    public void Activate(string updatedBy)
    {
        if (string.IsNullOrWhiteSpace(updatedBy))
            throw new ArgumentException("UpdatedBy không được để trống", nameof(updatedBy));

        if (IsActive) return; // Đã active rồi

        IsActive = true;
        SetUpdatedAudit(updatedBy);

        AddDomainEvent(new ProductActivatedEvent(Id, Name.Value, updatedBy));
    }

    /// <summary>
    /// Vô hiệu hóa sản phẩm (soft deactivate)
    /// Business operation: Product deactivation với audit trail
    /// </summary>
    public void Deactivate(string updatedBy, string reason = "Manual")
    {
        if (string.IsNullOrWhiteSpace(updatedBy))
            throw new ArgumentException("UpdatedBy không được để trống", nameof(updatedBy));

        if (!IsActive) return; // Đã inactive rồi

        IsActive = false;
        SetUpdatedAudit(updatedBy);

        AddDomainEvent(new ProductDeactivatedEvent(Id, Name.Value, reason, updatedBy));
    }

    /// <summary>
    /// Soft delete product với business validation
    /// Business operation: Product deletion với cross-service notification
    /// </summary>
    public void MarkAsDeleted(string deletedBy, string? reason = null)
    {
        // Business validation
        if (string.IsNullOrWhiteSpace(deletedBy))
            throw new ArgumentException("DeletedBy không được để trống", nameof(deletedBy));

        // Business rule: Chỉ có thể delete inactive products
        if (IsActive)
            throw new InvalidOperationException($"Phải deactivate sản phẩm '{Name.Value}' trước khi delete");

        // Update entity state (IsActive đã false, nhưng có thể thêm DeletedAt flag)
        SetUpdatedAudit(deletedBy);

        // Domain Event: Notify other bounded contexts
        AddDomainEvent(new ProductDeletedEvent(
            Id,
            Name.Value,
            Sku.Value,
            deletedBy,
            reason));
    }

    #endregion

    #region Business Query Methods

    /// <summary>
    /// Kiểm tra sản phẩm có thể bán không
    /// Business rule: Active và có tồn kho
    /// </summary>
    public bool CanBeSold()
    {
        return IsActive && StockQuantity > 0;
    }

    /// <summary>
    /// Kiểm tra có đủ tồn kho để bán số lượng yêu cầu không
    /// Business validation: Prevent overselling
    /// </summary>
    public bool HasSufficientStock(int requestedQuantity)
    {
        return IsActive && StockQuantity >= requestedQuantity && requestedQuantity > 0;
    }

    /// <summary>
    /// Kiểm tra có phải low stock không
    /// Business threshold: <= 10 units
    /// </summary>
    public bool IsLowStock()
    {
        return IsActive && StockQuantity <= 10 && StockQuantity > 0;
    }

    /// <summary>
    /// Kiểm tra có out of stock không
    /// Business condition: Active nhưng stock = 0
    /// </summary>
    public bool IsOutOfStock()
    {
        return IsActive && StockQuantity == 0;
    }

    /// <summary>
    /// Tính toán total value của inventory
    /// Business calculation: Price × Stock
    /// </summary>
    public Money CalculateInventoryValue()
    {
        return Price.Multiply(StockQuantity);
    }

    #endregion
}