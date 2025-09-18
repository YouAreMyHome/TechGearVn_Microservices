namespace Product.Application.DTOs;

/// <summary>
/// Paginated result cho Product queries
/// CQRS Read Model: Optimized for product catalog browsing
/// </summary>
public record PagedProductResult
{
    public List<ProductDto> Products { get; init; } = new();
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages { get; init; }
    public bool HasNextPage { get; init; }
    public bool HasPreviousPage { get; init; }

    /// <summary>
    /// Filters applied to this result
    /// </summary>
    public ProductFilterMetadata? FilterMetadata { get; init; }
    
    /// <summary>
    /// Applied filters as dictionary
    /// </summary>
    public Dictionary<string, object> AppliedFilters { get; init; } = new();
}

/// <summary>
/// Metadata về filters được apply
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