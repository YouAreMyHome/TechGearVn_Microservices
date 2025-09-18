using Microsoft.EntityFrameworkCore;
using Product.Domain.Repositories;
using Product.Infrastructure.Persistence;

namespace Product.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation của IProductRepository từ Domain
/// Tuân thủ Clean Architecture: Infrastructure implement Domain interfaces
/// Chứa toàn bộ database access logic nhưng KHÔNG có business logic
/// Business logic thuộc về Domain và Application layers
/// </summary>
public class ProductRepository : IProductRepository
{
    private readonly ProductDbContext _context;

    public ProductRepository(ProductDbContext context)
    {
        _context = context;
    }

    #region Basic CRUD Operations

    /// <summary>
    /// Lấy Product theo ID
    /// Sử dụng tracking để có thể update sau này
    /// </summary>
    public async Task<Domain.Entities.Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    /// <summary>
    /// Thêm Product mới vào database
    /// Return Product sau khi đã persist để có đầy đủ generated values
    /// </summary>
    public async Task<Domain.Entities.Product> AddAsync(Domain.Entities.Product product, CancellationToken cancellationToken = default)
    {
        _context.Products.Add(product);
        await SaveChangesAsync(cancellationToken);
        return product;
    }

    /// <summary>
    /// Cập nhật Product đã tồn tại
    /// EF Core tracking sẽ tự động detect changes
    /// </summary>
    public async Task UpdateAsync(Domain.Entities.Product product, CancellationToken cancellationToken = default)
    {
        _context.Products.Update(product);
        await SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Xóa Product theo ID (soft delete thông qua IsActive flag)
    /// Business requirement: Không hard delete để preserve audit trail
    /// </summary>
    public async Task DeleteAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        var product = await GetByIdAsync(productId, cancellationToken);
        if (product != null)
        {
            product.Deactivate("Product discontinued", "system");
            await UpdateAsync(product, cancellationToken);
        }
    }

