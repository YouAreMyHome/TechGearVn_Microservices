using MediatR;
using Product.Application.DTOs;

namespace Product.Application.Queries;

/// <summary>
/// Query để lấy category theo ID
/// </summary>
public record GetCategoryByIdQuery : IRequest<CategoryDto?>
{
    public Guid Id { get; init; }
}

/// <summary>
/// Query để lấy danh sách categories với pagination
/// </summary>
public record GetCategoriesQuery : IRequest<CategoryListDto>
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public string? SearchTerm { get; init; }
    public bool? IsActive { get; init; }
    public Guid? ParentId { get; init; }
}

/// <summary>
/// Query để lấy category hierarchy (tree structure)
/// </summary>
public record GetCategoryHierarchyQuery : IRequest<List<CategoryDto>>
{
    public Guid? RootId { get; init; }
    public bool IncludeInactive { get; init; } = false;
    public int MaxDepth { get; init; } = 5;
}

/// <summary>
/// Query để lấy category path (breadcrumb)
/// </summary>
public record GetCategoryPathQuery : IRequest<List<CategorySummaryDto>>
{
    public Guid CategoryId { get; init; }
}

/// <summary>
/// Query để lấy categories cho dropdown/select
/// </summary>
public record GetCategoriesForSelectQuery : IRequest<List<CategorySummaryDto>>
{
    public bool IncludeInactive { get; init; } = false;
    public Guid? ExcludeCategoryId { get; init; } // Exclude specific category (e.g., when moving)
}

/// <summary>
/// Query để validate category business rules
/// </summary>
public record ValidateCategoryQuery : IRequest<bool>
{
    public Guid? CategoryId { get; init; }
    public string? Slug { get; init; }
    public Guid? ParentId { get; init; }
}

/// <summary>
/// Query để lấy category statistics
/// </summary>
public record GetCategoryStatsQuery : IRequest<CategoryStatsDto>
{
    public Guid CategoryId { get; init; }
}

/// <summary>
/// DTO cho category statistics
/// </summary>
public record CategoryStatsDto
{
    public Guid CategoryId { get; init; }
    public string CategoryName { get; init; } = string.Empty;
    public int TotalProducts { get; init; }
    public int ActiveProducts { get; init; }
    public int SubCategoriesCount { get; init; }
    public int TotalDescendants { get; init; }
}