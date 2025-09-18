namespace Product.Application.DTOs;

/// <summary>
/// DTO cho Category - dùng để trả về cho client
/// Chứa thông tin category và metadata để build UI hierarchy
/// </summary>
public record CategoryDto
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
    public List<CategoryDto> Children { get; init; } = new();

    /// <summary>
    /// Total number of products in this category
    /// </summary>
    public int ProductCount { get; init; }
}

/// <summary>
/// DTO cho Category list với pagination
/// </summary>
public record CategoryListDto
{
    public List<CategoryDto> Categories { get; init; } = new();
    public int TotalCount { get; init; }
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public bool HasNextPage => PageNumber * PageSize < TotalCount;
    public bool HasPreviousPage => PageNumber > 1;
}

/// <summary>
/// DTO đơn giản cho dropdown/select lists
/// </summary>
public record CategorySummaryDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Path { get; init; } = string.Empty;
    public int Level { get; init; }
    public bool IsActive { get; init; }
}