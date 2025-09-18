namespace Product.Api.Contracts.Categories;

/// <summary>
/// Response cho category details
/// </summary>
public record CategoryResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string? Description { get; init; }
    public Guid? ParentId { get; init; }
    public int Level { get; init; }
    public string Path { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public int DisplayOrder { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public string CreatedBy { get; init; } = string.Empty;
    public string? UpdatedBy { get; init; }

    /// <summary>
    /// Children categories (for tree structure)
    /// </summary>
    public List<CategoryResponse> Children { get; init; } = new();

    /// <summary>
    /// Total number of products in this category
    /// </summary>
    public int ProductCount { get; init; }
}

/// <summary>
/// Response cho category list với pagination
/// </summary>
public record CategoryListResponse
{
    public List<CategoryResponse> Categories { get; init; } = new();
    public int TotalCount { get; init; }
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public bool HasNextPage { get; init; }
    public bool HasPreviousPage { get; init; }
}

/// <summary>
/// Response đơn giản cho dropdown/select lists
/// </summary>
public record CategorySummaryResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Path { get; init; } = string.Empty;
    public int Level { get; init; }
    public bool IsActive { get; init; }
}

/// <summary>
/// Response cho category creation
/// </summary>
public record CreateCategoryResponse
{
    public Guid Id { get; init; }
}

/// <summary>
/// Response cho validation
/// </summary>
public record ValidationResponse
{
    public bool IsValid { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Response cho category statistics
/// </summary>
public record CategoryStatsResponse
{
    public Guid CategoryId { get; init; }
    public string CategoryName { get; init; } = string.Empty;
    public int TotalProducts { get; init; }
    public int ActiveProducts { get; init; }
    public int SubCategoriesCount { get; init; }
    public int TotalDescendants { get; init; }
}