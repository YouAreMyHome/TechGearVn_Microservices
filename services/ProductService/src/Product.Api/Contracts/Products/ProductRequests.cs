using System.ComponentModel.DataAnnotations;

namespace Product.Api.Contracts.Products;

/// <summary>
/// Request để get products với advanced filtering
/// API Layer: HTTP request contract cho product catalog browsing
/// </summary>
public record GetProductsRequest
{
    /// <summary>
    /// Số trang (bắt đầu từ 1)
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "Page phải lớn hơn 0")]
    public int Page { get; init; } = 1;

    /// <summary>
    /// Số items per page (max 100)
    /// </summary>
    [Range(1, 100, ErrorMessage = "PageSize phải từ 1 đến 100")]
    public int PageSize { get; init; } = 20;

    /// <summary>
    /// Filter theo Category path (electronics/smartphones)
    /// </summary>
    public string? CategoryPath { get; init; }

    /// <summary>
    /// Filter theo Category ID
    /// </summary>
    public Guid? CategoryId { get; init; }

    /// <summary>
    /// Include sub-categories trong filter
    /// </summary>
    public bool IncludeSubCategories { get; init; } = false;

    /// <summary>
    /// Giá minimum (VND)
    /// </summary>
    [Range(0, double.MaxValue, ErrorMessage = "MinPrice phải >= 0")]
    public decimal? MinPrice { get; init; }

    /// <summary>
    /// Giá maximum (VND)
    /// </summary>
    [Range(0, double.MaxValue, ErrorMessage = "MaxPrice phải >= 0")]
    public decimal? MaxPrice { get; init; }

    /// <summary>
    /// Search term (tên, mô tả, SKU)
    /// </summary>
    [StringLength(100, ErrorMessage = "Search term không được quá 100 ký tự")]
    public string? SearchTerm { get; init; }

    /// <summary>
    /// Chỉ lấy active products
    /// </summary>
    public bool OnlyActive { get; init; } = true;

    /// <summary>
    /// Sort field (name, price, created_date, stock)
    /// </summary>
    [RegularExpression("^(name|price|created_date|stock|updated_date)$", 
        ErrorMessage = "SortBy phải là: name, price, created_date, stock, updated_date")]
    public string SortBy { get; init; } = "name";

    /// <summary>
    /// Sort direction (asc, desc)
    /// </summary>
    [RegularExpression("^(asc|desc)$", ErrorMessage = "SortDirection phải là: asc hoặc desc")]
    public string SortDirection { get; init; } = "asc";
}

/// <summary>
/// Response cho paginated products
/// </summary>
public record PagedProductResponse
{
    public List<ProductResponse> Products { get; init; } = new();
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages { get; init; }
    public bool HasNextPage { get; init; }
    public bool HasPreviousPage { get; init; }

    /// <summary>
    /// Applied filters metadata
    /// </summary>
    public ProductFilterMetadata? FilterMetadata { get; init; }
}

/// <summary>
/// Filter metadata for API response
/// </summary>
public record ProductFilterMetadata
{
    public string? CategoryPath { get; init; }
    public Guid? CategoryId { get; init; }
    public bool IncludeSubCategories { get; init; }
    public decimal? MinPrice { get; init; }
    public decimal? MaxPrice { get; init; }
    public string? SearchTerm { get; init; }
    public bool OnlyActive { get; init; }
    public string SortBy { get; init; } = "name";
    public string SortDirection { get; init; } = "asc";
}

/// <summary>
/// Response cho create product
/// </summary>
public record CreateProductResponse
{
    public Guid ProductId { get; init; }
    public ProductResponse? Product { get; init; }
}

/// <summary>
/// Request cho bulk price update
/// </summary>
public record BulkUpdatePricesRequest
{
    [Required(ErrorMessage = "ProductPriceUpdates is required")]
    [MinLength(1, ErrorMessage = "Phải có ít nhất 1 product để update")]
    public List<ProductPriceUpdate> ProductPriceUpdates { get; init; } = new();

    [StringLength(200, ErrorMessage = "Reason không được quá 200 ký tự")]
    public string? Reason { get; init; }
}

/// <summary>
/// Individual product price update trong bulk operation
/// </summary>
public record ProductPriceUpdate
{
    [Required]
    public Guid ProductId { get; init; }

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "NewPrice phải > 0")]
    public decimal NewPrice { get; init; }

    [StringLength(3, MinimumLength = 3, ErrorMessage = "Currency phải có 3 ký tự")]
    public string Currency { get; init; } = "VND";
}