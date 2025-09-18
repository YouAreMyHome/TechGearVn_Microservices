namespace Product.Application.DTOs;

/// <summary>
/// DTO cho danh sách sản phẩm với pagination metadata
/// Application layer DTO: Chứa business data và pagination info
/// Khác với API Response: Đây là internal business structure
/// </summary>
public record ProductListDto
{
    /// <summary>
    /// Danh sách products trong page hiện tại
    /// </summary>
    public List<ProductDto> Products { get; init; } = new();

    /// <summary>
    /// Tổng số products match với filter criteria
    /// Dùng cho calculate pagination metadata
    /// </summary>
    public int TotalCount { get; init; }

    /// <summary>
    /// Page hiện tại (1-based indexing)
    /// </summary>
    public int Page { get; init; }

    /// <summary>
    /// Số items per page
    /// Business rule: Max 100 items/page để tránh performance issues
    /// </summary>
    public int PageSize { get; init; }

    /// <summary>
    /// Tổng số pages (calculated property)
    /// Math.Ceiling để round up (page cuối có thể ít items hơn PageSize)
    /// </summary>
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    /// <summary>
    /// Có page tiếp theo không (UI pagination)
    /// </summary>
    public bool HasNextPage => Page < TotalPages;

    /// <summary>
    /// Có page trước đó không (UI pagination)
    /// </summary>
    public bool HasPreviousPage => Page > 1;

    /// <summary>
    /// Starting item number trong tổng list (for display: "Showing 21-40 of 156")
    /// </summary>
    public int StartItemNumber => ((Page - 1) * PageSize) + 1;

    /// <summary>
    /// Ending item number trong tổng list
    /// </summary>
    public int EndItemNumber => Math.Min(Page * PageSize, TotalCount);
}