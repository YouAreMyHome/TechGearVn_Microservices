using ProductEntity = Product.Domain.Entities.Product;

namespace Product.Domain.Repositories;

/// <summary>
/// Repository interface cho Product Aggregate
/// Interface này định nghĩa contract cho data access
/// Infrastructure layer sẽ implement bằng EF Core
/// Domain không biết gì về database technology
/// </summary>
public interface IProductRepository
{
    // ============ BASIC CRUD OPERATIONS ============

    /// <summary>
    /// Lấy sản phẩm theo ID
    /// Trả null nếu không tìm thấy
    /// </summary>
    Task<ProductEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy sản phẩm theo SKU (unique identifier)
    /// SKU là business key quan trọng cho inventory
    /// </summary>
    Task<ProductEntity?> GetBySkuAsync(string sku, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy tất cả sản phẩm (với pagination)
    /// </summary>
    Task<List<ProductEntity>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Thêm sản phẩm mới vào repository
    /// Aggregate Root sẽ có Domain Events được publish
    /// </summary>
    Task<ProductEntity> AddAsync(ProductEntity product, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cập nhật sản phẩm hiện có
    /// EF Core sẽ track changes và persist
    /// </summary>
    Task UpdateAsync(ProductEntity product, CancellationToken cancellationToken = default);

    /// <summary>
    /// Xóa sản phẩm (hard delete)
    /// Thường không dùng, prefer soft delete (deactivate)
    /// </summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Kiểm tra sản phẩm có tồn tại không
    /// Dùng cho validation, nhanh hơn GetById
    /// </summary>
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);

    // ============ BUSINESS QUERY METHODS ============

    /// <summary>
    /// Lấy sản phẩm theo category
    /// Quan trọng cho catalog browsing
    /// </summary>
    Task<List<ProductEntity>> GetByCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy sản phẩm trong khoảng giá
    /// Dùng cho price filtering
    /// </summary>
    Task<List<ProductEntity>> GetByPriceRangeAsync(
        decimal minPrice,
        decimal maxPrice,
        string currency = "VND",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy sản phẩm có low stock (cần reorder)
    /// Critical cho inventory management
    /// </summary>
    Task<List<ProductEntity>> GetLowStockProductsAsync(
        int threshold = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy sản phẩm active (đang bán)
    /// Chỉ show products available cho customers
    /// </summary>
    Task<List<ProductEntity>> GetActiveProductsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Tìm kiếm sản phẩm theo tên
    /// Cơ bản cho search functionality
    /// </summary>
    Task<List<ProductEntity>> SearchByNameAsync(
        string searchTerm,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy sản phẩm theo list IDs
    /// Dùng cho cart, wishlist, bulk operations
    /// </summary>
    Task<List<ProductEntity>> GetByIdsAsync(
        IEnumerable<Guid> ids,
        CancellationToken cancellationToken = default);

    // ============ AGGREGATE METHODS ============

    /// <summary>
    /// Đếm tổng số sản phẩm theo category
    /// Dùng cho dashboard, reporting
    /// </summary>
    Task<int> CountByCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tính tổng giá trị inventory
    /// Business metric quan trọng
    /// </summary>
    Task<decimal> GetTotalInventoryValueAsync(
        string currency = "VND",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Kiểm tra SKU có unique không (before create/update)
    /// SKU phải unique trong toàn bộ hệ thống
    /// </summary>
    Task<bool> IsSkuUniqueAsync(
        string sku,
        Guid? excludeProductId = null,
        CancellationToken cancellationToken = default);
}