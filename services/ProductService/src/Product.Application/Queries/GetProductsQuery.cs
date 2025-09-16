using MediatR;
using Product.Application.DTOs;

namespace Product.Application.Queries;

/// <summary>
/// Query để lấy danh sách sản phẩm với pagination và filtering
/// Support các filters thường dùng: category, price range, active status
/// </summary>
public record GetProductsQuery(
    int Page = 1,
    int PageSize = 20,
    Guid? CategoryId = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    bool? IsActive = null,
    string? SearchTerm = null) : IRequest<ProductListDto>;

/// <summary>
/// DTO cho danh sách sản phẩm với pagination info
/// </summary>
public record ProductListDto
{
    public List<ProductDto> Products { get; init; } = new();
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
}