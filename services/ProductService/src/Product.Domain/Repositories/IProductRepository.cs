namespace Product.Domain.Repositories;

/// <summary>
/// Repository interface cho Product Aggregate
/// Tuân thủ DDD Clean Architecture: Domain định nghĩa contract, Infrastructure implement
/// Chứa tất cả operations cần thiết cho Product business logic
/// Không chứa technical concerns (EF Core, SQL, etc.)
/// </summary>
public interface IProductRepository
{
    #region Basic CRUD Operations

    /// <summary>
    /// Lấy Product theo ID
    /// Core operation cho single entity retrieval
    /// </summary>
    Task<Domain.Entities.Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Thêm Product mới
    /// Aggregate Root sẽ được persist với tất cả child entities
    /// </summary>
    Task<Domain.Entities.Product> AddAsync(Domain.Entities.Product product, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cập nhật Product
    /// Domain logic sẽ handle business rules trước khi persist
    /// </summary>
    Task UpdateAsync(Domain.Entities.Product product, CancellationToken cancellationToken = default);

    /// <summary>
    /// Xóa Product (soft delete thông qua IsActive flag)
    /// Business requirement: Không hard delete để preserve audit trail
    /// </summary>
    Task DeleteAsync(Guid productId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Kiểm tra Product có tồn tại không
    /// Optimize cho existence check mà không cần load full entity
    /// </summary>
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);

    #endregion

    #region Business Query Operations

    /// <summary>
    /// Lấy Product theo SKU (business identifier)
    /// Critical business operation: SKU là unique identifier cho business users
    /// </summary>
    Task<Domain.Entities.Product?> GetBySkuAsync(string sku, CancellationToken cancellationToken = default);

    /// <summary>
    /// Kiểm tra SKU có unique không (cho validation)
    /// Business rule: SKU phải unique across all products
    /// </summary>
    Task<bool> IsSkuUniqueAsync(string sku, Guid? excludeProductId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy products với pagination và advanced filtering
    /// Business use case: Product catalog browsing với performance optimization
    /// </summary>
    Task<(List<Domain.Entities.Product> products, int totalCount)> GetProductsPagedAsync(
        int page,
        int pageSize,
        List<Guid>? categoryIds = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        string? searchTerm = null,
        bool onlyActive = true,
        string sortBy = "name",
        string sortDirection = "asc",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy products có tồn kho thấp
    /// Business use case: Inventory management alerts
    /// </summary>
    Task<List<Domain.Entities.Product>> GetLowStockProductsAsync(
        int threshold = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy tất cả Products đang active
    /// Simple query cho basic listing (không recommend cho production với data lớn)
    /// </summary>
    Task<List<Domain.Entities.Product>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy Products theo CategoryId
    /// Core business operation cho catalog browsing
    /// </summary>
    Task<List<Domain.Entities.Product>> GetByCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy Products trong khoảng giá
    /// Business requirement cho price filtering
    /// </summary>
    Task<List<Domain.Entities.Product>> GetByPriceRangeAsync(
        decimal minPrice,
        decimal maxPrice,
        string currency,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy Products đang active (có thể bán)
    /// Customer-facing operation cho product catalog
    /// </summary>
    Task<List<Domain.Entities.Product>> GetActiveProductsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Search Products theo tên, description, SKU
    /// Customer product discovery operation
    /// </summary>
    Task<List<Domain.Entities.Product>> SearchByNameAsync(string searchTerm, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy Products theo danh sách IDs
    /// Bulk operations cho cart, order processing
    /// </summary>
    Task<List<Domain.Entities.Product>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);

    #endregion

    #region Analytics & Reporting

    /// <summary>
    /// Đếm số lượng Products theo Category
    /// Analytics operation cho business reporting
    /// </summary>
    Task<int> CountByCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tính tổng giá trị inventory theo currency
    /// Financial reporting operation
    /// </summary>
    Task<decimal> GetTotalInventoryValueAsync(string currency, CancellationToken cancellationToken = default);

    #endregion
}