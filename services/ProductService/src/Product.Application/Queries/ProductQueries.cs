using MediatR;
using Product.Application.DTOs;

namespace Product.Application.Queries;

/// <summary>
/// Query lấy danh sách sản phẩm với filtering business logic
/// CQRS Read Model: Optimized cho product catalog browsing
/// Business rules: Active products, category filtering, price range
/// </summary>
public record GetProductsQuery : IRequest<PagedProductResult>
{
    /// <summary>
    /// Số trang (bắt đầu từ 1)
    /// </summary>
    public int Page { get; init; } = 1;

    /// <summary>
    /// Số items per page (max 100)
    /// </summary>
    public int PageSize { get; init; } = 20;

    /// <summary>
    /// Filter theo Category ID
    /// </summary>
    public Guid? CategoryId { get; init; }

    /// <summary>
    /// Include sub-categories trong filter
    /// Business logic: Category tree traversal
    /// </summary>
    public bool IncludeSubCategories { get; init; } = false;

    /// <summary>
    /// Giá minimum (VND)
    /// </summary>
    public decimal? MinPrice { get; init; }

    /// <summary>
    /// Giá maximum (VND)
    /// </summary>
    public decimal? MaxPrice { get; init; }

    /// <summary>
    /// Search term (tên, mô tả, SKU)
    /// Business logic: Full-text search optimization
    /// </summary>
    public string? SearchTerm { get; init; }

    /// <summary>
    /// Filter theo active status
    /// </summary>
    public bool? IsActive { get; init; }

    /// <summary>
    /// Filter có stock hay không
    /// </summary>
    public bool? HasStock { get; init; }

    /// <summary>
    /// Chỉ lấy active products
    /// Business rule: Default chỉ show sản phẩm đang bán
    /// </summary>
    public bool OnlyActive { get; init; } = true;

    /// <summary>
    /// Sort field (name, price, created_date)
    /// </summary>
    public string SortBy { get; init; } = "name";

    /// <summary>
    /// Sort direction (asc, desc)
    /// </summary>
    public string SortDirection { get; init; } = "asc";
}

/// <summary>
/// Query lấy product theo ID
/// </summary>
public record GetProductByIdQuery(Guid ProductId) : IRequest<ProductDto?>;

/// <summary>
/// Query lấy product theo SKU
/// Business use case: Barcode scanning, quick lookup
/// </summary>
public record GetProductBySkuQuery(string Sku) : IRequest<ProductDto?>;

/// <summary>
/// Query lấy products có tồn kho thấp
/// Business use case: Inventory management alerts
/// </summary>
public record GetLowStockProductsQuery(int Threshold = 10) : IRequest<List<ProductDto>>;

/// <summary>
/// Query search products với advanced options
/// Business use case: Product search với filtering
/// </summary>
public record SearchProductsQuery : IRequest<PagedProductResult>
{
    public string SearchTerm { get; init; } = string.Empty;
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public Guid? CategoryId { get; init; }
    public bool IncludeSubCategories { get; init; } = true;
    public decimal? MinPrice { get; init; }
    public decimal? MaxPrice { get; init; }
    public bool OnlyActive { get; init; } = true;
}

/// <summary>
/// Query lấy products theo category với business logic
/// </summary>
public record GetProductsByCategoryQuery : IRequest<PagedProductResult>
{
    public Guid CategoryId { get; init; }
    public bool IncludeSubCategories { get; init; } = true;
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public bool OnlyActive { get; init; } = true;
    public string SortBy { get; init; } = "name";
    public string SortDirection { get; init; } = "asc";
}