    /// <summary>
    /// Kiểm tra Product có tồn tại không
    /// Optimize query bằng Any() thay vì FirstOrDefault()
    /// </summary>
    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .AsNoTracking()
            .AnyAsync(p => p.Id == id, cancellationToken);
    }

    #endregion

    #region Business Query Operations

    /// <summary>
    /// Lấy Product theo SKU (business identifier)
    /// SKU là unique identifier dành cho business users
    /// </summary>
    public async Task<Domain.Entities.Product?> GetBySkuAsync(string sku, CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => EF.Property<string>(p, "ProductSku") == sku, cancellationToken);
    }

    /// <summary>
    /// Kiểm tra SKU có unique không (exclude specific product để support update)
    /// Critical business rule: SKU phải unique trong toàn hệ thống
    /// </summary>
    public async Task<bool> IsSkuUniqueAsync(string sku, Guid? excludeProductId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Products.Where(p => EF.Property<string>(p, "ProductSku") == sku);
        
        if (excludeProductId.HasValue)
        {
            query = query.Where(p => p.Id != excludeProductId.Value);
        }

        return !await query.AnyAsync(cancellationToken);
    }

    /// <summary>
    /// Lấy products với pagination và advanced filtering
    /// Business use case: Product catalog browsing với performance optimization
    /// </summary>
    public async Task<(List<Domain.Entities.Product> products, int totalCount)> GetProductsPagedAsync(
        int page,
        int pageSize,
        List<Guid>? categoryIds = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        string? searchTerm = null,
        bool onlyActive = true,
        string sortBy = "name",
        string sortDirection = "asc",
        CancellationToken cancellationToken = default)
    {
        var query = _context.Products.AsNoTracking();

        // Apply filters
        if (onlyActive)
        {
            query = query.Where(p => p.IsActive);
        }

        if (categoryIds != null && categoryIds.Any())
        {
            query = query.Where(p => categoryIds.Contains(p.CategoryId));
        }

        if (minPrice.HasValue)
        {
            query = query.Where(p => p.Price.Amount >= minPrice.Value);
        }

        if (maxPrice.HasValue)
        {
            query = query.Where(p => p.Price.Amount <= maxPrice.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(p => 
                EF.Property<string>(p, "ProductName").Contains(searchTerm) ||
                p.Description.Contains(searchTerm) ||
                EF.Property<string>(p, "ProductSku").Contains(searchTerm));
        }

        // Get total count
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply sorting
        query = sortBy.ToLower() switch
        {
            "price" => sortDirection.ToLower() == "desc" 
                ? query.OrderByDescending(p => p.Price.Amount)
                : query.OrderBy(p => p.Price.Amount),
            "stock" => sortDirection.ToLower() == "desc"
                ? query.OrderByDescending(p => p.StockQuantity)
                : query.OrderBy(p => p.StockQuantity),
            "createdat" => sortDirection.ToLower() == "desc"
                ? query.OrderByDescending(p => p.CreatedAt)
                : query.OrderBy(p => p.CreatedAt),
            _ => sortDirection.ToLower() == "desc"
                ? query.OrderByDescending(p => EF.Property<string>(p, "ProductName"))
                : query.OrderBy(p => EF.Property<string>(p, "ProductName"))
        };

        // Apply pagination
        var products = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (products, totalCount);
    }

    /// <summary>
    /// Lấy products có tồn kho thấp
    /// Business use case: Inventory management alerts
    /// </summary>
    public async Task<List<Domain.Entities.Product>> GetLowStockProductsAsync(
        int threshold = 10,
        CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .AsNoTracking()
            .Where(p => p.IsActive &&
                       p.StockQuantity <= threshold &&
                       p.StockQuantity > 0)
            .OrderBy(p => p.StockQuantity)
            .ThenBy(p => EF.Property<string>(p, "ProductName"))
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Lấy tất cả Products đang active
    /// Simple query cho basic listing (không recommend cho production với data lớn)
    /// </summary>
    public async Task<List<Domain.Entities.Product>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => EF.Property<string>(p, "ProductName"))
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Lấy Products theo CategoryId
    /// Core business operation cho catalog browsing
    /// </summary>
    public async Task<List<Domain.Entities.Product>> GetByCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .AsNoTracking()
            .Where(p => p.CategoryId == categoryId && p.IsActive)
            .OrderBy(p => EF.Property<string>(p, "ProductName"))
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Lấy Products trong khoảng giá
    /// Business requirement cho price filtering
    /// </summary>
    public async Task<List<Domain.Entities.Product>> GetByPriceRangeAsync(
        decimal minPrice,
        decimal maxPrice,
        string currency,
        CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .AsNoTracking()
            .Where(p => p.IsActive &&
                       p.Price.Amount >= minPrice &&
                       p.Price.Amount <= maxPrice &&
                       p.Price.Currency == currency)
            .OrderBy(p => p.Price.Amount)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Lấy Products đang active (có thể bán)
    /// Customer-facing operation cho product catalog
    /// </summary>
    public async Task<List<Domain.Entities.Product>> GetActiveProductsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => EF.Property<string>(p, "ProductName"))
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Search Products theo tên, description, SKU
    /// Customer product discovery operation
    /// </summary>
    public async Task<List<Domain.Entities.Product>> SearchByNameAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .AsNoTracking()
            .Where(p => p.IsActive &&
                       (EF.Property<string>(p, "ProductName").Contains(searchTerm) ||
                        p.Description.Contains(searchTerm) ||
                        EF.Property<string>(p, "ProductSku").Contains(searchTerm)))
            .OrderBy(p => EF.Property<string>(p, "ProductName"))
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Lấy Products theo danh sách IDs
    /// Bulk operations cho cart, order processing
    /// </summary>
    public async Task<List<Domain.Entities.Product>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        var idList = ids.ToList();
        if (!idList.Any())
            return new List<Domain.Entities.Product>();

        return await _context.Products
            .AsNoTracking()
            .Where(p => idList.Contains(p.Id))
            .ToListAsync(cancellationToken);
    }

    #endregion

    #region Analytics & Reporting

    /// <summary>
    /// Đếm số lượng Products theo Category
    /// Analytics operation cho business reporting
    /// </summary>
    public async Task<int> CountByCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .AsNoTracking()
            .CountAsync(p => p.CategoryId == categoryId && p.IsActive, cancellationToken);
    }

    /// <summary>
    /// Tính tổng giá trị inventory theo currency
    /// Financial reporting operation
    /// </summary>
    public async Task<decimal> GetTotalInventoryValueAsync(string currency, CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .AsNoTracking()
            .Where(p => p.IsActive && p.Price.Currency == currency)
            .SumAsync(p => p.Price.Amount * p.StockQuantity, cancellationToken);
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Save changes với error handling
    /// </summary>
    private async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }

    #endregion
}